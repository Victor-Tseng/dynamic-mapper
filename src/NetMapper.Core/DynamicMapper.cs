using System;
using System.Collections.Concurrent;

namespace NetMapper.Core;

/// <summary>
/// Runtime mapper capable of translating between registered model pairs.
/// </summary>
public sealed class DynamicMapper : IMappingRegistry
{
    private readonly ConcurrentDictionary<MappingKey, MappingDefinition> _definitions = new();

    /// <inheritdoc />
    public void Register<TSource, TTarget>(Action<MappingBuilder<TSource, TTarget>> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = new MappingBuilder<TSource, TTarget>();
        configure(builder);
        var definition = builder.Build();
        StoreDefinition(definition);

        if (definition.Reverse != null)
        {
            StoreDefinition(definition.CreateReverse());
        }
    }

    /// <inheritdoc />
    public TTarget? Map<TSource, TTarget>(TSource? source, MappingContext? context = null)
    {
        if (source == null)
        {
            return default;
        }

        var key = new MappingKey(typeof(TSource), typeof(TTarget));
        if (!_definitions.TryGetValue(key, out var definition))
        {
            throw new InvalidOperationException($"No mapping registered for {typeof(TSource).Name} -> {typeof(TTarget).Name}.");
        }

        return (TTarget?)definition.Invoke(source, context ?? MappingContext.Empty, this);
    }

    /// <inheritdoc />
    public object? Map(object? source, Type targetType, MappingContext? context = null)
    {
        if (targetType == null)
        {
            throw new ArgumentNullException(nameof(targetType));
        }

        if (source == null)
        {
            return null;
        }

        var sourceType = source.GetType();
        var key = new MappingKey(sourceType, targetType);
        if (!_definitions.TryGetValue(key, out var definition))
        {
            throw new InvalidOperationException($"No mapping registered for {sourceType.Name} -> {targetType.Name}.");
        }

        return definition.Invoke(source, context ?? MappingContext.Empty, this);
    }

    private void StoreDefinition(MappingDefinition definition)
    {
        _definitions[definition.Key] = definition;
    }
}

internal sealed record MappingDefinition
{
    private readonly Func<object?, MappingContext, IDynamicMapper, object?> _forward;
    private readonly Func<object?, MappingContext, IDynamicMapper, object?>? _reverse;

    internal MappingDefinition(Type sourceType, Type targetType, Func<object?, MappingContext, IDynamicMapper, object?> forward, Func<object?, MappingContext, IDynamicMapper, object?>? reverse)
    {
        SourceType = sourceType ?? throw new ArgumentNullException(nameof(sourceType));
        TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        _forward = forward ?? throw new ArgumentNullException(nameof(forward));
        _reverse = reverse;
        Key = new MappingKey(SourceType, TargetType);
    }

    internal MappingKey Key { get; }

    internal Type SourceType { get; }

    internal Type TargetType { get; }

    internal Func<object?, MappingContext, IDynamicMapper, object?>? Reverse => _reverse;

    internal object? Invoke(object? source, MappingContext context, IDynamicMapper mapper)
    {
        return _forward(source, context, mapper);
    }

    internal MappingDefinition CreateReverse()
    {
        if (_reverse == null)
        {
            throw new InvalidOperationException($"Reverse mapping not configured for {SourceType.Name} -> {TargetType.Name}.");
        }

        return new MappingDefinition(TargetType, SourceType, _reverse, _forward);
    }
}

internal readonly struct MappingKey : IEquatable<MappingKey>
{
    private readonly Type _source;
    private readonly Type _target;

    internal MappingKey(Type source, Type target)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _target = target ?? throw new ArgumentNullException(nameof(target));
    }

    public bool Equals(MappingKey other) => _source == other._source && _target == other._target;

    public override bool Equals(object? obj) => obj is MappingKey other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(_source, _target);
}
