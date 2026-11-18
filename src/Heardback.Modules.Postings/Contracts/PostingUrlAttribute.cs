using System.ComponentModel.DataAnnotations;
using Heardback.Modules.Postings.Domain;

namespace Heardback.Modules.Postings.Contracts;

/// <summary>
/// Validates that a request field holds an absolute http(s) link that <see cref="IJobPostings"/> accepts.
/// Null and empty values pass, so combine it with <see cref="RequiredAttribute"/> when the link is mandatory.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class PostingUrlAttribute : ValidationAttribute
{
    /// <summary>The message shown next to the field (docs/design.md §14).</summary>
    public const string Message = "Enter a full link that starts with https://";

    /// <summary>Creates the attribute with the standard message.</summary>
    public PostingUrlAttribute()
        : base(Message)
    {
    }

    /// <inheritdoc />
    public override bool IsValid(object? value) =>
        value switch
        {
            null => true,
            string text when string.IsNullOrWhiteSpace(text) => true,
            string text => PostingUrl.TryNormalize(text, out _),
            _ => false,
        };
}
