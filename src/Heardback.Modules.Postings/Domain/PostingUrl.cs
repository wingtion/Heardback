using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Heardback.Modules.Postings.Contracts;

namespace Heardback.Modules.Postings.Domain;

/// <summary>
/// Turns a posting link into a comparison key, so the same job matches however the link was copied:
/// with or without <c>www.</c>, tracking parameters, a fragment, a trailing slash or parameters in another order.
/// The key has no scheme, so http and https links to the same page match.
/// </summary>
internal static class PostingUrl
{
    private const string WwwPrefix = "www.";
    private const string TrackingParameterPrefix = "utm_";
    private const char PathSeparator = '/';
    private const char QueryStart = '?';
    private const char ParameterSeparator = '&';
    private const char NameValueSeparator = '=';
    private const char PortSeparator = ':';
    private const char HostLabelSeparator = '.';

    private const string LinkedInHost = "linkedin.com";
    private const string LinkedInSubdomainSuffix = ".linkedin.com";
    private const string LinkedInJobPath = "/jobs/view/";
    private const string LinkedInJobIdParameter = "currentJobId";

    // Parameters that only say where a click came from; they never pick a different posting.
    private static readonly FrozenSet<string> TrackingParameters = new[]
    {
        "gclid", "fbclid", "msclkid", "mc_cid", "mc_eid", "trk", "trkInfo", "refId", "trackingId", "lipi", "eBP",
        "originalSubdomain", "ref", "src", "source",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static bool TryNormalize(string input, [NotNullWhen(true)] out string? key)
    {
        key = TryParse(input, out var parts) ? parts.Key : null;
        return key is not null;
    }

    public static bool TryParse(string input, [NotNullWhen(true)] out PostingUrlParts? parts)
    {
        ArgumentNullException.ThrowIfNull(input);
        parts = null;

        var trimmed = input.Trim();
        if (trimmed.Length > PostingLimits.UrlMaxLength
            || !Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return false;
        }

        var host = NormalizeHost(uri.IdnHost);
        if (!host.Contains(HostLabelSeparator, StringComparison.Ordinal))
        {
            return false;
        }

        var key = host == LinkedInHost && TryGetLinkedInJobId(uri, out var jobId)
            ? LinkedInHost + LinkedInJobPath + jobId
            : host + Port(uri) + Path(uri) + Query(uri);
        var cleanUrl = Clean(uri);

        // Both are stored and indexed, and percent-encoding can make them much longer than the input.
        if (key.Length > PostingLimits.UrlMaxLength || cleanUrl.Length > PostingLimits.UrlMaxLength)
        {
            return false;
        }

        parts = new PostingUrlParts(key, cleanUrl);
        return true;
    }

    // Keeps the link as typed but drops what can identify the person who copied it.
    private static string Clean(Uri uri)
    {
        var kept = Parameters(uri).Where(parameter => !IsTracking(parameter.Name)).Select(parameter => parameter.Text);
        var builder = new UriBuilder(uri)
        {
            UserName = string.Empty,
            Password = string.Empty,
            Fragment = string.Empty,
            Query = string.Join(ParameterSeparator, kept),
        };
        if (uri.IsDefaultPort)
        {
            builder.Port = -1;
        }

        return builder.Uri.AbsoluteUri;
    }

    private static string NormalizeHost(string host)
    {
        var lower = host.TrimEnd(HostLabelSeparator).ToLowerInvariant();
        if (lower.EndsWith(LinkedInSubdomainSuffix, StringComparison.Ordinal))
        {
            return LinkedInHost;
        }

        return lower.StartsWith(WwwPrefix, StringComparison.Ordinal) ? lower[WwwPrefix.Length..] : lower;
    }

    private static string Port(Uri uri) =>
        uri.IsDefaultPort ? string.Empty : PortSeparator + uri.Port.ToString(CultureInfo.InvariantCulture);

    private static string Path(Uri uri) => uri.AbsolutePath.TrimEnd(PathSeparator);

    private static string Query(Uri uri)
    {
        var kept = Parameters(uri)
            .Where(parameter => !IsTracking(parameter.Name))
            .OrderBy(parameter => parameter.Name, StringComparer.Ordinal)
            .ThenBy(parameter => parameter.Text, StringComparer.Ordinal)
            .Select(parameter => parameter.Text)
            .ToList();

        return kept.Count == 0 ? string.Empty : QueryStart + string.Join(ParameterSeparator, kept);
    }

    private static IEnumerable<(string Name, string Text)> Parameters(Uri uri) =>
        uri.Query.TrimStart(QueryStart)
            .Split(ParameterSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(text => (Name: text.Split(NameValueSeparator, 2)[0], Text: text))
            .Where(parameter => parameter.Name.Length > 0);

    private static bool IsTracking(string name) =>
        TrackingParameters.Contains(name) || name.StartsWith(TrackingParameterPrefix, StringComparison.OrdinalIgnoreCase);

    // LinkedIn shows one job under many links: /jobs/view/123, /jobs/view/backend-engineer-at-x-123 and
    // search pages with ?currentJobId=123. The numeric id is what identifies the job.
    private static bool TryGetLinkedInJobId(Uri uri, [NotNullWhen(true)] out string? jobId)
    {
        if (uri.AbsolutePath.StartsWith(LinkedInJobPath, StringComparison.OrdinalIgnoreCase))
        {
            var segment = uri.AbsolutePath[LinkedInJobPath.Length..].Split(PathSeparator)[0];
            jobId = TrailingDigits(segment);
            return jobId is not null;
        }

        var parameter = Parameters(uri)
            .FirstOrDefault(candidate => string.Equals(candidate.Name, LinkedInJobIdParameter, StringComparison.OrdinalIgnoreCase));
        var value = parameter.Text?.Split(NameValueSeparator, 2) is [_, var raw] ? raw : string.Empty;
        jobId = value.Length > 0 && value.All(char.IsAsciiDigit) ? value : null;
        return jobId is not null;
    }

    private static string? TrailingDigits(string segment)
    {
        var start = segment.Length;
        while (start > 0 && char.IsAsciiDigit(segment[start - 1]))
        {
            start--;
        }

        return start == segment.Length ? null : segment[start..];
    }
}
