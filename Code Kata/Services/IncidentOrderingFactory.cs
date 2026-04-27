using Code_Kata.Entities.Incidents;

namespace Code_Kata.Services;

internal sealed class IncidentOrderingFactory
{
    private readonly ScoringService _scoringService;

    public IncidentOrderingFactory(ScoringService scoringService)
    {
        _scoringService = scoringService;
    }

    public IEnumerable<IReadOnlyList<int>> Create(IReadOnlyList<Incident> incidents)
    {
        var indexedIncidents = incidents
            .Select((incident, index) => new IndexedIncident(index, incident))
            .ToList();

        yield return OrderByUnresolvedPenalty(indexedIncidents);
        yield return OrderByDeadline(indexedIncidents);
        yield return OrderByWeightedImpact(indexedIncidents);
        yield return OrderByPenaltyDensity(indexedIncidents);
    }

    private int[] OrderByUnresolvedPenalty(IReadOnlyList<IndexedIncident> indexedIncidents)
    {
        return indexedIncidents
            .OrderByDescending(item => _scoringService.GetUnresolvedPenalty(item.Incident))
            .ThenBy(item => item.Incident.Deadline)
            .ThenBy(item => item.Incident.ReportedAt)
            .Select(item => item.Index)
            .ToArray();
    }

    private int[] OrderByDeadline(IReadOnlyList<IndexedIncident> indexedIncidents)
    {
        return indexedIncidents
            .OrderBy(item => item.Incident.Deadline)
            .ThenByDescending(item => _scoringService.GetUnresolvedPenalty(item.Incident))
            .ThenBy(item => item.Incident.ReportedAt)
            .Select(item => item.Index)
            .ToArray();
    }

    private int[] OrderByWeightedImpact(IReadOnlyList<IndexedIncident> indexedIncidents)
    {
        return indexedIncidents
            .OrderByDescending(item => _scoringService.GetWeightedImpact(item.Incident))
            .ThenBy(item => item.Incident.Deadline)
            .ThenByDescending(item => item.Incident.EstimatedMinutes)
            .Select(item => item.Index)
            .ToArray();
    }

    private int[] OrderByPenaltyDensity(IReadOnlyList<IndexedIncident> indexedIncidents)
    {
        return indexedIncidents
            .OrderByDescending(item => (double)_scoringService.GetUnresolvedPenalty(item.Incident) / Math.Max(1, item.Incident.EstimatedMinutes))
            .ThenBy(item => item.Incident.Deadline)
            .ThenBy(item => item.Incident.ReportedAt)
            .Select(item => item.Index)
            .ToArray();
    }

    private readonly record struct IndexedIncident(int Index, Incident Incident);
}

