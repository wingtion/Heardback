using System.Globalization;
using Heardback.Shared.Results;

namespace Heardback.Modules.Matching;

/// <summary>Expected failures of the Matching module. Problems with an uploaded CV are field errors on "file".</summary>
internal static class MatchingErrors
{
    public const string FileField = "file";
    public const string SkillsField = "skills";

    private const int BytesPerMegabyte = 1024 * 1024;

    public static readonly Error ApplicationNotFound =
        Error.NotFound("matching.application_not_found", "This application doesn't exist or was deleted.");

    public static readonly Error CvNotFound =
        Error.NotFound("matching.cv_not_found", "Upload a CV first.");

    public static readonly Error CvMissing =
        Error.Validation("matching.cv_missing", FileField, "Choose a PDF file.");

    public static readonly Error CvNotPdf =
        Error.Validation("matching.cv_not_pdf", FileField, "Choose a PDF file.");

    public static readonly Error CvEncrypted =
        Error.Validation(
            "matching.cv_encrypted", FileField, "This PDF is password-protected. Save it again without a password.");

    public static readonly Error CvUnreadable =
        Error.Validation(
            "matching.cv_unreadable", FileField, "We couldn't read this PDF. Export it again from your editor.");

    public static readonly Error CvTooComplex =
        Error.Validation(
            "matching.cv_too_complex",
            FileField,
            "This PDF is too complex to read. Export it again from your editor as a simple PDF.");

    public static readonly Error CvNoText =
        Error.Validation(
            "matching.cv_no_text",
            FileField,
            "This PDF has no text we can read; it may be a scan. Export your CV as a PDF from your editor instead.");

    public static readonly Error AiConsentRequired =
        Error.Conflict(
            "matching.ai_consent_required", "Allow Heardback to send your CV text to the AI provider first.");

    public static readonly Error TailoringNeedsCv =
        Error.Conflict("matching.tailoring_needs_cv", "Upload a CV to get suggestions.");

    public static readonly Error TailoringNeedsPostingText =
        Error.Conflict("matching.tailoring_needs_posting_text", "Add the posting text to get suggestions.");

    public static readonly Error TailoringUnavailable =
        Error.Conflict("matching.tailoring_unavailable", "AI suggestions aren't available right now.");

    public static Error TailoringQuotaExceeded(int dailyLimit, DateTimeOffset resetsAt) =>
        Error.QuotaExceeded(
            "matching.tailoring_quota_exceeded",
            string.Create(
                CultureInfo.InvariantCulture,
                $"You've used today's {dailyLimit} suggestion requests. You can ask again after the daily reset."),
            resetsAt);

    public static Error CvTooLarge(int maxBytes) =>
        Error.Validation(
            "matching.cv_too_large",
            FileField,
            string.Create(CultureInfo.InvariantCulture, $"The PDF is larger than {maxBytes / BytesPerMegabyte} MB."));

    public static Error CvTooManyPages(int maxPages) =>
        Error.Validation(
            "matching.cv_too_many_pages",
            FileField,
            string.Create(CultureInfo.InvariantCulture, $"The PDF has more than {maxPages} pages."));

    public static Error CvTooMuchText(int maxLength) =>
        Error.Validation(
            "matching.cv_too_much_text",
            FileField,
            string.Create(
                CultureInfo.InvariantCulture,
                $"The PDF has more text than a CV usually does (over {maxLength:N0} characters)."));

    public static Error UnknownSkill(string name) =>
        Error.Validation(
            "matching.unknown_skill",
            SkillsField,
            string.Create(CultureInfo.InvariantCulture, $"“{name}” is not a skill Heardback knows."));
}
