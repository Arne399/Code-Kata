namespace Code_Kata.Services;

public class WorkScheduleService {

	public WorkScheduleEntry OptimizeSchedule(Engineer engineer, Incident incident){
		var lastWorkSchedule = State.WorkSchedules.Where(ws => ws.EngineerId == engineer.Id).OrderBy(ws => ws.StartAt).LastOrDefault();

		var newEnty = new WorkScheduleEntry
		{	
			EngineerId = engineer.Id,
			IncidentId = Incident.Id,
			StartAt = lastWorkSchedule.EndAt,
			EndAt = lastWorkSchedule.EndAt.AddMinutes(incident.EstimatedMinutes)
		};

		State.WorkSchedules.add(newEntry);
		return newEntry;
	}
}
