using Code_Kata.Entities.Output;

namespace Code_Kata.Services;

public class ScheduleService
{
    private readonly IncidentService _incidentService;
    private readonly SchedulingPlanner _planner;
    private readonly SchedulingStateApplier _stateApplier;

    public ScheduleService()
        : this(new IncidentService(), new SchedulingPlanner(), new SchedulingStateApplier())
    {
    }

    public ScheduleService(
        IncidentService incidentService,
        SchedulingPlanner planner,
        SchedulingStateApplier stateApplier)
    {
        _incidentService = incidentService;
        _planner = planner;
        _stateApplier = stateApplier;
    }

    public ScheduleResult CreateSchedule()
    {
        var incidents = _incidentService.GetPendingIncidents().ToList();
        if (incidents.Count == 0)
        {
            return new ScheduleResult();
        }

        var plan = _planner.CreatePlan(incidents, State.Engineers, State.WorkScheduleEntries);
        _stateApplier.Apply(incidents, plan);

        return new ScheduleResult
        {
            Assignments = plan.Entries
                .Select(entry => new Assignment
                {
                    IncidentId = entry.IncidentId,
                    EngineerId = entry.EngineerId,
                    StartTime = entry.StartAt,
                    EndTime = entry.EndAt
                })
                .ToList(),
            UnAssignedIncidents = plan.UnassignedIncidentIds.ToList()
        };
    }
}