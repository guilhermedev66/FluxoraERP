using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Fluxora.Application.Common;

/// <summary>
/// Hashes a normalized command DTO for idempotency comparison - deterministic property
/// ordering via System.Text.Json, never the raw HTTP bytes or the idempotency key itself.
/// </summary>
public static class RequestHasher
{
    public static string Hash<T>(T command)
    {
        var json = JsonSerializer.Serialize(command);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexStringLower(bytes);
    }
}
