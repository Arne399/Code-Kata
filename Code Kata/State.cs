using Code_Kata.Entities.Engineers;
using Code_Kata.Entities.Incidents;
using Code_Kata.Entities.WorkSchedules;

namespace Code_Kata;

public static class State
{
    public static List<Engineer> Engineers { get; set; }
    public static List<Incident> Incidents  { get; set; }
    public static List<WorkScheduleEntry> WorkSchedules  { get; set; }
}