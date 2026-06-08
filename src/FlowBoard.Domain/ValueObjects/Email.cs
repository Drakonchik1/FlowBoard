using System.Text.RegularExpressions;
using FlowBoard.Domain.Common;
using FlowBoard.Domain.Exceptions;

namespace FlowBoard.Domain.ValueObjects;

/// <summary>
/// Strongly-typed email address. Validates format on creation and stores lowercase.
/// Provides implicit conversion to string for EF Core value conversion.
/// </summary>
public sealed partial class Email : ValueObject
{
    public string Value { get; }

    private Email(string value) => Value = value;

    /// <summary>Creates a validated, normalized email. Throws DomainException on invalid input.</summary>
    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email cannot be empty.");

        email = email.Trim().ToLowerInvariant();

        if (!IsValidFormat(email))
            throw new DomainException($"'{email}' is not a valid email address.");

        return new Email(email);
    }

    /// <summary>
    /// Bypasses validation. Used by the EF Core value converter when materializing
    /// a row already persisted (the database is the source of truth — re-validating
    /// every read costs allocations and would block schema migrations from tightening rules).
    /// Never call from application code.
    /// </summary>
    internal static Email FromTrustedSource(string value) => new(value);

    /// <summary>
    /// Normalizes user input (trim + lowercase) without constructing the value object.
    /// Used by repositories for lookup queries.
    /// </summary>
    public static string Normalize(string email) =>
        (email ?? string.Empty).Trim().ToLowerInvariant();

    private static bool IsValidFormat(string email) => EmailRegex().IsMatch(email);

    // Stricter than MailAddress: requires non-empty local, non-empty multi-part domain with TLD.
    // Aligned with HTML5 input[type=email] validation. Real deliverability is verified via confirmation email.
    [GeneratedRegex(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(Email email) => email.Value;
    public override string ToString() => Value;
}
