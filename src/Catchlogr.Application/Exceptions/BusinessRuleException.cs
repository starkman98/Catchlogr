namespace Catchlogr.Application.Exceptions;

/// <summary>
/// Thrown by the service layer when a domain/business rule is violated.
/// The API middleware maps this to HTTP 400 Bad Request.
/// </summary>
public sealed class BusinessRuleException(string message) : Exception(message);

