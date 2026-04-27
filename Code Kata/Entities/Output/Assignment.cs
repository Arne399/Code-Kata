namespace Code_Kata.Entities.Output;

public class Assignment
{
    public string IncidentId { get; set; } = string.Empty;
    public string EngineerId { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}