# Dynamic Mapper

A Dynamic Mapper for converting between .NET internal models and partner-specific contracts.

## Features

- ✅ **Bidirectional Mapping**: Supports forward and reverse conversions
- ✅ **Auto-Mapping**: Intelligent matching based on property names
- ✅ **Nested Objects**: Automatic handling of complex object hierarchies
- ✅ **Collection Support**: Seamless conversion for List, Array, and IEnumerable
- ✅ **ForMember API**: Fine-grained property-level control
- ✅ **Context-Driven**: Runtime passing of configuration and metadata
- ✅ **Extensible**: Modular organization through Profiles

## Quick Start

1. Restore and build the solution:
   ```pwsh
   dotnet build
   ```
2. Run the reservation mapping tests:
   ```pwsh
   dotnet test
   ```
3. View the architecture documentation `docs/SystemDesign.md`

## Project Structure

- `src/NetMapper.Core` – Mapping engine (`DynamicMapper`, `MappingBuilder`, configuration API)
- `tests/NetMapper.Tests` – xUnit tests covering reservation mappings, nested objects, and collections
- `docs` – Technical documentation and usage guides

## Usage Examples

### Basic Mapping

```csharp
var mapper = new MapperConfiguration()
    .AddProfile(new ReservationMappingProfile())
    .BuildMapper();

var partner = mapper.Map<Reservation, PartnerReservation>(reservation, context);
```

### Nested Objects and Collections

```csharp
var mapper = new MapperConfiguration()
    .AddRegistration(registry =>
    {
        // Register child object mapping
        registry.Register<Address, PartnerAddress>(builder =>
        {
            builder.AutoMap(new Dictionary<string, string>
            {
                ["Street"] = "StreetAddress",
                ["PostalCode"] = "Zip"
            });
        });

        // Parent object automatically handles nested mapping
        registry.Register<Customer, PartnerCustomer>(builder =>
        {
            builder.AutoMap()
                .ForMember(x => x.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));
        });
    })
    .BuildMapper();
```

## Differences from AutoMapper

NetMapper focuses on **runtime dynamic mapping**, suitable for scenarios requiring flexible adaptation to different partner models. Compared to AutoMapper:

- ✅ Stronger dynamism and runtime registration capabilities
- ✅ Native bidirectional mapping support
- ✅ Context-driven mapping decisions
- ⚠️ Relatively simplified functionality (focused on core scenarios)
- ⚠️ Slightly lower performance than compiled mappings (suitable for low-frequency dynamic scenarios)

See the technical documentation for complete comparisons and advanced features.
