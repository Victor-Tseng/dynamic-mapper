# Dynamic Mapper

## Overview

The Dynamic Mapper enables translation between internal domain models and partner-specific message contracts at runtime. A mapping registry stores bidirectional converters that can be extended through profiles. Consumers compose profiles inside a `MapperConfiguration`, build a mapper, and execute conversions as needed.

## Core Concepts

- **DynamicMapper** implements `IMappingRegistry` and `IDynamicMapper`. It stores mapping definitions and performs runtime conversions.
- **MappingBuilder** provides a fluent API for configuring forward and reverse conversions, either via explicit delegates, auto-mapping between similar property sets, or fine-grained member configuration.
- **MappingContext** carries optional metadata (for example default currencies or formatting rules) that mapping delegates can read.
- **MapperConfiguration** aggregates registrations and profiles, producing an `IDynamicMapper` instance.
- **IMappingProfile** represents an extension point for packaging related mappings together.

## Registering Mappings

### Basic Mapping

```csharp
var configuration = new MapperConfiguration()
    .AddRegistration(registry =>
    {
        registry.Register<InternalModel, PartnerModel>(builder =>
        {
            builder.MapForward(source => new PartnerModel { /* map fields */ })
                   .MapReverse(target => new InternalModel { /* map fields */ });
        });
    });

var mapper = configuration.BuildMapper();
```

### Auto-Mapping

Use `MappingBuilder.AutoMap` to copy matching properties automatically:

```csharp
registry.Register<InternalDto, PartnerDto>(builder =>
{
    builder.AutoMap(new Dictionary<string, string>
    {
        ["InternalId"] = "PartnerCode"
    });
});
```

Auto-mapping works case-insensitively and attempts simple type conversions (for example `int` to `double`). Supply a property map to rename members.

**Advanced Features:**

- **Nested Objects**: Automatically maps nested objects when corresponding mappings are registered
- **Collections**: Handles `List<T>`, arrays, and other `IEnumerable<T>` types
- **Bidirectional**: Supports automatic reverse mapping generation

### Fine-Grained Member Configuration

Use `ForMember` to customize individual property mappings:

```csharp
registry.Register<Customer, PartnerCustomer>(builder =>
{
    builder.AutoMap()
        .ForMember(x => x.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
        .ForMember(x => x.InternalId, opt => opt.Ignore());
});
```

**ForMember Options:**

- `MapFrom(func)`: Provide a custom value resolver
- `MapFrom(expression)`: Map from a source property expression
- `Ignore()`: Skip mapping this property

## Complex Data Structure Mapping

### Nested Objects

When mapping types with nested objects, register mappings for both the parent and child types:

```csharp
// Register child mapping
registry.Register<Address, PartnerAddress>(builder =>
{
    builder.AutoMap(new Dictionary<string, string>
    {
        ["Street"] = "StreetAddress",
        ["PostalCode"] = "Zip"
    });
});

// Register parent mapping - nested objects are automatically mapped
registry.Register<Customer, PartnerCustomer>(builder =>
{
    builder.AutoMap(new Dictionary<string, string>
    {
        ["PrimaryAddress"] = "Address"
    });
});
```

### Collections

Collections are automatically mapped when element type mappings are registered:

```csharp
// Register element mapping
registry.Register<OrderItem, PartnerOrderLine>(builder =>
{
    builder.AutoMap(new Dictionary<string, string>
    {
        ["ProductName"] = "Product",
        ["Quantity"] = "Qty"
    });
});

// Parent mapping automatically handles List<OrderItem> → List<PartnerOrderLine>
registry.Register<Order, PartnerOrder>(builder =>
{
    builder.AutoMap(new Dictionary<string, string>
    {
        ["Items"] = "Lines"
    });
});
```

Supported collection types:

- `List<T>`
- `IEnumerable<T>`
- Arrays (`T[]`)
- Other `IEnumerable` implementations

## Mapping Context

Create a `MappingContext` to provide extra configuration to mapping delegates:

```csharp
var context = new MappingContext();
context.Set("partner:date-format", "yyyy/MM/dd");

var partnerModel = mapper.Map<Reservation, PartnerReservation>(reservation, context);
```

The `MappingContext.Empty` singleton is used when no custom context is supplied. Since it is read-only, instantiate a new context before calling `Set`.

## Extending with Profiles

Group related registrations inside a profile:

```csharp
public sealed class ReservationProfile : IMappingProfile
{
    public void Configure(IMappingRegistry registry)
    {
        registry.Register<Reservation, PartnerReservation>(builder =>
        {
            builder.MapForward(/* ... */)
                   .MapReverse(/* ... */);
        });
    }
}

var mapper = new MapperConfiguration()
    .AddProfile<ReservationProfile>()
    .BuildMapper();
```

