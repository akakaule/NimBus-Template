using NimBus.Core.Endpoints;
using Acme.Platform.Events;

namespace Acme.Platform.Endpoints;

/// <summary>
/// The externally-facing reseller portal — the only producer outside the organisation.
/// </summary>
public class PartnerPortalEndpoint : Endpoint
{
    public PartnerPortalEndpoint()
    {
        Produces<PartnerLeadSubmitted>();
    }

    public override ISystem System => new PartnerPortalSystem();

    public override string Description =>
        "Reseller-facing portal. Submits leads into the platform; consumes nothing, so an outage upstream of the CRM cannot block it.";
}

internal sealed class PartnerPortalSystem : ISystem
{
    public string SystemId => "PartnerPortal";
}
