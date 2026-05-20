using NHibernate;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Driver;
using Environment = NHibernate.Cfg.Environment;

namespace Aqua.UserService.Persistence;

public sealed class UserServiceSessionFactoryBuilder
{
    private readonly string _connectionString;

    public UserServiceSessionFactoryBuilder(string connectionString)
    {
        _connectionString = connectionString;
    }

    public ISessionFactory Build()
    {
        var cfg = new Configuration();
        cfg.SetProperty(Environment.ConnectionDriver, typeof(NpgsqlDriver).AssemblyQualifiedName);
        cfg.SetProperty(Environment.Dialect, typeof(PostgreSQL83Dialect).AssemblyQualifiedName);
        cfg.SetProperty(Environment.ConnectionString, _connectionString);
        cfg.SetProperty(Environment.Hbm2ddlKeyWords, "auto-quote");
        cfg.SetProperty(Environment.ShowSql, "false");
        cfg.SetProperty(Environment.FormatSql, "true");
        cfg.AddAssembly(typeof(UserServiceSessionFactoryBuilder).Assembly);
        return cfg.BuildSessionFactory();
    }
}
