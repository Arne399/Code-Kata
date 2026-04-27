using Code_Kata.Entities.Incidents;

namespace Code_Kata.Parsers;

public static class IncidentParser
{
    public static List<Incident> GetIncidents(string json)
    {
        return JsonParser.Parse<List<Incident>>(json);
    }
 
}