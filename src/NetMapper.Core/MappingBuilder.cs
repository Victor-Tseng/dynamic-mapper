using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NetMapper.Core;

/// <summary>
/// Fluent API for configuring mapping behavior between two types.
/// </summary>
/// <typeparam name="TSource">Source type.</typeparam>
/// <typeparam name="TTarget">Target type.</typeparam>
public sealed class MappingBuilder<TSource, TTarget>
{
    private Func<TSource?, MappingContext, IDynamicMapper, TTarget?>? _forward;
    private Func<TTarget?, MappingContext, IDynamicMapper, TSource?>? _reverse;
    private IReadOnlyDictionary<string, string>? _forwardPropertyMap;
    private readonly Dictionary<string, object> _memberConfigurations = new(StringComparer.OrdinalIgnoreCase);
    private bool _useAutoMap;

    internal MappingBuilder()
    {
    }

    /// <summary>
    /// Configures the forward mapping function.
    /// </summary>
    public MappingBuilder<TSource, TTarget> MapForward(Func<TSource?, MappingContext, TTarget?> mapper)
    {
        if (mapper == null)
        {
            throw new ArgumentNullException(nameof(mapper));
        }

        _forward = (source, context, _) => mapper(source, context);
        return this;
    }

    /// <summary>
    /// Configures the forward mapping function without a context parameter.
    /// </summary>
    public MappingBuilder<TSource, TTarget> MapForward(Func<TSource?, TTarget?> mapper)
    {
        if (mapper == null)
        {
            throw new ArgumentNullException(nameof(mapper));
        }

        _forward = (source, _, __) => mapper(source);
        return this;
    }

    /// <summary>
    /// Configures the reverse mapping function.
    /// </summary>
    public MappingBuilder<TSource, TTarget> MapReverse(Func<TTarget?, MappingContext, TSource?> mapper)
    {
        if (mapper == null)
        {
            throw new ArgumentNullException(nameof(mapper));
        }

        _reverse = (target, context, _) => mapper(target, context);
        return this;
    }

    /// <summary>
    /// Configures the reverse mapping function without a context parameter.
    /// </summary>
    public MappingBuilder<TSource, TTarget> MapReverse(Func<TTarget?, TSource?> mapper)
    {
        if (mapper == null)
        {
            throw new ArgumentNullException(nameof(mapper));
        }

        _reverse = (target, _, __) => mapper(target);
        return this;
    }

    /// <summary>
    /// Configures fine-grained mapping for a specific target member.
    /// </summary>
    public MappingBuilder<TSource, TTarget> ForMember<TMember>(
        Expression<Func<TTarget, TMember>> targetMember,
        Action<MemberConfiguration<TSource, TTarget, TMember>> configure)
    {
        if (targetMember == null)
        {
            throw new ArgumentNullException(nameof(targetMember));
        }

        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var propertyInfo = ExtractProperty(targetMember);
        var memberConfig = new MemberConfiguration<TSource, TTarget, TMember>(propertyInfo);
        configure(memberConfig);
        _memberConfigurations[propertyInfo.Name] = memberConfig;
        return this;
    }

    /// <summary>
    /// Configures automatic property matching between types. Matching is case-insensitive and
    /// supports simple type conversion, nested objects, and collections.
    /// </summary>
    /// <param name="propertyMap">Optional explicit property mapping from source property name to target property name.</param>
    /// <param name="includeReverse">When true, registers a symmetric reverse map using the supplied property map.</param>
    public MappingBuilder<TSource, TTarget> AutoMap(IReadOnlyDictionary<string, string>? propertyMap = null, bool includeReverse = true)
    {
        _forwardPropertyMap = WrapWithComparer(propertyMap);
        _useAutoMap = true;

        if (includeReverse)
        {
            IReadOnlyDictionary<string, string>? reverseMap = null;

            if (_forwardPropertyMap != null)
            {
                var buffer = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var pair in _forwardPropertyMap)
                {
                    buffer[pair.Value] = pair.Key;
                }

                reverseMap = buffer;
            }

            _reverse = (target, context, mapper) => AutoMapInternal<TTarget, TSource>(target, context, mapper, reverseMap, null);
        }

