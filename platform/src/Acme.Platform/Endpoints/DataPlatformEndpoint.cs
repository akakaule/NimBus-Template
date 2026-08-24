using NimBus.Core.Endpoints;
using Acme.Platform.Events;

namespace Acme.Platform.Endpoints;

/// <summary>
/// Analytics sink — subscribes to everything, publishes nothing.
/// </summary>
/// <remarks>
/// A pure consumer is worth modelling explicitly: it makes the fan-out visible in the
/// topology (every event type gets a second subscription) and it is the endpoint that proves
/// adding a reader costs the producers nothing — no producer declares who reads it.
/// </remarks>
public class DataPlatformEndpoint : Endpoint
{
    public DataPlatformEndpoint()
    {
        Consumes<CrmAccountCreated>();
        Consumes<CrmAccountUpdated>();
        Consumes<CrmContactCreated>();
        Consumes<ErpCustomerCreated>();
        Consumes<ErpCustomerUpdated>();
        Consumes<ErpCreditCheckRequested>();
        Consumes<PartnerLeadSubmitted>();

        // PlaceCustomerOnCreditHold is deliberately absent: it is a Command, and a second
        // consumer would turn a single instruction into a broadcast. The hold shows up in
        // analytics through ErpCustomerUpdated.OnCreditHold — the outcome, not the order.
    }

    public override ISystem System => new DataPlatformSystem();

    public override string Description =>
        "Analytics sink. Consumes every business event for the lakehouse; produces nothing, so adding it changed no producer.";
}

internal sealed class DataPlatformSystem : ISystem
{
    public string SystemId => "DataPlatform";
}
