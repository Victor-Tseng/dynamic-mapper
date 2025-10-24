using System;
using System.Collections.Generic;

namespace NetMapper.Core;

/// <summary>
/// Carries contextual information across nested mapping operations.
/// </summary>
public sealed class MappingContext
{
    private readonly Dictionary<string, object?> _items;
    private readonly bool _isReadOnly;

    private MappingContext(Dictionary<string, object?> items, bool isReadOnly)
    {
        _items = items;
        _isReadOnly = isReadOnly;
    }

    /// <summary>
    /// Initializes a writable context instance.
    /// </summary>
    public MappingContext()
        : this(new Dictionary<string, object?>(), false)
    {
    }

    private MappingContext(bool isReadOnly)
        : this(new Dictionary<string, object?>(), isReadOnly)
    {
    }

    /// <summary>
    /// Gets a reusable read-only context shared by default when no custom context is supplied.
    /// </summary>
    public static MappingContext Empty { get; } = new MappingContext(true);

    /// <summary>
    /// Sets a context value for the lifetime of the mapping operation.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when attempting to mutate the shared read-only context.</exception>
    public void Set(string key, object? value)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (_isReadOnly)
        {
            throw new InvalidOperationException("MappingContext.Empty is read-only. Create a new instance to store items.");
        }

        _items[key] = value;
    }

    /// <summary>
    /// Retrieves a context value by key.
    /// </summary>
    public bool TryGet<TValue>(string key, out TValue value)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (_items.TryGetValue(key, out var stored) && stored is TValue casted)
        {
            value = casted;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// Returns an immutable snapshot of the stored values.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Snapshot() => new Dictionary<string, object?>(_items);
}
