using System;
using System.Collections.Generic;

namespace NetMapper.Core;

/// <summary>
/// Aggregates mapping profiles and builds a mapper instance.
/// </summary>
public sealed class MapperConfiguration
{
    private readonly IList<Action<IMappingRegistry>> _registrations = new List<Action<IMappingRegistry>>();

    /// <summary>
    /// Adds a profile instance to the configuration pipeline.
    /// </summary>
    public MapperConfiguration AddProfile(IMappingProfile profile)
    {
        if (profile == null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        _registrations.Add(profile.Configure);
        return this;
    }

    /// <summary>
    /// Adds a profile declared by type.
    /// </summary>
    public MapperConfiguration AddProfile<TProfile>() where TProfile : IMappingProfile, new()
    {
        return AddProfile(new TProfile());
    }

    /// <summary>
    /// Adds an inline registration block that can configure mappings directly.
    /// </summary>
    public MapperConfiguration AddRegistration(Action<IMappingRegistry> registration)
    {
        if (registration == null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        _registrations.Add(registration);
        return this;
    }

    /// <summary>
    /// Materializes the mapper with all configured registrations.
    /// </summary>
    public IDynamicMapper BuildMapper()
    {
        var mapper = new DynamicMapper();
        foreach (var registration in _registrations)
        {
            registration(mapper);
        }

        return mapper;
    }
}
