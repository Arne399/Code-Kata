namespace Code_Kata.Entities.WorkSchedules;

public class WorkScheduleEntry
{
    public string Id { get; set; } = string.Empty;
    public string EngineerId { get; set; } = string.Empty;
    public string IncidentId { get; set; } = string.Empty;
    public TimeOnly StartAt { get; set; }
    public TimeOnly EndAt { get; set; }
    public int Minutes { get; set; }
}