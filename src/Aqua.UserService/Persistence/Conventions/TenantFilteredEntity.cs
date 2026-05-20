namespace Aqua.UserService.Persistence.Conventions;

/// <summary>
/// Marker for entities that participate in the tenant_filter NHibernate filter.
/// Used by reflection-based mapping helpers in tests to assert filter coverage.
/// </summary>
public interface ITenantFilteredEntity
{
    long CustomerId { get; }
}
