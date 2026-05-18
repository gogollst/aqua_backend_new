using NHibernate;
using NHibernate.Dialect;
using NHibernate.Dialect.Function;

namespace Aqua.Data.Dialects;

/// <summary>
/// Extends PostgreSQL83Dialect with aqua-specific SQL functions ported from the legacy
/// aquaBackEnd PostgresExtendedDialect. DevExpress-dependent members (full-text search
/// filter builder, health-checker, lock-query) have been removed — they belong to the
/// legacy app layer, not the foundation NuGet.
/// </summary>
public class PostgresExtendedDialect : PostgreSQL83Dialect, INaturalStringSortingDialect
{
    public PostgresExtendedDialect()
        : base()
    {
        // We use our custom function here which (depending on underlying version of sqlserver) either
        // uses isowk date part (>=2008), or calculates manually (<=2005).
        // see also http://blogs.lessthandot.com/index.php/DataMgmt/DataDesign/iso-week-in-sql-server
        RegisterFunction("week", new SQLFunctionTemplate(NHibernateUtil.Int32, "extract(week from ?1)"));
        RegisterFunction("dateonly", new SQLFunctionTemplate(NHibernateUtil.DateTime, "(?1)::date"));
        RegisterFunction("monthonly", new SQLFunctionTemplate(NHibernateUtil.DateTime, "extract(month from ?1)"));
        RegisterFunction("datalength", new SQLFunctionTemplate(NHibernateUtil.Int32, "octet_length(?1)"));
        RegisterFunction("hour", new SQLFunctionTemplate(NHibernateUtil.Int32, "extract(hour from ?1)"));
        RegisterFunction("minute", new SQLFunctionTemplate(NHibernateUtil.Int32, "extract(minute from ?1)"));
        RegisterFunction("contains", new SQLFunctionTemplate(NHibernateUtil.Boolean, "(to_tsvector(?1) @@ to_tsquery(?2))"));

        // 'isodayofweek' is intended to return a week day number (1-7). Monday=1,...,Sunday=7
        RegisterFunction("isodayofweek", new SQLFunctionTemplate(NHibernateUtil.Int32, "extract(isodow from ?1)"));
        RegisterFunction("dayofmonth", new SQLFunctionTemplate(NHibernateUtil.Int32, "extract(day from ?1)"));

        // workaround for https://nhibernate.jira.com/projects/NH/issues/NH-3893
        RegisterFunction("lleft", new SQLFunctionTemplate(NHibernateUtil.String, "left(?1, ?2)"));
        RegisterFunction("rright", new SQLFunctionTemplate(NHibernateUtil.String, "right(?1, ?2)"));
    }

    /// <summary>
    /// Returns an ORDER BY expression that applies natural string sorting for the given column.
    /// Delegates to the <c>natural_string_padding</c> database function (ported from legacy).
    /// </summary>
    public string GetNaturalStringSortExpression(string columnExpression)
        => $"natural_string_padding({columnExpression})";
}
