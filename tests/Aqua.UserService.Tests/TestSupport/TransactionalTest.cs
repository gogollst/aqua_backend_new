using NHibernate;

namespace Aqua.UserService.Tests.TestSupport;

public abstract class TransactionalTest : IDisposable
{
    protected ISession Session { get; }
    private readonly ITransaction _tx;

    protected TransactionalTest(PostgresFixture fx)
    {
        Session = fx.SessionFactory.OpenSession();
        _tx = Session.BeginTransaction();
    }

    public void Dispose()
    {
        if (_tx.IsActive) _tx.Rollback();
        _tx.Dispose();
        Session.Dispose();
    }
}
