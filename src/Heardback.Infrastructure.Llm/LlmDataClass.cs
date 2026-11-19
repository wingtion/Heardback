namespace Heardback.Infrastructure.Llm;

/// <summary>What kind of data a request carries; decides which providers may receive it (ADR-0008).</summary>
public enum LlmDataClass
{
    /// <summary>Text that is normally public, such as a pasted job posting.</summary>
    Public,

    /// <summary>Anything containing a user's CV text. Never sent to a provider that may use it for training.</summary>
    Personal,
}
