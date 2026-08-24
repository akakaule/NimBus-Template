using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NimBus.Core.Events;

namespace Acme.Platform.Events;

/// <summary>
/// Master data on an existing CRM account changed.
/// </summary>
[Description("Published when master data on an existing CRM account changes.")]
[SessionKey(nameof(AccountId))]
public class CrmAccountUpdated : Event
{
    public static readonly CrmAccountUpdated Example = new()
    {
        AccountId = Guid.Parse("9c1a6f2e-4c31-4f0e-9a1c-2f7d5b3e8a10"),
        Name = "Nordvest Logistik A/S",
        CountryCode = "DK",
        SalesRegion = "Nordics",
        ChangedFields = ["SalesRegion"],
    };

    [Required]
    [Description("The account that changed.")]
    public Guid AccountId { get; set; }

    [Required]
    [Description("Registered company name after the change.")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Description("ISO 3166-1 alpha-2 country of registration after the change.")]
    public string CountryCode { get; set; } = string.Empty;

    [Description("Sales region the account is managed from after the change.")]
    public string SalesRegion { get; set; } = string.Empty;

    // Carried so a consumer can skip work it does not care about without diffing state it
    // may not hold — the ERP only reconciles when a field it mirrors actually moved.
    [Description("Names of the properties that changed in this update.")]
    public IList<string> ChangedFields { get; set; } = [];
}
