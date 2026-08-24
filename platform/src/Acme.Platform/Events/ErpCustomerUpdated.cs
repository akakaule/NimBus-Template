using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NimBus.Core.Events;

namespace Acme.Platform.Events;

/// <summary>
/// Commercial terms on an ERP customer record changed.
/// </summary>
[Description("Published when credit limit, currency or payment terms change on an ERP customer.")]
[SessionKey(nameof(AccountId))]
public class ErpCustomerUpdated : Event
{
    public static readonly ErpCustomerUpdated Example = new()
    {
        AccountId = Guid.Parse("9c1a6f2e-4c31-4f0e-9a1c-2f7d5b3e8a10"),
        CustomerNumber = "C-100482",
        CreditLimit = 400000m,
        CurrencyCode = "DKK",
        PaymentTerms = "NET45",
        OnCreditHold = false,
    };

    [Required]
    [Description("The CRM account behind this customer record.")]
    public Guid AccountId { get; set; }

    [Required]
    [Description("The ERP's customer number.")]
    public string CustomerNumber { get; set; } = string.Empty;

    [Description("Approved credit limit after the change, in CurrencyCode.")]
    public decimal CreditLimit { get; set; }

    [Required]
    [Description("ISO 4217 currency the credit limit is denominated in.")]
    public string CurrencyCode { get; set; } = string.Empty;

    [Description("Agreed payment terms after the change, e.g. 'NET45'.")]
    public string PaymentTerms { get; set; } = string.Empty;

    // The outcome side of PlaceCustomerOnCreditHold: the command instructs, this reports what
    // the ERP actually did, and the CRM reflects it on the account.
    [Description("Whether the customer is currently blocked from new orders.")]
    public bool OnCreditHold { get; set; }
}
