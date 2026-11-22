namespace Scheduler.Api.Contracts;

public sealed record SchedulingSettingsResponse(string ClientId, int MaxShiftsPerEmployee);