namespace RepairDesk.Api.Exceptions;

/// <summary>
/// Thrown when a caller's If-Match RowVersion no longer matches the stored one,
/// meaning another request modified the same record first.
/// </summary>
public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message) : base(message)
    {
    }
}
