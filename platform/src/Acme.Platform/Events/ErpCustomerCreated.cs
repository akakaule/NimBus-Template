using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NimBus.Core.Events;

namespace Acme.Platform.Events;

/// <summary>
/// The ERP created its own customer record for a CRM account.
/// </summary>
/// <remarks>
/// The acknowledgement half of the CRM-to-ERP flow: it carries the ERP's customer number
/// back so the CRM can store the cross-reference instead of guessing it.
/// </remarks>
[Description("Published when the ERP has created a customer record for a CRM account.")]
[SessionKey(nameof(AccountId))]
public class ErpCustomerCreated : Event
{
    public static readonly ErpCustomerCreated Example = new()
    {
        AccountId = Guid.Parse("9c1a6f2e-4c31-4f0e-9a1c-2f7d5b3e8a10"),
        CustomerNumber = "C-100482",
        CreditLimit = 250000m,
        CurrencyCode = "DKK",
        PaymentTerms = "NET30",
    };

    [Required]
    [Description("The CRM account this customer record was created for.")]
    public Guid AccountId { get; set; }

    [Required]
    [Description("The ERP's own customer number — the cross-reference the CRM stores.")]
    public string CustomerNumber { get; set; } = string.Empty;

    [Description("Approved credit limit, in CurrencyCode.")]
    public decimal CreditLimit { get; set; }

    [Required]
    [Description("ISO 4217 currency the credit limit is denominated in.")]
    public string CurrencyCode { get; set; } = string.Empty;

    [Description("Agreed payment terms, e.g. 'NET30'.")]
    public string PaymentTerms { get; set; } = string.Empty;
}
