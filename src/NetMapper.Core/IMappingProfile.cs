namespace NetMapper.Core;

/// <summary>
/// Declares a reusable mapping profile.
/// </summary>
public interface IMappingProfile
{
    /// <summary>
    /// Applies the profile-specific registrations to the provided registry.
    /// </summary>
    void Configure(IMappingRegistry registry);
}
