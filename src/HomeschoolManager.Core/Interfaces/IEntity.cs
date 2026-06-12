namespace HomeschoolManager.Core.Interfaces;

/// <summary>
/// Common contract for persisted entities so repositories can share
/// id assignment, creation stamping, and lookup logic.
/// </summary>
public interface IEntity
{
    int Id { get; set; }
    DateTime CreatedAt { get; set; }
}
