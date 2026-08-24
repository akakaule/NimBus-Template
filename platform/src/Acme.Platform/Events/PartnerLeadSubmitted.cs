using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NimBus.Core.Events;
using NimBus.Core.Messages.PII;

namespace Acme.Platform.Events;

/// <summary>
/// A reseller submitted a lead through the partner portal.
/// </summary>
/// <remarks>
/// Enters the platform from outside the organisation, which is why the portal is modelled as
/// its own endpoint: the lead is untrusted input until the CRM qualifies it, and having a
/// separate producer keeps that boundary visible in the topology instead of hiding it inside
/// the CRM.
/// </remarks>
[Description("Published when a reseller submits a new lead through the partner portal.")]
[SessionKey(nameof(LeadId))]
public class PartnerLeadSubmitted : Event
{
    public static readonly PartnerLeadSubmitted Example = new()
    {
        LeadId = Guid.Parse("b2f7c4a1-9d38-4e52-8c07-1a5e6b90d3f4"),
        PartnerId = "P-2041",
        CompanyName = "Havnegade Consulting ApS",
        ContactEmail = "info@havnegade.example.com",
        CountryCode = "DK",
        EstimatedAnnualValue = 180000m,
    };

    [Required]
    [Description("Identifier for this lead submission. Session key — a lead has no account yet.")]
    public Guid LeadId { get; set; }

    [Required]
    [Description("The reseller that submitted the lead.")]
    public string PartnerId { get; set; } = string.Empty;

    [Required]
    [Description("Company the lead is for.")]
    public string CompanyName { get; set; } = string.Empty;

    // Externally submitted and unverified, but still personal data the moment it arrives.
    [Sensitive(Mode = MaskMode.Hash)]
    [Required]
    [EmailAddress]
    [Description("Contact address supplied with the lead.")]
    public string ContactEmail { get; set; } = string.Empty;

    [Required]
    [Description("ISO 3166-1 alpha-2 country the lead is in.")]
    public string CountryCode { get; set; } = string.Empty;

    [Description("Annual value the partner estimates for the lead, in DKK.")]
    public decimal EstimatedAnnualValue { get; set; }
}
