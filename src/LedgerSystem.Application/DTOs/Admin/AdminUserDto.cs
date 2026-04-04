namespace LedgerSystem.Application.DTOs.Admin;

public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string Role,
    DateTime CreatedAt);
