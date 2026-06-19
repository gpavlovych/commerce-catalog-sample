namespace Commerce.Application.Abstractions.Behaviors;

/// <summary>
/// Thrown when request input fails validation. Kept in the application layer so the API can map it
/// to a 400 without taking a dependency on FluentValidation types.
/// </summary>
public sealed class ValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("One or more validation errors occurred.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
