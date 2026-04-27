using Code_Kata.Entities.Engineers;
using Code_Kata.Entities.Incidents;
using Code_Kata.Entities.WorkSchedules;

namespace Code_Kata.Services;

using Code_Kata.Entities.Engineers;
using Code_Kata.Entities.Incidents;
using Code_Kata.Entities.WorkSchedules;

public class WorkScheduleService {

	public WorkScheduleEntry OptimizeSchedule(List<Engineer> engineers, Incident incident){
		var current = TimeOnly.FromDateTime(DateTime.Now);
		var possible = new List<(Engineer eng, TimeOnly newStart, List<WorkScheduleEntry> newSchedule)>();

		foreach (var eng in engineers) {
			if (!eng.Skills.Contains(incident.SkillType)) continue;

			var allEntries = State.WorkSchedules.Where(ws => ws.EngineerId == eng.Id).ToList();
			var fixedEntries = allEntries.Where(e => e.EndAt <= current).OrderBy(e => e.StartAt).ToList();
			var pendingEntries = allEntries.Where(e => e.EndAt > current).ToList();
			var pendingIncidents = State.Incidents.Where(i => pendingEntries.Select(e => e.IncidentId).Contains(i.Id)).ToList();
			pendingIncidents.Add(incident);

			var sortedPending = pendingIncidents.OrderByDescending(i => i.Severity * i.Impact).ThenBy(i => i.ReportedAt).ThenBy(i => i.Id).ToList();

			var startTime = eng.AvailableFrom;
			if (fixedEntries.Any()) {
				startTime = TimeOnly.Max(startTime, fixedEntries.Last().EndAt);
			}

			var newPendingEntries = new List<WorkScheduleEntry>();
			TimeOnly? newIncStart = null;
			bool canFit = true;

			foreach (var inc in sortedPending) {
				var start = TimeOnly.Max(startTime, inc.ReportedAt);
				var end = start.AddMinutes(inc.EstimatedMinutes);

				if (end > eng.AvailableUntil || end > inc.Deadline) {
					canFit = false;
					break;
				}

				var entry = new WorkScheduleEntry {
					EngineerId = eng.Id,
					IncidentId = inc.Id,
					StartAt = start,
					EndAt = end
				};

				newPendingEntries.Add(entry);

				if (inc == incident) newIncStart = start;

				startTime = end;
			}

			if (!canFit) continue;

			var totalPendingMinutes = newPendingEntries.Sum(e => e.Minutes);
			var totalFixedMinutes = fixedEntries.Sum(e => e.Minutes);
			if (totalPendingMinutes + totalFixedMinutes > eng.MaxWorkMinutes) continue;

			var newSchedule = fixedEntries.Concat(newPendingEntries).ToList();
			possible.Add((eng, newIncStart.Value, newSchedule));
		}

		if (!possible.Any()) return null;

		var best = possible.OrderBy(p => p.newStart).First();

		
		State.WorkSchedules.RemoveAll(ws => ws.EngineerId == best.eng.Id);
		State.WorkSchedules.AddRange(best.newSchedule);
		incident.AssignedEngineerId = best.eng.Id;

		return best.newSchedule.First(ws => ws.IncidentId == incident.Id);
	}
}
