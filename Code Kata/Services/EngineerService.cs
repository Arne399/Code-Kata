using Code_Kata.Entities.Engineers;
using Code_Kata.Entities;
using Code_Kata.Entities.WorkSchedules;

namespace Code_Kata.Services;

public class EngineerService
{
	public IReadOnlyList<Engineer> GetQualifiedEngineers(SkillType skillType)
	{
    return GetQualifiedEngineers(State.Engineers, skillType);
	}

  public IReadOnlyList<Engineer> GetQualifiedEngineers(IEnumerable<Engineer> engineers, SkillType skillType)
  {
    return engineers
      .Where(engineer => HasSkill(engineer, skillType))
      .OrderBy(engineer => engineer.Id)
      .ToList();
  }

    public List<Engineer> GetAvailableEngineers(EngineerRequest request)
    {
        return State.Engineers
            .Where(engineer => HasSkill(engineer, request.SkillType))
            .Where(engineer => HasWorkingTimeRemaining(engineer, request))
            .Where(engineer => GetRemainingWorkMinutes(engineer) >= request.EstimatedMinutes)
            .OrderBy(engineer => engineer.Id)
            .ToList();
    }

    private bool HasSkill(Engineer engineer, SkillType skillType)
    {
        return engineer.Skills.Contains(skillType);
    }

    private int GetRemainingWorkMinutes(Engineer engineer)
    {
        var usedMinutes = GetScheduleEntriesForEngineer(engineer.Id).Sum(entry => entry.Minutes);
        return engineer.MaxWorkMinutes - usedMinutes;
    }

    private IReadOnlyList<WorkScheduleEntry> GetScheduleEntriesForEngineer(string engineerId)
    {
        return State.WorkScheduleEntries
            .Where(entry => entry.EngineerId == engineerId)
            .OrderBy(entry => entry.StartAt)
            .ToList();
    }

    private bool HasWorkingTimeRemaining(Engineer engineer, EngineerRequest request)
    {
        var earliestStart = request.ReportedAt > engineer.AvailableFrom
            ? request.ReportedAt
            : engineer.AvailableFrom;

        if (earliestStart >= engineer.AvailableUntil)
        {
            return false;
        }

        var latestEnd = earliestStart.AddMinutes(request.EstimatedMinutes);
        return latestEnd <= engineer.AvailableUntil;
    }
}