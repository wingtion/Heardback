using System.ComponentModel.DataAnnotations;

namespace JobTrail.Api.Cors;

internal sealed class CorsSettings
{
    public const string SectionName = "Cors";

    [Required]
    [MinLength(1, ErrorMessage = "At least one allowed origin must be configured for the frontend.")]
    public string[] AllowedOrigins { get; set; } = [];
}
