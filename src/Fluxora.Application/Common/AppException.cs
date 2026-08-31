namespace Fluxora.Application.Common;

public abstract class AppException(string message) : Exception(message);

public sealed class NotFoundException(string entityType, Guid id)
    : AppException($"{entityType} '{id}' was not found.");

public sealed class ConflictException(string message) : AppException(message);

/// <summary>
/// Raised when the caller's expected Version no longer matches the current state - either from
/// the application layer's early check, or translated from EF Core's DbUpdateConcurrencyException
/// when two requests race past that check. Always maps to HTTP 409.
/// </summary>
public sealed class ConcurrencyConflictException(string message) : AppException(message);
