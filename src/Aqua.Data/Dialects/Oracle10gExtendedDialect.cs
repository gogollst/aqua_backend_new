using NHibernate;
using NHibernate.Dialect;
using NHibernate.Dialect.Function;

namespace Aqua.Data.Dialects;

/// <summary>
/// Extends Oracle10gDialect with aqua-specific SQL functions ported from the legacy
/// aquaBackEnd Oracle10gExtendedDialect. DevExpress-dependent members (full-text search
/// filter builder, health-checker, lock-query) have been removed — they belong to the
/// legacy app layer, not the foundation NuGet.
/// </summary>
public class Oracle10gExtendedDialect : Oracle10gDialect, INaturalStringSortingDialect
{
    public Oracle10gExtendedDialect() : base()
    {
        RegisterFunction("week", new SQLFunctionTemplate(NHibernateUtil.Int32, "to_number(to_char(?1, 'iw'))"));
        RegisterFunction("dateonly", new SQLFunctionTemplate(NHibernateUtil.DateTime, "trunc(?1,'DD')"));
        RegisterFunction("monthonly", new SQLFunctionTemplate(NHibernateUtil.DateTime, "trunc(?1,'MM')"));
        RegisterFunction("datalength", new SQLFunctionTemplate(NHibernateUtil.Int32, "length(?1)"));
        RegisterFunction("hour", new SQLFunctionTemplate(NHibernateUtil.DateTime, "EXTRACT (HOUR from ?1)"));
        RegisterFunction("minute", new SQLFunctionTemplate(NHibernateUtil.DateTime, "EXTRACT (MINUTE from ?1)"));

        // 'isodayofweek' is intended to return a week day number (1-7). Monday=1,...,Sunday=7
        RegisterFunction("isodayofweek", new SQLFunctionTemplate(NHibernateUtil.DateTime, "(trunc(?1, 'DD') - trunc(?1, 'IW')+1)"));
        RegisterFunction("dayofmonth", new SQLFunctionTemplate(NHibernateUtil.DateTime, "to_char(?1, 'DD')"));

        // workaround for https://nhibernate.jira.com/projects/NH/issues/NH-3893
        RegisterFunction("lleft", new SQLFunctionTemplate(NHibernateUtil.String, "substr(?1, 1, ?2)"));
        RegisterFunction("rright", new SQLFunctionTemplate(NHibernateUtil.String, "substr(?1, -?2)"));
    }

    /// <summary>
    /// Oracle's TimestampResolutionInTicks: as a default all date-time fields in aqua use TIMESTAMP fields
    /// with a default precision of 6 fractional digits. This means resolution of 10 .NET ticks.
    /// See also RQ040511.
    /// </summary>
    public override long TimestampResolutionInTicks => 10;

    /// <summary>
    /// Returns an ORDER BY expression that applies natural string sorting for the given column.
    /// Delegates to the <c>natural_string_padding</c> database function with Oracle NLS sort (ported from legacy).
    /// </summary>
    public string GetNaturalStringSortExpression(string columnExpression)
        => $"NLSSORT(natural_string_padding({columnExpression}), 'NLS_SORT=BINARY_AI')";
}
