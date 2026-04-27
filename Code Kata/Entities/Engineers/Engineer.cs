namespace Code_Kata.Entities.Engineers;

public class Engineer
{
    public string Id { get; set; }
    public string Name { get; set; }
    public TimeOnly AvailableFrom { get; set; }
    public TimeOnly AvailableUntil { get; set; }
    public List<SkillType> Skills { get; set; }
    public int MaxWorkMinutes { get; set; }   
}