namespace GoodDeedsApi.Models.Dtos;

public record RegisterRequest(string Email, string Password);

public record LoginRequest(string Email, string Password);

public record RefreshRequest(string RefreshToken);
