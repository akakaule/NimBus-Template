using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NimBus.Core.Events;

namespace Acme.Platform.Events;

/// <summary>
/// A new company account was registered in the CRM.
/// </summary>
[Description("Published when a new company account is created in the CRM.")]
[SessionKey(nameof(AccountId))]
public class CrmAccountCreated : Event
{
    // Sessions are keyed on AccountId across the whole catalog, so every message about one
    // account is ordered relative to the others regardless of which system produced it.
    public static readonly CrmAccountCreated Example = new()
    {
        AccountId = Guid.Parse("9c1a6f2e-4c31-4f0e-9a1c-2f7d5b3e8a10"),
        Name = "Nordvest Logistik A/S",
        CountryCode = "DK",
        VatNumber = "DK12345678",
        SalesRegion = "Nordics",
    };

    [Required]
    [Description("Stable CRM identifier for the account. Session key for every account-scoped message.")]
    public Guid AccountId { get; set; }

    [Required]
    [Description("Registered company name.")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Description("ISO 3166-1 alpha-2 country of registration.")]
    public string CountryCode { get; set; } = string.Empty;

    [Description("VAT registration number, where the account has one.")]
    public string VatNumber { get; set; } = string.Empty;

    [Description("Sales region the account is managed from.")]
    public string SalesRegion { get; set; } = string.Empty;
}
