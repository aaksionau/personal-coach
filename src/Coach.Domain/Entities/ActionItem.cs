using Coach.Domain.Enums;

namespace Coach.Domain.Entities;

public sealed class ActionItem
{
    public required Guid Id { get; init; }

    public required Guid GoalId { get; init; }

    public required string Description { get; init; }

    /// <summary>Mutable: the only field of a goal/action item that changes after creation.</summary>
    public required ActionItemStatus Status { get; set; }

    public DateOnly? DueDate { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public static ActionItem Create(Guid goalId, string description, DateOnly? dueDate) => new()
    {
        Id = Guid.NewGuid(),
        GoalId = goalId,
        Description = description,
        Status = ActionItemStatus.Open,
        DueDate = dueDate,
        CreatedAtUtc = DateTimeOffset.UtcNow,
    };
}
