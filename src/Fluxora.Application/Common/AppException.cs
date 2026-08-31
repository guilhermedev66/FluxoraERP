namespace Fluxora.Application.Common;

public abstract class AppException(string message) : Exception(message);

public sealed class NotFoundException(string entityType, Guid id)
    : AppException($"{entityType} '{id}' was not found.");

public sealed class ConflictException(string message) : AppException(message);
