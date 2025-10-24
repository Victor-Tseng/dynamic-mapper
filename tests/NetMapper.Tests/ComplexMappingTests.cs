using System;
using System.Collections.Generic;
using System.Linq;
using NetMapper.Core;
using NetMapper.Tests.Fixtures;

namespace NetMapper.Tests;

public class ComplexMappingTests
{
    [Fact]
    public void Maps_Nested_Objects_Automatically()
    {
        var mapper = new MapperConfiguration()
            .AddRegistration(registry =>
            {
                // Register address mapping
                registry.Register<Address, PartnerAddress>(builder =>
                {
                    builder.AutoMap(new Dictionary<string, string>
                    {
                        ["Street"] = "StreetAddress",
                        ["City"] = "CityName",
                        ["PostalCode"] = "Zip"
                    });
                });

                // Register contact info mapping
                registry.Register<ContactInfo, PartnerContact>(builder =>
                {
                    builder.AutoMap(new Dictionary<string, string>
                    {
                        ["Email"] = "EmailAddress",
                        ["Phone"] = "PhoneNumber"
                    });
                });

                // Register customer mapping (with nested objects)
                registry.Register<Customer, PartnerCustomer>(builder =>
                {
                    builder.AutoMap(new Dictionary<string, string>
                    {
                        ["Name"] = "FullName",
                        ["PrimaryAddress"] = "Address",
                        ["Contact"] = "ContactDetails",
                        ["Tags"] = "Labels"
                    });
                });
            })
            .BuildMapper();

        var customer = new Customer
        {
            CustomerId = Guid.NewGuid(),
            Name = "John Doe",
            PrimaryAddress = new Address
            {
                Street = "123 Main St",
                City = "New York",
                PostalCode = "10001"
            },
            Contact = new ContactInfo
            {
                Email = "john@example.com",
                Phone = "+1-555-1234"
            },
            Tags = { "VIP", "Premium" }
        };

        var partner = mapper.Map<Customer, PartnerCustomer>(customer);

        Assert.NotNull(partner);
        Assert.Equal("John Doe", partner!.FullName);
        Assert.NotNull(partner.Address);
        Assert.Equal("123 Main St", partner.Address!.StreetAddress);
        Assert.Equal("New York", partner.Address.CityName);
        Assert.Equal("10001", partner.Address.Zip);
        Assert.NotNull(partner.ContactDetails);
        Assert.Equal("john@example.com", partner.ContactDetails!.EmailAddress);
        Assert.Equal("+1-555-1234", partner.ContactDetails.PhoneNumber);
        Assert.Equal(2, partner.Labels.Count);
        Assert.Contains("VIP", partner.Labels);
        Assert.Contains("Premium", partner.Labels);
    }

    [Fact]
    public void Maps_Collections_Of_Complex_Objects()
    {
        var mapper = new MapperConfiguration()
            .AddRegistration(registry =>
            {
                // Register order item mapping
                registry.Register<OrderItem, PartnerOrderLine>(builder =>
                {
                    builder.AutoMap(new Dictionary<string, string>
                    {
                        ["ProductName"] = "Product",
                        ["Quantity"] = "Qty",
                        ["Price"] = "UnitPrice"
                    });
                });

                // Register order mapping (with collections)
                registry.Register<Order, PartnerOrder>(builder =>
                {
                    builder.AutoMap(new Dictionary<string, string>
                    {
                        ["OrderId"] = "OrderNumber",
                        ["CustomerName"] = "Customer",
                        ["Items"] = "Lines"
                    });
                });
            })
            .BuildMapper();

        var order = new Order
        {
            OrderId = 12345,
            CustomerName = "Jane Smith",
            Items =
            {
                new OrderItem { ProductName = "Widget A", Quantity = 2, Price = 19.99m },
                new OrderItem { ProductName = "Widget B", Quantity = 1, Price = 39.99m }
            }
        };

        var partnerOrder = mapper.Map<Order, PartnerOrder>(order);

        Assert.NotNull(partnerOrder);
        Assert.Equal("12345", partnerOrder!.OrderNumber);
        Assert.Equal("Jane Smith", partnerOrder.Customer);
        Assert.Equal(2, partnerOrder.Lines.Count);
        Assert.Equal("Widget A", partnerOrder.Lines[0].Product);
        Assert.Equal(2, partnerOrder.Lines[0].Qty);
        Assert.Equal(19.99, partnerOrder.Lines[0].UnitPrice);
        Assert.Equal("Widget B", partnerOrder.Lines[1].Product);
    }

