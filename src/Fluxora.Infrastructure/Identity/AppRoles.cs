namespace Fluxora.Infrastructure.Identity;

/// <summary>
/// Seed role set. Coarse roles map to fine-grained permissions/policies introduced alongside
/// the modules that need them (Sales/Finance) - roles alone are not the full authorization model.
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Sales = "Sales";
    public const string Finance = "Finance";

    public static readonly IReadOnlyList<string> All = [Admin, Manager, Sales, Finance];
}
