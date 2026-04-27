using Code_Kata.Entities.Engineers;
using Code_Kata.Entities.Incidents;
using Code_Kata.Entities.WorkSchedules;

namespace Code_Kata.Services;

public class AssignmentService
{
    private readonly EngineerService _engineerService;
    private readonly IncidentService _incidentService;
    private readonly WorkScheduleService _workScheduleService;

    public AssignmentService()
        : this(new EngineerService(), new IncidentService(), new WorkScheduleService())
    {
    }

    public AssignmentService(
        EngineerService engineerService,
        IncidentService incidentService,
        WorkScheduleService workScheduleService)
    {
        _engineerService = engineerService;
        _incidentService = incidentService;
        _workScheduleService = workScheduleService;
    }

    public List<WorkScheduleEntry> AssignPendingIncidents()
    {
        var createdEntries = new List<WorkScheduleEntry>();

        foreach (var incident in _incidentService.GetPendingIncidents())
        {
            var scheduleEntry = TryScheduleIncident(incident);
            if (scheduleEntry is null)
            {
                _incidentService.MarkAtRisk(incident);
                continue;
            }

            _incidentService.MarkScheduled(incident, scheduleEntry.EngineerId);
            createdEntries.Add(scheduleEntry);
        }

        return createdEntries;
    }

    public WorkScheduleEntry? AssignIncident(Incident incident)
    {
        if (incident.Status != IncidentStatus.New)
        {
            return null;
        }

        var scheduleEntry = TryScheduleIncident(incident);
        if (scheduleEntry is null)
        {
            _incidentService.MarkAtRisk(incident);
            return null;
        }

        _incidentService.MarkScheduled(incident, scheduleEntry.EngineerId);
        return scheduleEntry;
    }

    private WorkScheduleEntry? TryScheduleIncident(Incident incident)
    {
        var request = new EngineerRequest(
            incident.Deadline,
            incident.ReportedAt,
            (int)incident.Severity * incident.Impact,
            incident.Type,
            incident.EstimatedMinutes);

        var eligibleEngineers = _engineerService.GetAvailableEngineers(request);
        if (eligibleEngineers.Count == 0)
        {
            return null;
        }

        return _workScheduleService.TrySchedule(eligibleEngineers, incident);
    }
}