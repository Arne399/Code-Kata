using Code_Kata.Entities.Incidents;

namespace Code_Kata.Services;

public class ScoringService
{
    private const int UnresolvedPenaltyMultiplier = 10;

    public int GetSeverityFactor(Severity severity)
    {
        return severity switch
        {
            Severity.Laag => 1,
            Severity.Normaal => 2,
            Severity.Hoog => 3,
            Severity.Kritiek => 5,
            _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown severity.")
        };
    }

    public long GetWeightedImpact(Incident incident)
    {
        return (long)incident.Impact * GetSeverityFactor(incident.Severity);
    }

    public long GetUnresolvedPenalty(Incident incident)
    {
        return GetWeightedImpact(incident) * UnresolvedPenaltyMultiplier;
    }

    public long GetLatePenalty(Incident incident, TimeOnly completedAt)
    {
        var overdueMinutes = GetOverdueMinutes(incident, completedAt);
        return overdueMinutes <= 0 ? 0L : overdueMinutes * GetWeightedImpact(incident);
    }

    public int GetOverdueMinutes(Incident incident, TimeOnly completedAt)
    {
        var overdue = completedAt.ToTimeSpan() - incident.Deadline.ToTimeSpan();
        return overdue <= TimeSpan.Zero ? 0 : (int)Math.Ceiling(overdue.TotalMinutes);
    }
}

