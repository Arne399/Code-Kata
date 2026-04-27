namespace Code_Kata.Entities.WorkSchedules;

public record SchedulingPlan(
    IReadOnlyList<WorkScheduleEntry> Entries,
    IReadOnlyList<string> UnassignedIncidentIds)
{
    public static SchedulingPlan Empty { get; } = new([], []);
}

