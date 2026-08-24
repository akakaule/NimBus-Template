#pragma warning disable CA1707, CA2007

using System.ComponentModel;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NimBus.Core.Events;
using NimBus.Core.Messages.PII;
using Acme.Platform.Events;

namespace Acme.Platform.Tests;

/// <summary>
/// Conventions every event in this catalog has to honour.
/// </summary>
/// <remarks>
/// These are the properties the NimBus management UI and the Resolver rely on: a session key
/// for ordering, a description for the operator reading a failed message, and an Example the
/// "generate fake data" resubmit flow seeds from.
/// </remarks>
[TestClass]
public class EventContractTests
{
    private static readonly Type[] EventTypes = typeof(AcmePlatform).Assembly
        .GetTypes()
        .Where(type => type.IsClass && !type.IsAbstract && typeof(Event).IsAssignableFrom(type))
        .OrderBy(type => type.Name, StringComparer.Ordinal)
        .ToArray();

    [TestMethod]
    public void Catalog_defines_the_expected_event_types()
    {
        // Event type ids are the routing keys — subscription rules filter on them — so the
        // set is pinned rather than counted.
        CollectionAssert.AreEqual(
            new[]
            {
                nameof(CrmAccountCreated),
                nameof(CrmAccountUpdated),
                nameof(CrmContactCreated),
                nameof(ErpCreditCheckRequested),
                nameof(ErpCustomerCreated),
                nameof(ErpCustomerUpdated),
                nameof(PartnerLeadSubmitted),
                nameof(PlaceCustomerOnCreditHold),
            },
            EventTypes.Select(type => type.Name).ToArray());
    }

    [TestMethod]
    public void Every_event_declares_a_session_key_that_resolves()
    {
        // GetSessionId() throws at runtime when [SessionKey] names a property that does not
        // exist — a rename away from a message that cannot be sent at all.
        foreach (var type in EventTypes)
        {
            var attribute = type.GetCustomAttribute<SessionKeyAttribute>();
            Assert.IsNotNull(attribute, $"{type.Name} has no [SessionKey]; ordering would fall back to a random session per message.");

            var instance = (Event)Activator.CreateInstance(type)!;
            var sessionId = instance.GetSessionId();

            Assert.IsFalse(string.IsNullOrWhiteSpace(sessionId), $"{type.Name} resolved an empty session id.");
        }
    }

    [TestMethod]
    public void Every_event_is_described_for_the_operator()
    {
        foreach (var type in EventTypes)
        {
            // Fully qualified: MSTest ships a DescriptionAttribute of its own.
            var description = type.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
            Assert.IsNotNull(description, $"{type.Name} has no [Description].");
            Assert.IsFalse(string.IsNullOrWhiteSpace(description.Description), $"{type.Name} has an empty [Description].");
        }
    }

    [TestMethod]
    public void Every_event_exposes_an_Example_of_its_own_type()
    {
        // The WebApp's fake-payload generator seeds from the authored Example and falls back
        // to it when randomisation fails validation, so a missing one degrades the UI.
        foreach (var type in EventTypes)
        {
            var example = type.GetField("Example", BindingFlags.Public | BindingFlags.Static);

            Assert.IsNotNull(example, $"{type.Name} has no public static Example field.");
            Assert.AreEqual(type, example.FieldType, $"{type.Name}.Example is not a {type.Name}.");
            Assert.IsNotNull(example.GetValue(null), $"{type.Name}.Example is null.");
        }
    }

    [TestMethod]
    public void Every_Example_passes_its_own_validation()
    {
        // An Example that violates its own DataAnnotations is a broken template for every
        // resubmit that starts from it.
        foreach (var type in EventTypes)
        {
            var example = (Event)type.GetField("Example", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
            var result = example.TryValidate();

            Assert.IsTrue(
                result.IsValid,
                $"{type.Name}.Example is invalid: {string.Join("; ", result.ValidationResults.Select(validation => validation.ErrorMessage))}");
        }
    }

    [TestMethod]
    public void Contact_details_are_masked_by_the_mode_that_fits_them()
    {
        // The PII carrier of this catalog. Asserting the modes — not merely the presence of
        // [Sensitive] — keeps a well-meaning "make it readable" change from quietly widening
        // what an operator without the PiiReader role can see.
        AssertMask<CrmContactCreated>(nameof(CrmContactCreated.Email), MaskMode.Hash);
        AssertMask<CrmContactCreated>(nameof(CrmContactCreated.Phone), MaskMode.PartialReveal);
        AssertMask<CrmContactCreated>(nameof(CrmContactCreated.NationalIdentificationNumber), MaskMode.Redact);
        AssertMask<PartnerLeadSubmitted>(nameof(PartnerLeadSubmitted.ContactEmail), MaskMode.Hash);
    }

    [TestMethod]
    public void Operational_fields_stay_readable()
    {
        // Masking has to leave enough behind to triage a failure without PII access.
        foreach (var property in new[] { nameof(CrmContactCreated.AccountId), nameof(CrmContactCreated.Role), nameof(CrmContactCreated.MarketingOptIn) })
        {
            var sensitive = typeof(CrmContactCreated).GetProperty(property)!.GetCustomAttribute<SensitiveAttribute>();
            Assert.IsNull(sensitive, $"CrmContactCreated.{property} is masked; an operator could not triage without PiiReader.");
        }
    }

    private static void AssertMask<TEvent>(string propertyName, MaskMode expected)
    {
        var attribute = typeof(TEvent).GetProperty(propertyName)!.GetCustomAttribute<SensitiveAttribute>();

        Assert.IsNotNull(attribute, $"{typeof(TEvent).Name}.{propertyName} carries personal data but has no [Sensitive].");
        Assert.AreEqual(expected, attribute.Mode, $"{typeof(TEvent).Name}.{propertyName} is masked as {attribute.Mode}, expected {expected}.");
    }
}
