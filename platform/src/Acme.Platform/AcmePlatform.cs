using Acme.Platform.Endpoints;

namespace Acme.Platform;

/// <summary>
/// The Acme integration catalog: which systems are connected, and which events flow
/// between them.
/// </summary>
/// <remarks>
/// This type is the whole product of this repository. `nb` reflection-loads it out of the
/// published package to provision the Service Bus topology (one topic per endpoint, one
/// subscription per consumed event type) and the management WebApp renders Endpoints, Event
/// Types and PII masking from it. Adding an endpoint or an event here is therefore a change
/// to the deployed topology — which is why the package version is pinned explicitly by the
/// deployment rather than floated.
/// </remarks>
// The base type is fully qualified because this assembly's own namespace ends in
// '.Platform', which shadows the unqualified NimBus.Core.Platform.
public class AcmePlatform : NimBus.Core.Platform
{
    public AcmePlatform()
    {
        AddEndpoint(new CrmEndpoint());
        AddEndpoint(new ErpEndpoint());
        AddEndpoint(new DataPlatformEndpoint());
        AddEndpoint(new PartnerPortalEndpoint());
    }
}
