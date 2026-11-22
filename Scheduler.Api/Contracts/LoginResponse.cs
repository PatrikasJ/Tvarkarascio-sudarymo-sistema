namespace Scheduler.Api.Contracts;

public sealed record LoginResponse(string AccessToken, string TokenType, DateTime ExpiresAt);