Profiles make the system modular and ready for partner-specific additions.

## Example Flow

1. **Compose** a `MapperConfiguration` with the required profiles.
2. **Build** an `IDynamicMapper` once at application startup.
3. **Map** models in either direction using the strongly-typed `Map<TSource, TTarget>` API or the runtime `Map(object, Type)` overload.
4. **Augment** behavior by passing a `MappingContext` when partner-specific overrides are necessary.

## Testing

Example xUnit tests are located in `tests/NetMapper.Tests`. They demonstrate:

- Forward and reverse reservation mappings
- Nested object mapping
- Collection mapping
- ForMember customization and ignore
- Error handling when mappings are missing

Run the suite with:

```pwsh
cd d:/Copilot/net-mapper
dotnet test
```

## Implementation Enhancements

### Nested Object Auto-Mapping

**Problem**: The original implementation could only handle simple property mappings, unable to process complex object hierarchies.

**Solution**:

- Extended `AutoMap` to recursively handle nested objects
- When encountering complex type properties, automatically find and apply corresponding registered mappings
- Support arbitrary depth of object nesting

### Collection Mapping Support

**Problem**: Unable to handle conversion of collection types like `List<T>`, arrays, etc.

**Solution**:

- Automatically detect collection types (`IEnumerable<T>`, `List<T>`, `T[]`)
- Apply corresponding mapping rules to each element in the collection
- Support conversion between collection types (e.g., List → Array)

### ForMember Fine-Grained Control

**Problem**: Lack of fine-grained configuration capability for individual properties.

**Solution**:

- Introduced `ForMember` API, inspired by AutoMapper design
- Support `MapFrom()`: Custom value resolver
- Support `Ignore()`: Skip specific property mapping
- Can be combined with `AutoMap()`

### Mapping Execution Logic Refactoring

**Problem**: Original `AutoMap` couldn't access the complete mapping registry, preventing handling of nested and collection mappings.

**Solution**:

- Modified `MappingDefinition` signature to pass `IDynamicMapper` instance
- Mapping execution can access the complete registry, supporting recursive mapping
- Introduced `IMemberConfigurationInternal` interface to simplify member configuration access

**Technical Details**:

```csharp
// Old signature
Func<object?, MappingContext, object?> _forward;

// New signature - passes mapper instance
Func<object?, MappingContext, IDynamicMapper, object?> _forward;
```

## Core Improvement Points

### Property Mapping Logic Optimization

- **Target-Driven**: Changed to iterate through target properties instead of source properties, ensuring ForMember configuration takes priority
- **ResolveSourceName**: New method to handle reverse property mapping lookup
- **Member Configuration Priority**: ForMember > Property mapping dictionary > Default name matching

### Type Conversion Enhancement

```csharp
private static void TryAssign(...)
{
    // 1. Direct type matching
    // 2. Collection mapping
    // 3. Nested object mapping (using registry)
    // 4. Simple type conversion
}
```

### Collection Handling Logic

```csharp
private static object? MapCollection(IEnumerable sourceCollection, Type targetPropertyType, IDynamicMapper mapper, MappingContext context)
{
    // 1. Detect element type
    // 2. Create target collection
    // 3. Apply mapping to each element
    // 4. Convert to target type (List/Array)
}
```

## Alignment with Stakeholder Requirements

### Original Requirements

> "Design and Implement Dynamic Mapper in .NET/C# to translate between our internal data models and partner-specific models. The solution must support both directions of mapping and be extensible for future additions."

### Implementation Status

✅ **Dynamic**: Runtime registration and configuration, context-driven support
✅ **Both Directions**: Native support for MapForward and MapReverse
✅ **Extensible**: Profile system + ForMember API
✅ **Complex Structures**: Nested object and collection support

### Added Value

- Goes beyond original requirements, solving complex data structure mapping problems
- Provides AutoMapper-like developer experience while maintaining dynamic advantages
- Complete test coverage and technical documentation

## Future Recommendations

### Potential Optimization Directions

1. **Performance Optimization**:

   - Cache reflection results
   - Compile expression trees to improve repeated mapping performance
2. **Feature Extensions**:

   - Conditional mapping (`Condition(predicate)`)
   - Value converters (`ConvertUsing<TConverter>()`)
   - Mapping validation (`AssertConfigurationIsValid()`)
   - Projection support (LINQ `ProjectTo<T>()`)
3. **Developer Experience**:

   - More detailed error messages
   - Mapping configuration visualization tools
   - Performance profiler

### Usage Recommendations

- **Suitable Scenarios**: Multi-partner integration, dynamic model conversion, flexible mapping requirements
- **Unsuitable Scenarios**: High-frequency large data conversion (consider AutoMapper), scenarios with fixed mapping rules
