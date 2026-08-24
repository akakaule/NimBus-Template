using NimBus.Core.Endpoints;
using Acme.Platform.Events;

namespace Acme.Platform.Endpoints;

/// <summary>
/// The ERP's integration endpoint — system of record for commercial terms and credit.
/// </summary>
public class ErpEndpoint : Endpoint
{
    public ErpEndpoint()
    {
        Produces<ErpCustomerCreated>();
        Produces<ErpCustomerUpdated>();

        Consumes<CrmAccountCreated>();
        Consumes<CrmAccountUpdated>();
        Consumes<ErpCreditCheckRequested>();

        // The single consumer of the credit-hold command. Adding a second one anywhere in
        // this catalog fails the build (PlatformValidation.EnsureCommandConsumers).
        Consumes<PlaceCustomerOnCreditHold>();
    }

    public override ISystem System => new ErpSystem();

    public override string Description =>
        "ERP adapter. System of record for customer numbers, credit limits and payment terms: mirrors CRM accounts, answers credit checks, and applies credit holds.";
}

internal sealed class ErpSystem : ISystem
{
    public string SystemId => "Erp";
}
