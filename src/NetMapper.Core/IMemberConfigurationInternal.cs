using System;

namespace NetMapper.Core;

/// <summary>
/// Non-generic interface for member configuration.
/// </summary>
internal interface IMemberConfigurationInternal
{
    bool IsIgnored { get; }
    
    object? ResolveValue(object? source);
}
