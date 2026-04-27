using Code_Kata.Entities.Engineers;

namespace Code_Kata.Services;

public class EngineerService
{
    public List<Engineer> GetAvailableEngineers(EngineerRequest request)
    {
        return State.Engineers.Where(e => e.Skills.Contains(request.SkillType) && IncidentFitsWithinSchedule(e, request)).ToList();
    }

    private bool IncidentFitsWithinSchedule(Engineer engineer, EngineerRequest request)
    {
        bool deadlineWithinWorkSchedule =
            request.Deadline > engineer.AvailableFrom && request.Deadline <= engineer.AvailableUntil;
        return deadlineWithinWorkSchedule;
    }
}