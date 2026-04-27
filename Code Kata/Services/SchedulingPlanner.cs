using Code_Kata.Entities.Engineers;
using Code_Kata.Entities.Incidents;
using Code_Kata.Entities.WorkSchedules;

namespace Code_Kata.Services;

internal sealed class SchedulingPlanner
{
    private readonly EngineerService _engineerService;
    private readonly WorkScheduleService _workScheduleService;
    private readonly ScoringService _scoringService;
    private readonly IncidentOrderingFactory _incidentOrderingFactory;
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
        _incidentOrderingFactory = new IncidentOrderingFactory(scoringService);
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

        var planningContext = CreatePlanningContext(incidents, engineers, existingEntries ?? []);
        var bestCandidate = EvaluateIncidentOrderings(planningContext);
        return ToPlan(bestCandidate);
    }

    private PlanningContext CreatePlanningContext(
        IReadOnlyList<Incident> incidents,
        IReadOnlyList<Engineer> engineers,
        IReadOnlyList<WorkScheduleEntry> existingEntries)
    {
        var orderedEngineers = OrderEngineers(engineers);
        var initialEngineerStates = EngineerAvailabilityState.CreateInitialStates(orderedEngineers, existingEntries);
        var candidateEngineerIndexes = BuildCandidateEngineerIndexes(incidents, orderedEngineers);

        return new PlanningContext(incidents, orderedEngineers, candidateEngineerIndexes, initialEngineerStates);
    }

    private static IReadOnlyList<Engineer> OrderEngineers(IEnumerable<Engineer> engineers)
    {
        return engineers
            .OrderBy(engineer => engineer.Id)
            .ToList();
    }

    private IReadOnlyList<int[]> BuildCandidateEngineerIndexes(
        IReadOnlyList<Incident> incidents,
        IReadOnlyList<Engineer> orderedEngineers)
    {
        var engineerIndexById = orderedEngineers
            .Select((engineer, index) => new { engineer.Id, Index = index })
            .ToDictionary(item => item.Id, item => item.Index, StringComparer.Ordinal);

        return incidents
            .Select(incident => _engineerService.GetQualifiedEngineers(orderedEngineers, incident.Type)
                .Select(engineer => engineerIndexById[engineer.Id])
                .ToArray())
            .ToArray();
    }

    private SchedulingCandidateState EvaluateIncidentOrderings(PlanningContext planningContext)
    {
        SchedulingCandidateState? bestCandidate = null;

        foreach (var incidentOrder in _incidentOrderingFactory.Create(planningContext.Incidents))
        {
            var candidate = EvaluateIncidentOrdering(planningContext, incidentOrder);
            bestCandidate = ChooseBetter(bestCandidate, candidate);
        }

        return bestCandidate ?? SchedulingCandidateState.Empty(planningContext.InitialEngineerStates);
    }

    private SchedulingCandidateState EvaluateIncidentOrdering(
        PlanningContext planningContext,
        IReadOnlyList<int> incidentOrder)
    {
        var currentBeam = new List<SchedulingCandidateState>
        {
            SchedulingCandidateState.Empty(planningContext.InitialEngineerStates)
        };

        foreach (var incidentIndex in incidentOrder)
        {
            currentBeam = ExpandBeam(planningContext, currentBeam, incidentIndex);
        }

        return RankCandidates(currentBeam)
            .First();
    }

    private List<SchedulingCandidateState> ExpandBeam(
        PlanningContext planningContext,
        IReadOnlyList<SchedulingCandidateState> currentBeam,
        int incidentIndex)
    {
        var incident = planningContext.Incidents[incidentIndex];
        var candidatesBySignature = new Dictionary<string, SchedulingCandidateState>(StringComparer.Ordinal);

        foreach (var candidate in currentBeam)
        {
            AddUnassignedCandidate(candidatesBySignature, candidate, incident);
            AddAssignedCandidates(candidatesBySignature, planningContext, candidate, incidentIndex, incident);
        }

        return RankCandidates(candidatesBySignature.Values)
            .Take(_beamWidth)
            .ToList();
    }

    private void AddUnassignedCandidate(
        IDictionary<string, SchedulingCandidateState> candidatesBySignature,
        SchedulingCandidateState candidate,
        Incident incident)
    {
        AddCandidate(candidatesBySignature, candidate.WithUnassigned(incident, _scoringService));
    }

    private void AddAssignedCandidates(
        IDictionary<string, SchedulingCandidateState> candidatesBySignature,
        PlanningContext planningContext,
        SchedulingCandidateState candidate,
        int incidentIndex,
        Incident incident)
    {
        foreach (var engineerIndex in planningContext.CandidateEngineerIndexes[incidentIndex])
        {
            TryAddAssignedCandidate(candidatesBySignature, planningContext.Engineers, candidate, engineerIndex, incident);
        }
    }

    private void TryAddAssignedCandidate(
        IDictionary<string, SchedulingCandidateState> candidatesBySignature,
        IReadOnlyList<Engineer> engineers,
        SchedulingCandidateState candidate,
        int engineerIndex,
        Incident incident)
    {
        var engineer = engineers[engineerIndex];
        var engineerState = candidate.EngineerStates[engineerIndex];
        var scheduleEntry = _workScheduleService.TryCreateScheduleEntry(
            engineer,
            incident,
            engineerState.NextAvailableAt,
            engineerState.RemainingMinutes);

        if (scheduleEntry is null)
        {
            return;
        }

        AddCandidate(candidatesBySignature, candidate.WithAssignment(engineerIndex, scheduleEntry, incident, _scoringService));
    }

    private IEnumerable<SchedulingCandidateState> RankCandidates(IEnumerable<SchedulingCandidateState> candidates)
    {
        return candidates
            .OrderBy(candidate => candidate.TotalPenalty)
            .ThenBy(candidate => candidate.UnresolvedPenalty)
            .ThenBy(candidate => candidate.LatePenalty)
            .ThenByDescending(candidate => candidate.Entries.Count)
            .ThenBy(candidate => candidate.PlanKey);
    }

    private static void AddCandidate(IDictionary<string, SchedulingCandidateState> candidatesBySignature, SchedulingCandidateState candidate)
    {
        if (candidatesBySignature.TryGetValue(candidate.Signature, out var existing))
        {
            candidatesBySignature[candidate.Signature] = ChooseBetter(existing, candidate)!;
            return;
        }

        candidatesBySignature[candidate.Signature] = candidate;
    }

    private static SchedulingCandidateState? ChooseBetter(SchedulingCandidateState? currentBest, SchedulingCandidateState? candidate)
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

    private SchedulingPlan ToPlan(SchedulingCandidateState state)
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

    private sealed record PlanningContext(
        IReadOnlyList<Incident> Incidents,
        IReadOnlyList<Engineer> Engineers,
        IReadOnlyList<int[]> CandidateEngineerIndexes,
        IReadOnlyList<EngineerAvailabilityState> InitialEngineerStates);
}

