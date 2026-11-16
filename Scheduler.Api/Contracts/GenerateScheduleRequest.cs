namespace Scheduler.Api.Contracts;

public sealed record GenerateScheduleRequest(DateTime PeriodStart, DateTime PeriodEnd);