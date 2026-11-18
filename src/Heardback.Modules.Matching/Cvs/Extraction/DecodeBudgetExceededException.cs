namespace Heardback.Modules.Matching.Cvs.Extraction;

/// <summary>Thrown from inside PdfPig to stop decoding; the extractor checks <see cref="DecodeBudget.Exceeded"/>.</summary>
internal sealed class DecodeBudgetExceededException : Exception
{
    public DecodeBudgetExceededException()
        : base("The PDF unpacks to more data than allowed.")
    {
    }

    public DecodeBudgetExceededException(string message)
        : base(message)
    {
    }

    public DecodeBudgetExceededException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
