using System;

namespace NetMapper.Core;

/// <summary>
/// Exposes mapping operations for translating between runtime models.
/// </summary>
public interface IDynamicMapper
{
    /// <summary>
    /// Maps the provided <paramref name="source"/> instance into a <typeparamref name="TTarget"/>.
    /// </summary>
    /// <typeparam name="TSource">Source type.</typeparam>
    /// <typeparam name="TTarget">Target type.</typeparam>
    /// <param name="source">Instance to convert; null values result in default target.</param>
    /// <param name="context">Optional contextual information shared across nested maps.</param>
    /// <returns>The mapped target instance.</returns>
    TTarget? Map<TSource, TTarget>(TSource? source, MappingContext? context = null);

    /// <summary>
    /// Maps the provided <paramref name="source"/> instance into the supplied <paramref name="targetType"/>.
    /// </summary>
    /// <param name="source">Instance to convert.</param>
    /// <param name="targetType">Runtime target type.</param>
    /// <param name="context">Optional contextual information shared across nested maps.</param>
    /// <returns>The mapped instance.</returns>
    object? Map(object? source, Type targetType, MappingContext? context = null);
}
