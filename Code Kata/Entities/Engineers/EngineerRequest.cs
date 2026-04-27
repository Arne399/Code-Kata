namespace Code_Kata.Entities.Engineers;

public record EngineerRequest(
    TimeOnly Deadline,
    TimeOnly ReportedAt,
    Decimal PriorityScore,
    SkillType SkillType,
    int EstimatedMinutes);