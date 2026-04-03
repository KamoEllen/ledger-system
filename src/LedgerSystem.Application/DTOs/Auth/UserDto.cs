namespace LedgerSystem.Application.DTOs.Auth;

public sealed record UserDto(Guid Id, string Email, string Role);
