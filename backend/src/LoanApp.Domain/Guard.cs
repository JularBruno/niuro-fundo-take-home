namespace LoanApp.Domain;

internal static class Guard
{
    public static string Required(string? value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new DomainException($"{name} is required.") : value.Trim();
}
