using Scheduler.Api.Domain;

namespace Scheduler.Api.Application;

public sealed record ScheduleGenerationResult(WorkSchedule Schedule, List<int> UnfilledShiftIds);