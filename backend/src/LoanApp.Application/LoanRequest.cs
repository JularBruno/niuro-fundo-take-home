using LoanApp.Domain.Customers;

namespace LoanApp.Application;

/// <summary>A loan application as submitted by the applicant, already validated by the domain types.</summary>
public sealed record LoanRequest(
    Ssn Ssn,
    string FirstName,
    string LastName,
    string CompanyName,
    Address Address,
    decimal RequestedAmount);
