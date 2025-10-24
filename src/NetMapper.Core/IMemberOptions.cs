using System;
using System.Linq.Expressions;

namespace NetMapper.Core;

/// <summary>
/// Provides configuration options for a specific target member.
/// </summary>
public interface IMemberOptions<TSource, TTarget>
{
    /// <summary>
    /// Ignores the target member during mapping.
    /// </summary>
    void Ignore();

    /// <summary>
    /// Specifies a custom value resolver for the target member.
    /// </summary>
    /// <param name="resolver">A function to resolve the value from the source object.</param>
    void MapFrom(Func<TSource, object?> resolver);

    /// <summary>
    /// Specifies a custom value resolver expression for the target member.
    /// </summary>
    /// <param name="expression">An expression to resolve the value from the source object.</param>
    void MapFrom<TMember>(Expression<Func<TSource, TMember>> expression);
}
