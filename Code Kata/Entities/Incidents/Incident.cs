namespace Code_Kata.Entities.Incidents;

public class Incident
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public TimeOnly ReportedAt { get; set; }
    public TimeSpan Sla { get; set; }
    public int SlaMinutes
    {
        get => (int)Sla.TotalMinutes;
        set => Sla = TimeSpan.FromMinutes(value);
    }
    public SkillType Type { get; set; }
    public Severity Severity { get; set; }
    public int Impact { get; set; }
    public int EstimatedMinutes { get; set; }
    public IncidentStatus Status { get; set; } = IncidentStatus.New;
    public string? AssignedEngineerId { get; set; }

    public TimeOnly Deadline => ReportedAt.Add(Sla);
}