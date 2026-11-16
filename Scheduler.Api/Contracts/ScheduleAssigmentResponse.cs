namespace Scheduler.Api.Contracts;

public record ScheduleAssignmentResponse(int ShiftId, int EmployeeId);

public sealed record ScheduleResponse(int Id, string ClientId, DateTime PeriodStart,
    DateTime PeriodEnd, DateTime CreatedAt, List<ScheduleAssignmentResponse> Assignments);