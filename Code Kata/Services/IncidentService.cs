using Code_Kata.Entities.Incidents;

namespace Code_Kata.Services;

public class IncidentService
{
	private readonly ScoringService _scoringService;

	public IncidentService()
		: this(new ScoringService())
	{
	}

	public IncidentService(ScoringService scoringService)
	{
		_scoringService = scoringService;
	}

	public IReadOnlyList<Incident> GetPendingIncidents()
	{
		return State.Incidents
			.Where(incident => incident.Status == IncidentStatus.New)
			.OrderByDescending(incident => _scoringService.GetUnresolvedPenalty(incident))
			.ThenBy(incident => incident.Deadline)
			.ThenBy(incident => incident.ReportedAt)
			.ThenByDescending(incident => incident.EstimatedMinutes)
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