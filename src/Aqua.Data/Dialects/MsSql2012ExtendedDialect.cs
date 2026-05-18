using NHibernate;
using NHibernate.Dialect;
using NHibernate.Dialect.Function;

namespace Aqua.Data.Dialects;

/// <summary>
/// Extends MsSql2012Dialect with aqua-specific SQL functions ported from the legacy
/// aquaBackEnd MsSql2012ExtendedDialect. DevExpress-dependent members (full-text search
/// filter builder, health-checker, lock-query) have been removed — they belong to the
/// legacy app layer, not the foundation NuGet.
/// </summary>
public class MsSql2012ExtendedDialect : MsSql2012Dialect, INaturalStringSortingDialect
{
    public MsSql2012ExtendedDialect()
        : base()
    {
        // We use our custom function here which (depending on underlying version of sqlserver) either
        // uses isowk date part (>=2008), or calculates manually (<=2005).
        // see also http://blogs.lessthandot.com/index.php/DataMgmt/DataDesign/iso-week-in-sql-server
        RegisterFunction("week", new SQLFunctionTemplate(NHibernateUtil.Int32, "dbo.ISOWeek(?1)"));
        RegisterFunction("dateonly", new SQLFunctionTemplate(NHibernateUtil.DateTime, "CONVERT(date, ?1)"));
        RegisterFunction("monthonly", new SQLFunctionTemplate(NHibernateUtil.DateTime, "DATEADD(m, DATEDIFF(m, 0, ?1), 0)"));
        RegisterFunction("datalength", new SQLFunctionTemplate(NHibernateUtil.Int32, "datalength(?1)"));

        // 'isodayofweek' is intended to return a week day number (1-7). Monday=1,...,Sunday=7
        RegisterFunction("isodayofweek", new SQLFunctionTemplate(NHibernateUtil.DateTime, "(((DATEPART( WEEKDAY, ?1) + @@DATEFIRST - 1 - 1) % 7)+1)"));
        RegisterFunction("dayofmonth", new SQLFunctionTemplate(NHibernateUtil.DateTime, "DATEPART(day, ?1)"));

        // workaround for https://nhibernate.jira.com/projects/NH/issues/NH-3893
        RegisterFunction("lleft", new SQLFunctionTemplate(NHibernateUtil.String, "left(?1, ?2)"));
        RegisterFunction("rright", new SQLFunctionTemplate(NHibernateUtil.String, "right(?1, ?2)"));
    }

    /// <summary>
    /// Returns an ORDER BY expression that applies natural string sorting for the given column.
    /// Delegates to the <c>dbo.natural_string_padding</c> database function (ported from legacy).
    /// </summary>
    public string GetNaturalStringSortExpression(string columnExpression)
        => $"dbo.natural_string_padding({columnExpression})";
}
