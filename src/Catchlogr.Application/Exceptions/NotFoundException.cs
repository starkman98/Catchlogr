namespace Catchlogr.Application.Exceptions;

/// <summary>
/// Thrown by the service layer when a requested resource cannot be found.
/// The API middleware maps this to HTTP 404 Not Found.
/// </summary>
public sealed class NotFoundException(string message) : Exception(message);
