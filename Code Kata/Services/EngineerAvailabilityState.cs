using Code_Kata.Entities.Engineers;
using Code_Kata.Entities.WorkSchedules;

namespace Code_Kata.Services;

internal readonly record struct EngineerAvailabilityState(TimeOnly NextAvailableAt, int RemainingMinutes)
{
    public static IReadOnlyList<EngineerAvailabilityState> CreateInitialStates(
        IReadOnlyList<Engineer> engineers,
        IReadOnlyList<WorkScheduleEntry> existingEntries)
    {
        return engineers
            .Select(engineer => CreateInitialState(engineer, existingEntries))
            .ToArray();
    }

    private static EngineerAvailabilityState CreateInitialState(
        Engineer engineer,
        IReadOnlyList<WorkScheduleEntry> existingEntries)
    {
        var engineerEntries = existingEntries
            .Where(entry => entry.EngineerId == engineer.Id)
            .OrderBy(entry => entry.StartAt)
            .ToList();

        var nextAvailableAt = engineerEntries.LastOrDefault()?.EndAt ?? engineer.AvailableFrom;
        var remainingMinutes = engineer.MaxWorkMinutes - engineerEntries.Sum(entry => entry.Minutes);

        return new EngineerAvailabilityState(
            nextAvailableAt > engineer.AvailableFrom ? nextAvailableAt : engineer.AvailableFrom,
            Math.Max(0, remainingMinutes));
    }
}

