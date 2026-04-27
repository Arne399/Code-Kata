using Code_Kata;
using Code_Kata.Parsers;

Console.WriteLine("Please give a json input:");
string json = Console.ReadLine();

State.Engineers = EngineerParser.GetEngineers(json);
State.Incidents = IncidentParser.GetIncidents(json);



