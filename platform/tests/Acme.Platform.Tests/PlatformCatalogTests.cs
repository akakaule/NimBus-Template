#pragma warning disable CA1707, CA2007

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NimBus.Core;
using NimBus.Core.Endpoints;
using NimBus.Core.Events;

namespace Acme.Platform.Tests;

/// <summary>
/// Contract tests over the catalog itself.
/// </summary>
/// <remarks>
/// This catalog decides the deployed Service Bus topology: endpoint ids become topic names
/// and consumed event types become subscriptions. A mistake here is not a failing feature,
/// it is messages that route nowhere in production — so the gate runs before the package can
/// be published, not after it is deployed.
/// </remarks>
[TestClass]
public class PlatformCatalogTests
{
    private static readonly IPlatform Platform = new AcmePlatform();

    [TestMethod]
    public void Every_command_has_exactly_one_consumer()
    {
        // The rule NimBus itself enforces at provisioning time. Zero consumers dead-letters
        // every send; two silently turn an instruction into a broadcast.
        var errors = PlatformValidation.ValidateCommandConsumers(Platform);

        // Fully qualified: NimBus.Core.Endpoints also defines an Environment type.
        Assert.AreEqual(0, errors.Count, string.Join(System.Environment.NewLine, errors));
    }

    [TestMethod]
    public void Endpoint_ids_are_the_four_expected_systems()
    {
        // Endpoint.Id is the class name and the topic is named after it, so a rename is a
        // new topic and an orphaned old one. Pinning the names makes that deliberate.
        var ids = Platform.Endpoints.Select(endpoint => endpoint.Id).OrderBy(id => id, StringComparer.Ordinal);

        CollectionAssert.AreEqual(
            new[] { "CrmEndpoint", "DataPlatformEndpoint", "ErpEndpoint", "PartnerPortalEndpoint" },
            ids.ToArray());
    }

    [TestMethod]
    public void Every_endpoint_describes_itself_and_its_system()
    {
        // Both surface in the management UI; an endpoint with neither is unattributable when
        // an operator is looking at a failed message at 2am.
        foreach (var endpoint in Platform.Endpoints)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(endpoint.Description), $"{endpoint.Id} has no Description.");
            Assert.IsNotNull(endpoint.System, $"{endpoint.Id} declares no ISystem.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(endpoint.System.SystemId), $"{endpoint.Id} has an empty SystemId.");
        }
    }

    [TestMethod]
    public void Every_produced_event_has_at_least_one_consumer()
    {
        // Catches the failure mode a typo produces: a Consumes<T>() that never landed leaves
        // a topic with no subscription, and every message published to it is dropped.
        var unconsumed = Platform.EventTypes
            .Where(eventType => !Platform.GetConsumers(eventType).Any())
            .Select(eventType => eventType.Id)
            .ToArray();

        Assert.AreEqual(0, unconsumed.Length, $"Produced with no consumer: {string.Join(", ", unconsumed)}");
    }

    [TestMethod]
    public void Every_consumed_event_has_a_producer()
    {
        // The mirror image: a subscription for something nobody publishes is a silent
        // integration that will never fire, which is worse than one that fails loudly.
        var unproduced = Platform.EventTypes
            .Where(eventType => !Platform.GetProducers(eventType).Any())
            .Select(eventType => eventType.Id)
            .ToArray();

        Assert.AreEqual(0, unproduced.Length, $"Consumed with no producer: {string.Join(", ", unproduced)}");
    }

    [TestMethod]
    public void No_endpoint_consumes_what_it_produces()
    {
        // Self-consumption is a routing loop waiting to happen and is never what was meant.
        foreach (var endpoint in Platform.Endpoints)
        {
            var both = endpoint.EventTypesProduced
                .Select(eventType => eventType.Id)
                .Intersect(endpoint.EventTypesConsumed.Select(eventType => eventType.Id), StringComparer.Ordinal)
                .ToArray();

            Assert.AreEqual(0, both.Length, $"{endpoint.Id} both produces and consumes: {string.Join(", ", both)}");
        }
    }
}
