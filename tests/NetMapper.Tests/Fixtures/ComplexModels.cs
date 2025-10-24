using System;
using System.Collections.Generic;

namespace NetMapper.Tests.Fixtures;

// Nested object test models
public sealed class Address
{
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
}

public sealed class ContactInfo
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public sealed class Customer
{
    public Guid CustomerId { get; set; }
    public string? Name { get; set; }
    public Address? PrimaryAddress { get; set; }
    public ContactInfo? Contact { get; set; }
    public List<string> Tags { get; set; } = new();
}

public sealed class PartnerAddress
{
    public string? StreetAddress { get; set; }
    public string? CityName { get; set; }
    public string? Zip { get; set; }
}

public sealed class PartnerContact
{
    public string? EmailAddress { get; set; }
    public string? PhoneNumber { get; set; }
}

public sealed class PartnerCustomer
{
    public string? Id { get; set; }
    public string? FullName { get; set; }
    public PartnerAddress? Address { get; set; }
    public PartnerContact? ContactDetails { get; set; }
    public List<string> Labels { get; set; } = new();
}

// Collection test models
public sealed class Order
{
    public int OrderId { get; set; }
    public string? CustomerName { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

public sealed class OrderItem
{
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public sealed class PartnerOrder
{
    public string? OrderNumber { get; set; }
    public string? Customer { get; set; }
    public List<PartnerOrderLine> Lines { get; set; } = new();
}

public sealed class PartnerOrderLine
{
    public string? Product { get; set; }
    public int Qty { get; set; }
    public double UnitPrice { get; set; }
}
