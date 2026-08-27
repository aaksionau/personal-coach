using Coach.Domain.Entities;

namespace Coach.Application.Models;

/// <summary>
/// A coach's current goal/reflection state, named. Assembled fresh per turn from live data -- never
/// a cached snapshot. <see cref="CoachContext"/> holds one for the coach being called and a list of
/// them for the other coaches, so advice from any one coach can account for what's happening in the
/// other domains (e.g. Career seeing a Health goal that's eating into work time). Carries no ids for
/// the other coaches' state, since the called coach can't act on it.
/// </summary>
public sealed record CoachTrackedState(
    string CoachName,
    IReadOnlyList<GoalWithActionItems> Goals,
    IReadOnlyList<Reflection> Reflections);