        return this;
    }

    internal MappingDefinition Build()
    {
        if (_forward == null && !_useAutoMap)
        {
            throw new InvalidOperationException($"No forward mapping configured for {typeof(TSource).Name} -> {typeof(TTarget).Name}.");
        }

        Func<object?, MappingContext, IDynamicMapper, object?> forwardFunc;
        
        if (_useAutoMap)
        {
            // When AutoMap is used, always apply member configurations at build time
            forwardFunc = (input, context, mapper) => AutoMapInternal<TSource, TTarget>((TSource?)input, context ?? MappingContext.Empty, mapper, _forwardPropertyMap, _memberConfigurations);
        }
        else if (_forward != null)
        {
            forwardFunc = (input, context, mapper) => _forward((TSource?)input, context ?? MappingContext.Empty, mapper);
        }
        else
        {
            throw new InvalidOperationException($"No forward mapping configured for {typeof(TSource).Name} -> {typeof(TTarget).Name}.");
        }

        return new MappingDefinition(
            typeof(TSource),
            typeof(TTarget),
            forwardFunc,
            _reverse == null
                ? null
                : (input, context, mapper) => _reverse((TTarget?)input, context ?? MappingContext.Empty, mapper));
    }

    private static IReadOnlyDictionary<string, string>? WrapWithComparer(IReadOnlyDictionary<string, string>? propertyMap)
    {
        if (propertyMap == null)
        {
            return null;
        }

        if (propertyMap is Dictionary<string, string> dictionary && dictionary.Comparer == StringComparer.OrdinalIgnoreCase)
        {
            return dictionary;
        }

        var buffer = new Dictionary<string, string>(propertyMap.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in propertyMap)
        {
            buffer[pair.Key] = pair.Value;
        }

        return buffer;
    }

    private static PropertyInfo ExtractProperty<TType, TMember>(Expression<Func<TType, TMember>> expression)
    {
        if (expression.Body is not MemberExpression memberExpression || memberExpression.Member is not PropertyInfo propertyInfo)
        {
            throw new ArgumentException("Expression must be a property accessor.", nameof(expression));
        }

        return propertyInfo;
    }

    private static TOutput? AutoMapInternal<TInput, TOutput>(
        TInput? source,
        MappingContext context,
        IDynamicMapper mapper,
        IReadOnlyDictionary<string, string>? propertyMap,
        Dictionary<string, object>? memberConfigurations)
    {
        if (source == null)
        {
            return default;
        }

        var target = CreateInstance<TOutput>();
        var targetProperties = GetWritableProperties(typeof(TOutput));
        var sourcePropertyLookup = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var prop in typeof(TInput).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (prop.CanRead)
            {
                sourcePropertyLookup[prop.Name] = prop;
            }
        }

        // Process each target property
        foreach (var targetProperty in targetProperties)
        {
            // Check for member configuration first
            if (memberConfigurations != null && memberConfigurations.TryGetValue(targetProperty.Name, out var configObj))
            {
                if (configObj is IMemberConfigurationInternal config)
                {
                    if (config.IsIgnored)
                    {
                        continue;
                    }

                    var customValue = config.ResolveValue(source);
                    TryAssign(target, targetProperty, customValue, mapper, context);
                    continue;
                }
            }

            // Try to find matching source property
            var sourceName = ResolveSourceName(targetProperty.Name, propertyMap);
            if (sourceName != null && sourcePropertyLookup.TryGetValue(sourceName, out var sourceProperty))
            {
                var value = sourceProperty.GetValue(source);
                TryAssign(target, targetProperty, value, mapper, context);
            }
        }

        return target;
    }

    private static string? ResolveSourceName(string targetName, IReadOnlyDictionary<string, string>? propertyMap)
    {
        if (propertyMap == null)
        {
            return targetName;
        }

        // Look for reverse mapping (target -> source)
        foreach (var pair in propertyMap)
        {
            if (pair.Value.Equals(targetName, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Key;
            }
        }

        // Fallback to same name
        return targetName;
    }

    private static List<PropertyInfo> GetWritableProperties(Type type)
    {
        var result = new List<PropertyInfo>();
        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.CanWrite)
            {
                result.Add(property);
            }
        }

        return result;
    }

    private static TInstance CreateInstance<TInstance>()
    {
        try
        {
            return Activator.CreateInstance<TInstance>() ?? throw new InvalidOperationException($"Unable to create instance of {typeof(TInstance).Name}.");
        }
        catch (MissingMethodException ex)
        {
            throw new InvalidOperationException($"Type {typeof(TInstance).Name} must provide a parameterless constructor for auto-mapping.", ex);
        }
    }

    private static void TryAssign<TInstance>(TInstance target, PropertyInfo targetProperty, object? value, IDynamicMapper mapper, MappingContext context)
    {
        var propertyType = targetProperty.PropertyType;
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (value == null)
        {
            if (!underlyingType.IsValueType || propertyType != underlyingType)
            {
                targetProperty.SetValue(target, null);
            }

            return;
        }

        // Check if value is already the correct type
        if (underlyingType.IsInstanceOfType(value))
        {
            targetProperty.SetValue(target, value);
            return;
        }

        // Handle collections
        if (IsCollection(propertyType) && value is IEnumerable sourceCollection)
        {
            var mappedCollection = MapCollection(sourceCollection, propertyType, mapper, context);
            if (mappedCollection != null)
            {
                targetProperty.SetValue(target, mappedCollection);
                return;
            }
        }

        // Try to map nested objects using registered mappings
        var valueType = value.GetType();
        if (!IsPrimitiveType(valueType) && !IsPrimitiveType(underlyingType))
        {
            try
            {
                var mapped = mapper.Map(value, underlyingType, context);
                targetProperty.SetValue(target, mapped);
                return;
            }
            catch
            {
                // Fall through to type conversion
            }
        }

        // Try simple type conversion
        try
        {
            var converted = Convert.ChangeType(value, underlyingType);
            targetProperty.SetValue(target, converted);
        }
        catch
        {
            // Silently skip values that cannot be converted to avoid partial failures.
        }
    }

    private static bool IsCollection(Type type)
    {
        if (type == typeof(string))
        {
            return false;
        }

        return typeof(IEnumerable).IsAssignableFrom(type);
    }

    private static bool IsPrimitiveType(Type type)
    {
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || 
               type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) ||
               type == typeof(Guid);
    }

    private static object? MapCollection(IEnumerable sourceCollection, Type targetPropertyType, IDynamicMapper mapper, MappingContext context)
    {
        var elementType = GetCollectionElementType(targetPropertyType);
        if (elementType == null)
        {
            return null;
        }

        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = (IList)Activator.CreateInstance(listType)!;

        foreach (var item in sourceCollection)
        {
            if (item == null)
            {
                continue;
            }

            var itemType = item.GetType();
            if (elementType.IsAssignableFrom(itemType))
            {
                list.Add(item);
            }
            else
            {
                try
                {
                    var mapped = mapper.Map(item, elementType, context);
                    if (mapped != null)
                    {
                        list.Add(mapped);
                    }
                }
                catch
                {
                    // Skip items that can't be mapped
                }
            }
        }

        // Convert list to target type if needed
        if (targetPropertyType.IsArray)
        {
            var array = Array.CreateInstance(elementType, list.Count);
            list.CopyTo(array, 0);
            return array;
        }

        return list;
    }

    private static Type? GetCollectionElementType(Type collectionType)
    {
        if (collectionType.IsArray)
        {
            return collectionType.GetElementType();
        }

        if (collectionType.IsGenericType)
        {
            var genericArgs = collectionType.GetGenericArguments();
            if (genericArgs.Length == 1)
            {
                return genericArgs[0];
            }
        }

        foreach (var interfaceType in collectionType.GetInterfaces())
        {
            if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return interfaceType.GetGenericArguments()[0];
            }
        }

        return null;
    }
}
