namespace RepairDesk.Api.Models;

/// <summary>
/// Marks entities with a manually-rotated concurrency token so optimistic concurrency
/// behaves the same way on SQL Server and on SQLite (which has no native rowversion type).
/// </summary>
public interface IHasRowVersion
{
    byte[] RowVersion { get; set; }
}
