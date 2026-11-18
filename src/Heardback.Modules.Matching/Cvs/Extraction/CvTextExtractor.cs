using System.Text;
using Heardback.Shared.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Exceptions;

namespace Heardback.Modules.Matching.Cvs.Extraction;

/// <summary>
/// Reads the text of a CV PDF in memory; the file itself is never stored (PLAN.md). Size, page count,
/// decompressed data and text length are all capped (ADR-0006).
/// </summary>
internal sealed partial class CvTextExtractor(IOptions<MatchingSettings> options, ILogger<CvTextExtractor> logger)
{
    // PDF readers accept the header anywhere in the first kilobyte, after junk some generators write.
    private const int HeaderSearchLength = 1024;
    private const string PageBreak = "\n\n";

    // Raw text shrinks when whitespace is collapsed; stop reading pages well before that could matter.
    private const int RawTextAllowance = 2;

    private static readonly byte[] PdfHeader = "%PDF-"u8.ToArray();

    public Result<ExtractedCv> Extract(ReadOnlyMemory<byte> pdf)
    {
        var settings = options.Value;
        if (pdf.Length > settings.CvMaxBytes)
        {
            return MatchingErrors.CvTooLarge(settings.CvMaxBytes);
        }

        if (!HasPdfHeader(pdf.Span))
        {
            return MatchingErrors.CvNotPdf;
        }

        var budget = new DecodeBudget(settings.CvMaxDecodedBytes);
        try
        {
            return Read(pdf, settings, budget);
        }
        catch (Exception exception) when (budget.Exceeded)
        {
            LogRejected(exception.GetType().Name);
            return MatchingErrors.CvTooComplex;
        }
        catch (PdfDocumentEncryptedException)
        {
            return MatchingErrors.CvEncrypted;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // A damaged or unusual PDF can fail anywhere in the parser. Only the exception type is logged: the
            // message may quote document content.
            LogUnreadable(exception.GetType().Name);
            return MatchingErrors.CvUnreadable;
        }
    }

    private static Result<ExtractedCv> Read(ReadOnlyMemory<byte> pdf, MatchingSettings settings, DecodeBudget budget)
    {
        using var document = PdfDocument.Open(
            pdf.ToArray(),
            new ParsingOptions { FilterProvider = new BudgetedFilterProvider(budget) });

        var pageCount = document.NumberOfPages;
        if (pageCount > settings.CvMaxPages)
        {
            return MatchingErrors.CvTooManyPages(settings.CvMaxPages);
        }

        var raw = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            raw.Append(ContentOrderTextExtractor.GetText(page)).Append(PageBreak);
            if (raw.Length > settings.CvMaxTextLength * RawTextAllowance)
            {
                return MatchingErrors.CvTooMuchText(settings.CvMaxTextLength);
            }
        }

        var text = CvText.Normalize(raw.ToString());
        if (text.Length == 0)
        {
            return MatchingErrors.CvNoText;
        }

        return text.Length > settings.CvMaxTextLength
            ? MatchingErrors.CvTooMuchText(settings.CvMaxTextLength)
            : new ExtractedCv(text, pageCount);
    }

    private static bool HasPdfHeader(ReadOnlySpan<byte> data) =>
        data[..Math.Min(data.Length, HeaderSearchLength)].IndexOf(PdfHeader) >= 0;

    [LoggerMessage(Level = LogLevel.Warning, Message = "CV rejected: it unpacks to too much data ({ExceptionType})")]
    private partial void LogRejected(string exceptionType);

    [LoggerMessage(Level = LogLevel.Warning, Message = "CV could not be read ({ExceptionType})")]
    private partial void LogUnreadable(string exceptionType);
}
