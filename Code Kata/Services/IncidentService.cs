using Code_Kata.Entities.Incidents;

namespace Code_Kata.Services;

public class IncidentService
{
	public IReadOnlyList<Incident> GetPendingIncidents()
	{
		return State.Incidents
			.Where(incident => incident.Status == IncidentStatus.New)
			.OrderBy(incident => incident.Deadline)
			.ThenByDescending(incident => incident.Severity * incident.Impact)
			.ThenBy(incident => incident.ReportedAt)
			.ThenBy(incident => incident.Id)
			.ToList();
	}

	public void MarkScheduled(Incident incident, string engineerId)
	{
		incident.AssignedEngineerId = engineerId;
		incident.Status = IncidentStatus.Scheduled;
	}

	public void MarkAtRisk(Incident incident)
	{
		incident.Status = IncidentStatus.AtRisk;
	}
}