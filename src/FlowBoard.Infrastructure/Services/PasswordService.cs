using FlowBoard.Application.Common.Interfaces;

namespace FlowBoard.Infrastructure.Services;

/// <summary>
/// BCrypt password hashing. Work factor 12 is the current industry-standard baseline.
/// Verify uses BCrypt's built-in constant-time comparison — no manual timing-safe compare needed.
/// </summary>
internal sealed class PasswordService : IPasswordService
{
    private const int WorkFactor = 12;

    // Pre-computed at type load — hash of "timing-safe-dummy-password"
    public string TimingSafeDummyHash { get; } =
        BCrypt.Net.BCrypt.HashPassword("timing-safe-dummy-password", WorkFactor);

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
