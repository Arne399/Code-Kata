using Code_Kata.Entities.Engineers;

namespace Code_Kata.Parsers;
 
public static class EngineerParser
{
    public static List<Engineer> GetEngineers(string json)
    {
        return JsonParser.ParseCollection<Engineer>(json, "engineers");
    }
}	