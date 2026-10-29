namespace LoanApp.Domain.Customers;

/// <summary>
/// US Social Security Number, normalized to 9 digits so "123-45-6789" and "123456789"
/// are the same customer for lookups and blacklist checks.
/// </summary>
public sealed record Ssn
{
    public string Value { get; }

    public Ssn(string value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
        if (digits.Length != 9)
            throw new DomainException("SSN must contain exactly 9 digits.");

        Value = digits;
    }

    // Keeps the full SSN out of logs and exception messages by default.
    public override string ToString() => $"***-**-{Value[^4..]}";
}
