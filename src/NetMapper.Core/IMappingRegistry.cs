using System;

namespace NetMapper.Core;

/// <summary>
/// Describes the capabilities required to register mapping definitions.
/// </summary>
public interface IMappingRegistry : IDynamicMapper
{
    /// <summary>
    /// Registers a mapping between <typeparamref name="TSource"/> and <typeparamref name="TTarget"/>.
    /// </summary>
    /// <typeparam name="TSource">Source type.</typeparam>
    /// <typeparam name="TTarget">Target type.</typeparam>
    /// <param name="configure">Callback used to configure the mapping logic.</param>
    void Register<TSource, TTarget>(Action<MappingBuilder<TSource, TTarget>> configure);
}
