using Code_Kata.Entities.Engineers;
using Code_Kata.Entities.Incidents;
using Code_Kata.Entities.WorkSchedules;

namespace Code_Kata.Services;

public class WorkScheduleService
{
    public WorkScheduleEntry TrySchedule(List<Engineer> engineers, Incident incident)
    {
        var engineer = engineers.FirstOrDefault();

        var lastWorkSchedule = State.WorkScheduleEntries.Where(ws => ws.EngineerId == engineer.Id)
            .OrderBy(ws => ws.StartAt).LastOrDefault();
        
        var startAt = GetStartTime(lastWorkSchedule, incident, engineer);
        var endAt = GetEndTime(startAt, incident);

        var newEntry = new WorkScheduleEntry
        {
            EngineerId = engineer.Id,
            IncidentId = incident.Id,
            StartAt = startAt,
            EndAt = endAt
        };

        State.WorkScheduleEntries.Add(newEntry);
        return newEntry;
    }

    private TimeOnly GetStartTime(WorkScheduleEntry? lastWorkSchedule, Incident incident, Engineer engineer)
    {
        if (lastWorkSchedule is not null) return lastWorkSchedule.EndAt;
       
        return incident.ReportedAt > engineer.AvailableFrom ? incident.ReportedAt : engineer.AvailableFrom;
    }
    
    private TimeOnly GetEndTime(TimeOnly startAt, Incident incident)
    {
        return startAt.AddMinutes(incident.EstimatedMinutes);
    }
}