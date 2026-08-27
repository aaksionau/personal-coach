using Coach.Domain.Entities;

namespace Coach.Application.Models;

/// <summary>
/// A read-only view of one other coach's current state, assembled so the coach being called can
/// account for what's happening in the user's other domains (e.g. Career seeing a Health goal that's
/// eating into work time). Built fresh per turn from live data -- never a cached snapshot -- and
/// carries no ids, since the called coach can't act on another coach's goals or reflections.
/// </summary>
public sealed record CrossCoachSnapshot(
    string CoachName,
    IReadOnlyList<GoalWithActionItems> Goals,
    IReadOnlyList<Reflection> Reflections);
