using System.ComponentModel.DataAnnotations;

namespace LoanApp.Api.LoanApplications;

// Field-level validation for friendly 400s; the domain types still enforce the same invariants.
public sealed record SubmitLoanApplicationRequest
{
    [Required, MaxLength(100)]
    public string FirstName { get; init; } = "";

    [Required, MaxLength(100)]
    public string LastName { get; init; } = "";

    [Required, MaxLength(200)]
    public string CompanyName { get; init; } = "";

    [Required]
    public AddressRequest Address { get; init; } = null!;

    [Required, RegularExpression(@"^\d{3}-?\d{2}-?\d{4}$", ErrorMessage = "SSN must be 9 digits (123-45-6789).")]
    public string Ssn { get; init; } = "";

    [Range(0.01, double.MaxValue, ErrorMessage = "Requested amount must be greater than zero.")]
    public decimal RequestedAmount { get; init; }
}

public sealed record AddressRequest
{
    [Required, MaxLength(200)]
    public string Street { get; init; } = "";

    [Required, MaxLength(100)]
    public string City { get; init; } = "";

    [Required, RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "State must be a 2-letter code.")]
    public string State { get; init; } = "";

    [Required, RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Zip code must be 5 digits.")]
    public string ZipCode { get; init; } = "";
}
