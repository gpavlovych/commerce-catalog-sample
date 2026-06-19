namespace Commerce.Domain.Common;

/// <summary>Small set of invariant checks used by entities and value objects.</summary>
public static class Guard
{
    public static string AgainstNullOrWhiteSpace(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        return value.Trim();
    }

    public static decimal AgainstNegative(decimal value, string parameterName)
    {
        if (value < 0m)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, $"{parameterName} cannot be negative.");
        }

        return value;
    }
}
