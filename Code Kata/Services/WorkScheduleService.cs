using Code_Kata.Entities.Engineers;
using Code_Kata.Entities.Incidents;
using Code_Kata.Entities.WorkSchedules;

namespace Code_Kata.Services;

public class WorkScheduleService
{
    public WorkScheduleEntry? TryCreateScheduleEntry(
        Engineer engineer,
        Incident incident,
        TimeOnly nextAvailableAt,
        int remainingMinutes)
    {
        if (remainingMinutes < incident.EstimatedMinutes)
        {
            return null;
        }

        var startAt = GetStartTime(nextAvailableAt, incident, engineer);
        var endAt = GetEndTime(startAt, incident);

        if (endAt > engineer.AvailableUntil)
        {
            return null;
        }

        return new WorkScheduleEntry
        {
            EngineerId = engineer.Id,
            IncidentId = incident.Id,
            StartAt = startAt,
            EndAt = endAt
        };
    }


    private TimeOnly GetStartTime(TimeOnly nextAvailableAt, Incident incident, Engineer engineer)
    {
        var startAt = nextAvailableAt > engineer.AvailableFrom ? nextAvailableAt : engineer.AvailableFrom;
        return incident.ReportedAt > startAt ? incident.ReportedAt : startAt;
    }
    
    private TimeOnly GetEndTime(TimeOnly startAt, Incident incident)
    {
        return startAt.AddMinutes(incident.EstimatedMinutes);
    }
}