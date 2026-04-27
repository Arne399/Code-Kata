using Code_Kata.Entities.Incidents;
using Code_Kata.Entities.WorkSchedules;

namespace Code_Kata.Services;

public class AssignmentService
{
    private readonly IncidentService _incidentService;
    private readonly SchedulingPlanner _planner;
    private readonly SchedulingStateApplier _stateApplier;

    public AssignmentService()
        : this(new IncidentService(), new SchedulingPlanner(), new SchedulingStateApplier())
    {
    }

    internal AssignmentService(
        IncidentService incidentService,
        SchedulingPlanner planner,
        SchedulingStateApplier stateApplier)
    {
        _incidentService = incidentService;
        _planner = planner;
        _stateApplier = stateApplier;
    }

    public List<WorkScheduleEntry> AssignPendingIncidents()
    {
        var pendingIncidents = _incidentService.GetPendingIncidents().ToList();
        if (pendingIncidents.Count == 0)
        {
            State.WorkScheduleEntries = SchedulingStateApplier.SortEntries(State.WorkScheduleEntries);
            return [];
        }

        var plan = _planner.CreatePlan(pendingIncidents, State.Engineers, State.WorkScheduleEntries);
        _stateApplier.Apply(pendingIncidents, plan);
        return plan.Entries.ToList();
    }

    public WorkScheduleEntry? AssignIncident(Incident incident)
    {
        if (incident.Status != IncidentStatus.New)
        {
            return null;
        }

        var plan = _planner.CreatePlan([incident], State.Engineers, State.WorkScheduleEntries);
        _stateApplier.Apply([incident], plan);

        var scheduleEntry = plan.Entries.SingleOrDefault();
        if (scheduleEntry is null)
        {
            return null;
        }

        return scheduleEntry;
    }
}