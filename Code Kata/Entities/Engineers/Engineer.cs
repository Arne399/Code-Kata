namespace Code_Kata.Entities.Engineers;

public class Engineer
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public TimeOnly AvailableFrom { get; set; }
    public TimeOnly AvailableUntil { get; set; }
    public List<SkillType> Skills { get; set; } = new();
    public int MaxWorkMinutes { get; set; }
}