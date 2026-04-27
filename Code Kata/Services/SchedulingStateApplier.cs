using Code_Kata.Entities.Incidents;
using Code_Kata.Entities.WorkSchedules;

namespace Code_Kata.Services;

internal class SchedulingStateApplier
{
    private readonly IncidentService _incidentService;

    public SchedulingStateApplier()
        : this(new IncidentService())
    {
    }

    public SchedulingStateApplier(IncidentService incidentService)
    {
        _incidentService = incidentService;
    }

    public void Apply(IReadOnlyList<Incident> incidents, SchedulingPlan plan)
    {
        var assignmentsByIncidentId = plan.Entries
            .ToDictionary(entry => entry.IncidentId, StringComparer.Ordinal);

        foreach (var incident in incidents)
        {
            if (assignmentsByIncidentId.TryGetValue(incident.Id, out var entry))
            {
                _incidentService.MarkScheduled(incident, entry.EngineerId);
                continue;
            }

            _incidentService.MarkAtRisk(incident);
        }

        State.WorkScheduleEntries = SortEntries(State.WorkScheduleEntries.Concat(plan.Entries));
    }

    public static List<WorkScheduleEntry> SortEntries(IEnumerable<WorkScheduleEntry> entries)
    {
        return entries
            .OrderBy(entry => entry.EngineerId)
            .ThenBy(entry => entry.StartAt)
            .ThenBy(entry => entry.EndAt)
            .ThenBy(entry => entry.IncidentId)
            .ToList();
    }
}

