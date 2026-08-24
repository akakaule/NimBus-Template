using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NimBus.Core.Events;

namespace Acme.Platform.Events;

/// <summary>
/// The CRM asked the ERP to re-assess an account's credit.
/// </summary>
/// <remarks>
/// Deliberately an event and not a <see cref="Command"/>: a credit assessment is a request
/// for an opinion that more than one system may legitimately act on (the ERP scores it, the
/// data platform records that it was asked), whereas
/// <see cref="PlaceCustomerOnCreditHold"/> instructs exactly one system to change state.
/// </remarks>
[Description("Published when the CRM asks for an account's credit to be re-assessed.")]
[SessionKey(nameof(AccountId))]
public class ErpCreditCheckRequested : Event
{
    public static readonly ErpCreditCheckRequested Example = new()
    {
        AccountId = Guid.Parse("9c1a6f2e-4c31-4f0e-9a1c-2f7d5b3e8a10"),
        RequestedLimit = 400000m,
        CurrencyCode = "DKK",
        Reason = "Customer requested a higher order ceiling before Q4.",
    };

    [Required]
    [Description("The account to assess.")]
    public Guid AccountId { get; set; }

    [Description("The credit limit being asked for, in CurrencyCode.")]
    public decimal RequestedLimit { get; set; }

    [Required]
    [Description("ISO 4217 currency the requested limit is denominated in.")]
    public string CurrencyCode { get; set; } = string.Empty;

    [Description("Why the re-assessment was asked for; shown to the credit controller.")]
    public string Reason { get; set; } = string.Empty;
}
