namespace LoanApp.Domain;

/// <summary>Raised when input violates a domain invariant (mapped to 400 by the API).</summary>
public sealed class DomainException(string message) : Exception(message);
