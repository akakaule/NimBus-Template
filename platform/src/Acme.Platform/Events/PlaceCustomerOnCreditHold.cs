using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NimBus.Core.Events;

namespace Acme.Platform.Events;

/// <summary>
/// Instructs the ERP to block a customer from placing new orders.
/// </summary>
/// <remarks>
/// A <see cref="Command"/>: it instructs one system to act, so the catalog must declare
/// exactly one consuming endpoint. Zero consumers would dead-letter every send and two would
/// silently turn the instruction into a broadcast — <c>PlatformValidation</c> enforces the
/// rule at provisioning time, and this repo's tests enforce it before the package publishes.
/// </remarks>
[Description("Instructs the ERP to block a customer from placing new orders.")]
[SessionKey(nameof(AccountId))]
public class PlaceCustomerOnCreditHold : Command
{
    public static readonly PlaceCustomerOnCreditHold Example = new()
    {
        AccountId = Guid.Parse("9c1a6f2e-4c31-4f0e-9a1c-2f7d5b3e8a10"),
        Reason = "Two invoices more than 60 days overdue.",
        RequestedBy = "credit.control@example.com",
    };

    [Required]
    [Description("The account to place on hold.")]
    public Guid AccountId { get; set; }

    [Required]
    [Description("Why the hold was requested; recorded on the ERP customer and shown in the audit trail.")]
    public string Reason { get; set; } = string.Empty;

    [Required]
    [Description("Who requested the hold, for the audit trail.")]
    public string RequestedBy { get; set; } = string.Empty;
}
