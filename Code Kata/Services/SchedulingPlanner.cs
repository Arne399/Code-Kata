using Code_Kata.Entities.Engineers;
using Code_Kata.Entities.Incidents;
using Code_Kata.Entities.WorkSchedules;
using System.Text;

namespace Code_Kata.Services;

internal class SchedulingPlanner
{
    private readonly EngineerService _engineerService;
    private readonly WorkScheduleService _workScheduleService;
    private readonly ScoringService _scoringService;
    private readonly int _beamWidth;

    public SchedulingPlanner()
        : this(new EngineerService(), new WorkScheduleService(), new ScoringService())
    {
    }

    public SchedulingPlanner(
        EngineerService engineerService,
        WorkScheduleService workScheduleService,
        ScoringService scoringService,
        int beamWidth = 256)
    {
        _engineerService = engineerService;
        _workScheduleService = workScheduleService;
        _scoringService = scoringService;
        _beamWidth = beamWidth;
    }

    internal SchedulingPlan CreatePlan(
        IReadOnlyList<Incident> incidents,
        IReadOnlyList<Engineer> engineers,
        IReadOnlyList<WorkScheduleEntry>? existingEntries = null)
    {
        if (incidents.Count == 0)
        {
            return SchedulingPlan.Empty;
        }

        var orderedEngineers = engineers
            .OrderBy(engineer => engineer.Id)
            .ToList();

        var initialStates = BuildInitialStates(orderedEngineers, existingEntries ?? []);
        var engineerIndexById = orderedEngineers
            .Select((engineer, index) => new { engineer.Id, Index = index })
            .ToDictionary(item => item.Id, item => item.Index, StringComparer.Ordinal);

        var candidateEngineerIndexes = incidents
            .Select(incident => _engineerService.GetQualifiedEngineers(orderedEngineers, incident.Type)
                .Select(engineer => engineerIndexById[engineer.Id])
                .ToArray())
            .ToArray();

        CandidateState? bestState = null;
        foreach (var ordering in BuildIncidentOrderings(incidents))
        {
            var candidate = BuildScheduleForOrdering(ordering, incidents, orderedEngineers, candidateEngineerIndexes, initialStates);
            bestState = ChooseBetter(bestState, candidate);
        }

        return ToPlan(bestState ?? CandidateState.Empty(initialStates));
    }

    private CandidateState BuildScheduleForOrdering(
        IReadOnlyList<int> incidentOrder,
        IReadOnlyList<Incident> incidents,
        IReadOnlyList<Engineer> engineers,
        IReadOnlyList<int[]> candidateEngineerIndexes,
        IReadOnlyList<EngineerAvailabilityState> initialStates)
    {
        var beam = new List<CandidateState>
        {
            CandidateState.Empty(initialStates)
        };

        foreach (var incidentIndex in incidentOrder)
        {
            var incident = incidents[incidentIndex];
            var nextBySignature = new Dictionary<string, CandidateState>(StringComparer.Ordinal);

            foreach (var state in beam)
            {
                AddCandidate(nextBySignature, state.WithUnassigned(incident, _scoringService));

                foreach (var engineerIndex in candidateEngineerIndexes[incidentIndex])
                {
                    var engineer = engineers[engineerIndex];
                    var engineerState = state.EngineerStates[engineerIndex];
                    var scheduleEntry = _workScheduleService.TryCreateScheduleEntry(
                        engineer,
                        incident,
                        engineerState.NextAvailableAt,
                        engineerState.RemainingMinutes);

                    if (scheduleEntry is null)
                    {
                        continue;
                    }

                    AddCandidate(nextBySignature, state.WithAssignment(engineerIndex, scheduleEntry, incident, _scoringService));
                }
            }

            beam = nextBySignature.Values
                .OrderBy(candidate => candidate.TotalPenalty)
                .ThenBy(candidate => candidate.UnresolvedPenalty)
                .ThenBy(candidate => candidate.LatePenalty)
                .ThenByDescending(candidate => candidate.Entries.Count)
                .ThenBy(candidate => candidate.PlanKey)
                .Take(_beamWidth)
                .ToList();
        }

        return beam
            .OrderBy(candidate => candidate.TotalPenalty)
            .ThenBy(candidate => candidate.UnresolvedPenalty)
            .ThenBy(candidate => candidate.LatePenalty)
            .ThenByDescending(candidate => candidate.Entries.Count)
            .ThenBy(candidate => candidate.PlanKey)
            .First();
    }

