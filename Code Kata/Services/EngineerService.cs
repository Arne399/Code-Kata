using Code_Kata.Entities.Engineers;
using Code_Kata.Entities;
using Code_Kata.Entities.WorkSchedules;

namespace Code_Kata.Services;

public class EngineerService
{
    public List<Engineer> GetAvailableEngineers(EngineerRequest request)
    {
        return State.Engineers
            .Where(engineer => HasSkill(engineer, request.SkillType))
            .Where(engineer => IncidentFitsWithinSchedule(engineer, request))
            .Where(engineer => GetRemainingWorkMinutes(engineer) >= request.EstimatedMinutes)
            .ToList();
    }

    public bool HasSkill(Engineer engineer, SkillType skillType)
    {
        return engineer.Skills.Contains(skillType);
    }

    public int GetRemainingWorkMinutes(Engineer engineer)
    {
        var usedMinutes = GetScheduleEntriesForEngineer(engineer.Id).Sum(entry => entry.Minutes);
        return engineer.MaxWorkMinutes - usedMinutes;
    }

    public IReadOnlyList<WorkScheduleEntry> GetScheduleEntriesForEngineer(string engineerId)
    {
        return State.WorkSchedules
            .Where(entry => entry.EngineerId == engineerId)
            .OrderBy(entry => entry.StartAt)
            .ToList();
    }

    private bool IncidentFitsWithinSchedule(Engineer engineer, EngineerRequest request)
    {
        var reportedAtWithinShift = request.ReportedAt >= engineer.AvailableFrom && request.ReportedAt < engineer.AvailableUntil;
        var deadlineWithinShift = request.Deadline > engineer.AvailableFrom && request.Deadline <= engineer.AvailableUntil;
        return reportedAtWithinShift && deadlineWithinShift;
    }
}