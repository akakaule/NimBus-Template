using NimBus.Core.Endpoints;
using Acme.Platform.Events;

namespace Acme.Platform.Endpoints;

/// <summary>
/// The CRM's integration endpoint — system of record for accounts and contacts.
/// </summary>
public class CrmEndpoint : Endpoint
{
    public CrmEndpoint()
    {
        Produces<CrmAccountCreated>();
        Produces<CrmAccountUpdated>();
        Produces<CrmContactCreated>();

        // Asks the ERP for a credit opinion, and instructs it to act on the answer.
        Produces<ErpCreditCheckRequested>();
        Produces<PlaceCustomerOnCreditHold>();

        // The ERP's acknowledgements: the customer number to cross-reference, and the
        // commercial terms to reflect back on the account.
        Consumes<ErpCustomerCreated>();
        Consumes<ErpCustomerUpdated>();

        // Leads arrive from outside the organisation and are qualified here.
        Consumes<PartnerLeadSubmitted>();
    }

    public override ISystem System => new CrmSystem();

    public override string Description =>
        "CRM adapter. System of record for accounts and contacts: publishes Crm-prefixed events, consumes the ERP's customer acknowledgements, and qualifies partner leads.";
}

internal sealed class CrmSystem : ISystem
{
    public string SystemId => "Crm";
}