    private IEnumerable<IReadOnlyList<int>> BuildIncidentOrderings(IReadOnlyList<Incident> incidents)
    {
        var indexedIncidents = incidents
            .Select((incident, index) => new IndexedIncident(index, incident))
            .ToList();

        return
        [
            indexedIncidents
                .OrderByDescending(item => _scoringService.GetUnresolvedPenalty(item.Incident))
                .ThenBy(item => item.Incident.Deadline)
                .ThenBy(item => item.Incident.ReportedAt)
                .Select(item => item.Index)
                .ToArray(),

            indexedIncidents
                .OrderBy(item => item.Incident.Deadline)
                .ThenByDescending(item => _scoringService.GetUnresolvedPenalty(item.Incident))
                .ThenBy(item => item.Incident.ReportedAt)
                .Select(item => item.Index)
                .ToArray(),

            indexedIncidents
                .OrderByDescending(item => _scoringService.GetWeightedImpact(item.Incident))
                .ThenBy(item => item.Incident.Deadline)
                .ThenByDescending(item => item.Incident.EstimatedMinutes)
                .Select(item => item.Index)
                .ToArray(),

            indexedIncidents
                .OrderByDescending(item => (double)_scoringService.GetUnresolvedPenalty(item.Incident) / Math.Max(1, item.Incident.EstimatedMinutes))
                .ThenBy(item => item.Incident.Deadline)
                .ThenBy(item => item.Incident.ReportedAt)
                .Select(item => item.Index)
                .ToArray()
        ];
    }

    private static IReadOnlyList<EngineerAvailabilityState> BuildInitialStates(
        IReadOnlyList<Engineer> engineers,
        IReadOnlyList<WorkScheduleEntry> existingEntries)
    {
        return engineers
            .Select(engineer =>
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
            })
            .ToArray();
    }

    private static void AddCandidate(IDictionary<string, CandidateState> candidatesBySignature, CandidateState candidate)
    {
        if (candidatesBySignature.TryGetValue(candidate.Signature, out var existing))
        {
            candidatesBySignature[candidate.Signature] = ChooseBetter(existing, candidate)!;
            return;
        }

        candidatesBySignature[candidate.Signature] = candidate;
    }

    private static CandidateState? ChooseBetter(CandidateState? currentBest, CandidateState? candidate)
    {
        if (candidate is null)
        {
            return currentBest;
        }

        if (currentBest is null)
        {
            return candidate;
        }

        if (candidate.TotalPenalty != currentBest.TotalPenalty)
        {
            return candidate.TotalPenalty < currentBest.TotalPenalty ? candidate : currentBest;
        }

        if (candidate.UnresolvedPenalty != currentBest.UnresolvedPenalty)
        {
            return candidate.UnresolvedPenalty < currentBest.UnresolvedPenalty ? candidate : currentBest;
        }

        if (candidate.LatePenalty != currentBest.LatePenalty)
        {
            return candidate.LatePenalty < currentBest.LatePenalty ? candidate : currentBest;
        }

        if (candidate.Entries.Count != currentBest.Entries.Count)
        {
            return candidate.Entries.Count > currentBest.Entries.Count ? candidate : currentBest;
        }

        return string.CompareOrdinal(candidate.PlanKey, currentBest.PlanKey) < 0 ? candidate : currentBest;
    }

    private SchedulingPlan ToPlan(CandidateState state)
    {
        var orderedEntries = state.Entries
            .OrderBy(entry => entry.EngineerId)
            .ThenBy(entry => entry.StartAt)
            .ThenBy(entry => entry.EndAt)
            .ThenBy(entry => entry.IncidentId)
            .ToList();

        var unassignedIncidentIds = state.UnassignedIncidentIds
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        return new SchedulingPlan(orderedEntries, unassignedIncidentIds);
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

    private readonly record struct IndexedIncident(int Index, Incident Incident);

    private readonly record struct EngineerAvailabilityState(TimeOnly NextAvailableAt, int RemainingMinutes);

    private sealed record CandidateState(
        IReadOnlyList<EngineerAvailabilityState> EngineerStates,
        IReadOnlyList<WorkScheduleEntry> Entries,
        IReadOnlyList<string> UnassignedIncidentIds,
        long LatePenalty,
        long UnresolvedPenalty,
        string Signature,
        string PlanKey)
    {
        public long TotalPenalty => LatePenalty + UnresolvedPenalty;

        public static CandidateState Empty(IReadOnlyList<EngineerAvailabilityState> initialStates)
        {
            var clonedStates = initialStates.ToArray();
            return new CandidateState(clonedStates, Array.Empty<WorkScheduleEntry>(), Array.Empty<string>(), 0, 0, CreateStateSignature(clonedStates), string.Empty);
        }

        public CandidateState WithUnassigned(Incident incident, ScoringService scoringService)
        {
            var updatedUnassignedIncidentIds = UnassignedIncidentIds
                .Append(incident.Id)
                .ToArray();

            return new CandidateState(
                EngineerStates.ToArray(),
                Entries.ToArray(),
                updatedUnassignedIncidentIds,
                LatePenalty,
                UnresolvedPenalty + scoringService.GetUnresolvedPenalty(incident),
                Signature,
                CreatePlanKey(Entries, updatedUnassignedIncidentIds));
        }

        public CandidateState WithAssignment(
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

            return new CandidateState(
                updatedEngineerStates,
                updatedEntries,
                UnassignedIncidentIds.ToArray(),
                LatePenalty + scoringService.GetLatePenalty(incident, scheduleEntry.EndAt),
                UnresolvedPenalty,
                CreateStateSignature(updatedEngineerStates),
                CreatePlanKey(updatedEntries, UnassignedIncidentIds));
        }
    }
}

