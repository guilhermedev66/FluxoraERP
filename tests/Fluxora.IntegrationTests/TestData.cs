namespace Fluxora.IntegrationTests;

internal static class TestData
{
    public static string UniqueDocument() => $"TEST-{Guid.NewGuid():N}"[..20];
}
