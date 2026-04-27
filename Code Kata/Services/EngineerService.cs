using Code_Kata.Entities.Engineers;
using Code_Kata.Entities;

namespace Code_Kata.Services;

public class EngineerService
{
    public IReadOnlyList<Engineer> GetQualifiedEngineers(IEnumerable<Engineer> engineers, SkillType skillType)
    {
        return engineers
            .Where(engineer => HasSkill(engineer, skillType))
            .OrderBy(engineer => engineer.Id)
            .ToList();
    }

    private bool HasSkill(Engineer engineer, SkillType skillType)
    {
        return engineer.Skills.Contains(skillType);
    }
}