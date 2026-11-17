using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace JobTrail.Shared.Http;

/// <summary>
/// Validates request DTOs with DataAnnotations before the handler runs and returns a 400 ValidationProblem
/// with field-level errors (keyed by JSON property name). Applied once to the root route group, so every
/// module endpoint is covered without opting in. Attributes on records need the <c>property:</c> target.
/// </summary>
public static class RequestValidation
{
    private static readonly ConcurrentDictionary<Type, bool> ValidatableTypes = new();

    public static TBuilder WithRequestValidation<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.AddEndpointFilterFactory((factoryContext, next) =>
        {
            var validatedArguments = factoryContext.MethodInfo.GetParameters()
                .Where(parameter => IsValidatable(parameter.ParameterType))
                .Select(parameter => parameter.Position)
                .ToArray();

            if (validatedArguments.Length == 0)
            {
                return next;
            }

            return invocationContext =>
            {
                foreach (var position in validatedArguments)
                {
                    if (invocationContext.Arguments[position] is { } argument
                        && TryGetErrors(argument) is { } errors)
                    {
                        return ValueTask.FromResult<object?>(TypedResults.ValidationProblem(errors));
                    }
                }

                return next(invocationContext);
            };
        });

        return builder;
    }

    private static Dictionary<string, string[]>? TryGetErrors(object argument)
    {
        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(argument, new ValidationContext(argument), results, validateAllProperties: true))
        {
            return null;
        }

        return results
            .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty), (result, member) => (member, result))
            .GroupBy(pair => JsonNamingPolicy.CamelCase.ConvertName(pair.member), StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(pair => pair.result.ErrorMessage ?? "The value is invalid.").ToArray(),
                StringComparer.Ordinal);
    }

    private static bool IsValidatable(Type type) =>
        ValidatableTypes.GetOrAdd(type, static candidate =>
            candidate.IsClass
            && candidate != typeof(string)
            && candidate.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Any(property => property.IsDefined(typeof(ValidationAttribute), inherit: true)));
}
