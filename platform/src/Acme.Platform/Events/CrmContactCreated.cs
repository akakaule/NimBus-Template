using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NimBus.Core.Events;
using NimBus.Core.Messages.PII;

namespace Acme.Platform.Events;

/// <summary>
/// A person was attached to a CRM account.
/// </summary>
/// <remarks>
/// The PII carrier of this catalog. Operators without the PiiReader role see the annotated
/// values masked in the management UI while the operational fields (account, role, opt-in)
/// stay readable, so a failed message can still be triaged without access to personal data.
/// </remarks>
[Description("Published when a contact person is attached to a CRM account.")]
[SessionKey(nameof(AccountId))]
public class CrmContactCreated : Event
{
    public static readonly CrmContactCreated Example = new()
    {
        ContactId = Guid.Parse("4d8e0b57-1c9a-4a63-b0f5-6d2c8e19a743"),
        AccountId = Guid.Parse("9c1a6f2e-4c31-4f0e-9a1c-2f7d5b3e8a10"),
        FullName = "Mette Sørensen",
        Email = "mette.sorensen@example.com",
        Phone = "+4520304050",
        NationalIdentificationNumber = "010190-1234",
        Role = "Accounts payable",
        MarketingOptIn = false,
    };

    [Required]
    [Description("Stable CRM identifier for the contact.")]
    public Guid ContactId { get; set; }

    [Required]
    [Description("The account this contact belongs to. Session key, so contact and account messages stay ordered together.")]
    public Guid AccountId { get; set; }

    // PartialReveal keeps enough of the name for an operator to recognise the record in a
    // failure list without exposing the full identity.
    [Sensitive(Mode = MaskMode.PartialReveal, Reveal = 3)]
    [Required]
    [Description("The contact's full name.")]
    public string FullName { get; set; } = string.Empty;

    // Hash keeps the value correlatable across records — the same address always produces
    // the same hash — so repeat failures for one person are still spottable unmasked.
    [Sensitive(Mode = MaskMode.Hash)]
    [Required]
    [EmailAddress]
    [Description("Business email address used for integration notifications.")]
    public string Email { get; set; } = string.Empty;

    // The last four digits are what a caller reads out, and matching them is the whole
    // support workflow; the rest of the line has no operational use.
    [Sensitive(Mode = MaskMode.PartialReveal, Reveal = 4)]
    [Description("Direct phone number.")]
    public string Phone { get; set; } = string.Empty;

    // Full redaction: there is no triage question this value answers.
    [Sensitive]
    [Description("National identification number, captured only where invoicing regulation requires it.")]
    public string NationalIdentificationNumber { get; set; } = string.Empty;

    [Description("The contact's function at the account, e.g. 'Accounts payable'.")]
    public string Role { get; set; } = string.Empty;

    [Description("Whether the contact accepted marketing communication.")]
    public bool MarketingOptIn { get; set; }
}
