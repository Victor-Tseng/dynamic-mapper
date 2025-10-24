using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace NetMapper.Core;

/// <summary>
/// Configuration options for a specific target member.
/// </summary>
public sealed class MemberConfiguration<TSource, TTarget, TMember> : IMemberConfigurationInternal
{
    private Func<TSource?, object?>? _resolver;
    private bool _isIgnored;

    internal MemberConfiguration(PropertyInfo targetProperty)
    {
        TargetProperty = targetProperty ?? throw new ArgumentNullException(nameof(targetProperty));
    }

    internal PropertyInfo TargetProperty { get; }

    internal bool IsIgnored => _isIgnored;

    internal Func<TSource?, object?>? Resolver => _resolver;
    
    // IMemberConfigurationInternal implementation
    bool IMemberConfigurationInternal.IsIgnored => _isIgnored;

    object? IMemberConfigurationInternal.ResolveValue(object? source)
    {
        if (_resolver == null)
        {
            return null;
        }
        
        return _resolver((TSource?)source);
    }

    /// <summary>
    /// Ignores this member during mapping.
    /// </summary>
    public MemberConfiguration<TSource, TTarget, TMember> Ignore()
    {
        _isIgnored = true;
        return this;
    }

    /// <summary>
    /// Maps this member from a custom source value resolver.
    /// </summary>
    public MemberConfiguration<TSource, TTarget, TMember> MapFrom(Func<TSource?, object?> resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        return this;
    }

    /// <summary>
    /// Maps this member from a source property expression.
    /// </summary>
    public MemberConfiguration<TSource, TTarget, TMember> MapFrom<TSourceMember>(Expression<Func<TSource?, TSourceMember>> expression)
    {
        if (expression == null)
        {
            throw new ArgumentNullException(nameof(expression));
        }

        var compiled = expression.Compile();
        _resolver = source => compiled(source);
        return this;
    }
}
