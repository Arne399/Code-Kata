using Code_Kata; 
using Code_Kata.Parsers;
using System.Text;
using Code_Kata.Services;

Console.Clear();
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("========================================");
Console.WriteLine("                Code Kata               "); 
Console.WriteLine("========================================");
Console.ResetColor();
Console.WriteLine();

Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("Please enter JSON input (press Enter on an empty line to finish):");
Console.ResetColor();

var inputBuilder = new StringBuilder();

while (true)
{
	var line = Console.ReadLine();

	if (line is null)
	{
		break;
	}

	if (string.IsNullOrWhiteSpace(line) && inputBuilder.Length > 0)
	{
		break;
	}

	if (inputBuilder.Length > 0)
	{
		inputBuilder.AppendLine();
	}

	inputBuilder.Append(line);
}

string json = inputBuilder.ToString();

if (string.IsNullOrWhiteSpace(json))
{
	Console.WriteLine();
	Console.ForegroundColor = ConsoleColor.Red;
	Console.WriteLine("Input cannot be empty. Please restart and provide valid JSON.");
	Console.ResetColor();
	return;
}

State.Engineers = EngineerParser.GetEngineers(json);
State.Incidents = IncidentParser.GetIncidents(json);

Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("JSON parsed successfully.");
Console.ResetColor();

IncidentService incidentService = new IncidentService();
EngineerService engineerService = new EngineerService();
WorkScheduleService workScheduleService = new WorkScheduleService();
AssignmentService assignmentService = new AssignmentService();

var pendingIncidents = incidentService.GetPendingIncidents();
	State.WorkScheduleEntries = assignmentService.AssignPendingIncidents();

Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Gray;
foreach (var workScheduleEntry in State.WorkScheduleEntries)
{
	var output = JsonParser.ToJson(workScheduleEntry);
	Console.WriteLine(output);
}
