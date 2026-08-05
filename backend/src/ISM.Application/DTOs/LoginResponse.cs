namespace ISM.Application.DTOs;

public record LoginResponse(string Username, string Email, string Role, string Token);
