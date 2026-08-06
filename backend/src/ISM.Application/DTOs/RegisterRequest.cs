namespace ISM.Application.DTOs;

public record RegisterRequest(string Username, string Email, string Password, string Role);
