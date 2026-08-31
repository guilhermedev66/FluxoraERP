using Fluxora.Domain.Common;

namespace Fluxora.Domain.Customers;

public class Customer : BaseEntity
{
    public string Name { get; private set; }

    public string Document { get; private set; }

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public bool IsActive { get; private set; } = true;

    private Customer(string name, string document, string? email, string? phone)
    {
        Name = name;
        Document = document;
        Email = email;
        Phone = phone;
    }

    // EF Core materialization constructor.
    private Customer()
    {
        Name = string.Empty;
        Document = string.Empty;
    }

    public static Customer Create(string name, string document, string? email, string? phone)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Customer name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(document))
        {
            throw new ArgumentException("Customer document (CPF/CNPJ) is required.", nameof(document));
        }

        return new Customer(name.Trim(), document.Trim(), email?.Trim(), phone?.Trim());
    }

    public void Update(string name, string? email, string? phone)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Customer name is required.", nameof(name));
        }

        Name = name.Trim();
        Email = email?.Trim();
        Phone = phone?.Trim();
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
