using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Acme.Platform.Events;

/// <summary>
/// A postal address, carried as a nested object inside an event.
/// </summary>
/// <remarks>
/// Event properties are not limited to scalars. A class like this becomes a nested JSON
/// object in the payload, its members render with their own descriptions in the management
/// UI and the catalog export, and DataAnnotations on them are validated together with the
/// enclosing event. A company's registered address is public record, so nothing here is
/// marked <c>[Sensitive]</c>; put the attribute on the class to mask every member when an
/// address belongs to a person.
/// </remarks>
[Description("A postal address.")]
public class Address
{
    [Required]
    [Description("Street name and number, with any floor or unit designation.")]
    public string Street { get; set; } = string.Empty;

    [Required]
    [Description("Postal code.")]
    public string PostalCode { get; set; } = string.Empty;

    [Required]
    [Description("City or town.")]
    public string City { get; set; } = string.Empty;

    [Required]
    [Description("ISO 3166-1 alpha-2 country code.")]
    public string CountryCode { get; set; } = string.Empty;
}
