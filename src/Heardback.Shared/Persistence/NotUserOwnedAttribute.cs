namespace Heardback.Shared.Persistence;

/// <summary>
/// Declares that an entity with a <c>UserId</c> property is deliberately not filtered per user,
/// for example because it is looked up before the user is known. Architecture tests require every
/// entity with a <c>UserId</c> to be either <see cref="IUserOwned"/> or carry this attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class NotUserOwnedAttribute(string reason) : Attribute
{
    public string Reason { get; } = reason;
}
