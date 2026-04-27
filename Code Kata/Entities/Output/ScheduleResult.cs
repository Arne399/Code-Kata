namespace Code_Kata.Entities.Output;

public class ScheduleResult
{
    public List<Assignment> Assignments { get; set; } = [];
    public List<string> UnAssignedIncidents { get; set; } = [];
}