    [Fact]
    public void ForMember_Allows_Custom_Property_Mapping()
    {
        var mapper = new MapperConfiguration()
            .AddRegistration(registry =>
            {
                registry.Register<Customer, PartnerCustomer>(builder =>
                {
                    builder.AutoMap()
                        .ForMember(x => x.FullName, opt => opt.MapFrom(src => $"{src!.Name} (ID: {src.CustomerId})"))
                        .ForMember(x => x.Id, opt => opt.MapFrom(src => src!.CustomerId.ToString("N")));
                });
            })
            .BuildMapper();

        var customer = new Customer
        {
            CustomerId = Guid.Parse("12345678-1234-1234-1234-123456789012"),
            Name = "Alice Johnson"
        };

        var partner = mapper.Map<Customer, PartnerCustomer>(customer);

        Assert.NotNull(partner);
        Assert.Equal("Alice Johnson (ID: 12345678-1234-1234-1234-123456789012)", partner!.FullName);
        Assert.Equal("12345678123412341234123456789012", partner.Id);
    }

    [Fact]
    public void ForMember_Ignore_Skips_Property()
    {
        var mapper = new MapperConfiguration()
            .AddRegistration(registry =>
            {
                registry.Register<Customer, PartnerCustomer>(builder =>
                {
                    builder.AutoMap(new Dictionary<string, string>
                    {
                        ["Name"] = "FullName"
                    })
                        .ForMember(x => x.Address, opt => opt.Ignore())
                        .ForMember(x => x.ContactDetails, opt => opt.Ignore());
                });
            })
            .BuildMapper();

        var customer = new Customer
        {
            CustomerId = Guid.NewGuid(),
            Name = "Bob Wilson",
            PrimaryAddress = new Address { Street = "456 Elm St", City = "Boston", PostalCode = "02101" },
            Contact = new ContactInfo { Email = "bob@example.com", Phone = "+1-555-5678" }
        };

        var partner = mapper.Map<Customer, PartnerCustomer>(customer);

        Assert.NotNull(partner);
        Assert.Equal("Bob Wilson", partner!.FullName);
        Assert.Null(partner.Address);
        Assert.Null(partner.ContactDetails);
    }

    [Fact]
    public void Reverse_Mapping_Works_With_Nested_Objects()
    {
        var mapper = new MapperConfiguration()
            .AddRegistration(registry =>
            {
                registry.Register<Address, PartnerAddress>(builder =>
                {
                    builder.AutoMap(new Dictionary<string, string>
                    {
                        ["Street"] = "StreetAddress",
                        ["City"] = "CityName",
                        ["PostalCode"] = "Zip"
                    });
                });

                registry.Register<Customer, PartnerCustomer>(builder =>
                {
                    builder.AutoMap(new Dictionary<string, string>
                    {
                        ["Name"] = "FullName",
                        ["PrimaryAddress"] = "Address"
                    });
                });
            })
            .BuildMapper();

        var partnerCustomer = new PartnerCustomer
        {
            Id = Guid.NewGuid().ToString("N"),
            FullName = "Charlie Brown",
            Address = new PartnerAddress
            {
                StreetAddress = "789 Oak Ave",
                CityName = "Chicago",
                Zip = "60601"
            }
        };

        var customer = mapper.Map<PartnerCustomer, Customer>(partnerCustomer);

        Assert.NotNull(customer);
        Assert.Equal("Charlie Brown", customer!.Name);
        Assert.NotNull(customer.PrimaryAddress);
        Assert.Equal("789 Oak Ave", customer.PrimaryAddress!.Street);
        Assert.Equal("Chicago", customer.PrimaryAddress.City);
        Assert.Equal("60601", customer.PrimaryAddress.PostalCode);
    }
}
