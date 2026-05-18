namespace Aqua.Data.Dialects;

/// <summary>
/// Implemented by every aqua dialect so callers can ask for a natural-sort
/// SQL function (sorts "Item-2" before "Item-10" rather than after).
/// </summary>
public interface INaturalStringSortingDialect
{
    string GetNaturalStringSortExpression(string columnExpression);
}
