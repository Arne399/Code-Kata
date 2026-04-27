using Code_Kata.Entities.Incidents;
using Code_Kata.Entities.WorkSchedules;
using System.Text;

namespace Code_Kata.Services;

internal sealed record SchedulingCandidateState(
    IReadOnlyList<EngineerAvailabilityState> EngineerStates,
    IReadOnlyList<WorkScheduleEntry> Entries,
    IReadOnlyList<string> UnassignedIncidentIds,
    long LatePenalty,
    long UnresolvedPenalty,
    string Signature,
    string PlanKey)
{
    public long TotalPenalty => LatePenalty + UnresolvedPenalty;

    public static SchedulingCandidateState Empty(IReadOnlyList<EngineerAvailabilityState> initialStates)
    {
        var clonedStates = initialStates.ToArray();
        return new SchedulingCandidateState(
            clonedStates,
            Array.Empty<WorkScheduleEntry>(),
            Array.Empty<string>(),
            0,
            0,
            CreateStateSignature(clonedStates),
            string.Empty);
    }

    public SchedulingCandidateState WithUnassigned(Incident incident, ScoringService scoringService)
    {
        var updatedUnassignedIncidentIds = UnassignedIncidentIds
            .Append(incident.Id)
            .ToArray();

        return new SchedulingCandidateState(
            EngineerStates.ToArray(),
            Entries.ToArray(),
            updatedUnassignedIncidentIds,
            LatePenalty,
            UnresolvedPenalty + scoringService.GetUnresolvedPenalty(incident),
            Signature,
            CreatePlanKey(Entries, updatedUnassignedIncidentIds));
    }

    public SchedulingCandidateState WithAssignment(
        int engineerIndex,
        WorkScheduleEntry scheduleEntry,
        Incident incident,
        ScoringService scoringService)
    {
        var updatedEngineerStates = EngineerStates.ToArray();
        var engineerState = updatedEngineerStates[engineerIndex];
        updatedEngineerStates[engineerIndex] = new EngineerAvailabilityState(
            scheduleEntry.EndAt,
            engineerState.RemainingMinutes - incident.EstimatedMinutes);

        var updatedEntries = Entries
            .Append(scheduleEntry)
            .ToArray();

        return new SchedulingCandidateState(
            updatedEngineerStates,
            updatedEntries,
            UnassignedIncidentIds.ToArray(),
            LatePenalty + scoringService.GetLatePenalty(incident, scheduleEntry.EndAt),
            UnresolvedPenalty,
            CreateStateSignature(updatedEngineerStates),
            CreatePlanKey(updatedEntries, UnassignedIncidentIds));
    }

    private static string CreateStateSignature(IReadOnlyList<EngineerAvailabilityState> engineerStates)
    {
        var builder = new StringBuilder();

        for (var index = 0; index < engineerStates.Count; index++)
        {
            if (index > 0)
            {
                builder.Append('|');
            }

            var engineerState = engineerStates[index];
            builder.Append((int)engineerState.NextAvailableAt.ToTimeSpan().TotalMinutes)
                .Append(':')
                .Append(engineerState.RemainingMinutes);
        }

        return builder.ToString();
    }

    private static string CreatePlanKey(
        IReadOnlyList<WorkScheduleEntry> entries,
        IReadOnlyList<string> unassignedIncidentIds)
    {
        var entriesKey = string.Join(
            '|',
            entries
                .OrderBy(entry => entry.EngineerId)
                .ThenBy(entry => entry.StartAt)
                .ThenBy(entry => entry.EndAt)
                .ThenBy(entry => entry.IncidentId)
                .Select(entry => $"{entry.EngineerId}:{entry.IncidentId}:{entry.StartAt:HH\\:mm}:{entry.EndAt:HH\\:mm}"));

        var unassignedKey = string.Join(',', unassignedIncidentIds.OrderBy(id => id, StringComparer.Ordinal));
        return $"{entriesKey}#{unassignedKey}";
    }
}

