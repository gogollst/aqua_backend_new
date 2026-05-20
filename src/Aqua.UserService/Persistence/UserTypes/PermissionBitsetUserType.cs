using System.Data;
using System.Data.Common;
using Aqua.UserService.Roles;
using NHibernate;
using NHibernate.Engine;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;

namespace Aqua.UserService.Persistence.UserTypes;

public sealed class PermissionBitsetUserType : IUserType
{
    public SqlType[] SqlTypes => new[] { new SqlType(DbType.String) };
    public Type ReturnedType  => typeof(PermissionBitset);
    public bool IsMutable     => false;

    public new bool Equals(object? x, object? y) =>
        (x as PermissionBitset)?.Equals(y as PermissionBitset) ?? y is null;

    public int GetHashCode(object x) => x.GetHashCode();

    public object? NullSafeGet(DbDataReader rs, string[] names, ISessionImplementor session, object owner)
    {
        var idx = rs.GetOrdinal(names[0]);
        if (rs.IsDBNull(idx)) return PermissionBitset.None;
        var blob = rs.GetString(idx);
        return PermissionBitset.FromLegacyBlob(blob);
    }

    public void NullSafeSet(DbCommand cmd, object? value, int index, ISessionImplementor session)
    {
        var pb = value as PermissionBitset ?? PermissionBitset.None;
        ((DbParameter)cmd.Parameters[index]).Value = pb.ToLegacyBlob();
    }

    public object DeepCopy(object value) => value;
    public object Replace(object original, object target, object owner) => original;
    public object Assemble(object cached, object owner) => cached;
    public object Disassemble(object value) => value;
}
