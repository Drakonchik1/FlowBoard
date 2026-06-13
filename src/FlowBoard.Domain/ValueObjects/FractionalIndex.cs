using System.Text;
using FlowBoard.Domain.Common;
using FlowBoard.Domain.Exceptions;

namespace FlowBoard.Domain.ValueObjects;

/// <summary>
/// A position key for ordering items (cards, lists) that supports inserting between any two
/// neighbours without renumbering siblings — "fractional indexing".
///
/// The string is interpreted as a base-62 fraction (an implicit "0." prefix) over the alphabet
/// 0-9 A-Z a-z, which is in ascending ASCII order. This guarantees that ordinal/lexicographic
/// string comparison matches numeric ordering, so the database can <c>ORDER BY Position</c> directly.
///
/// Keys never end in the lowest digit ('0') so each value has a single canonical representation
/// and lexicographic order stays consistent across different lengths.
/// </summary>
public sealed class FractionalIndex : ValueObject
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const int Base = 62;

    public string Value { get; }

    private FractionalIndex(string value) => Value = value;

    /// <summary>Validates and creates an index. Throws <see cref="DomainException"/> on invalid input.</summary>
    public static FractionalIndex Create(string value)
    {
        if (string.IsNullOrEmpty(value))
            throw new DomainException("Fractional index cannot be empty.");

        foreach (var c in value)
        {
            if (Alphabet.IndexOf(c) < 0)
                throw new DomainException($"Fractional index contains invalid character '{c}'.");
        }

        if (value[^1] == Alphabet[0])
            throw new DomainException("Fractional index must not end with the lowest digit.");

        return new FractionalIndex(value);
    }

    /// <summary>Bypasses validation — used by EF Core value converter for materialization.</summary>
    internal static FractionalIndex FromTrustedSource(string value) => new(value);

    /// <summary>The position for the first item in an empty collection.</summary>
    public static FractionalIndex Start() => new(Alphabet[Base / 2].ToString());

    /// <summary>
    /// Generates a key strictly between <paramref name="before"/> and <paramref name="after"/>.
    /// Pass null for an open boundary: <c>Between(null, first)</c> prepends, <c>Between(last, null)</c> appends,
    /// and <c>Between(null, null)</c> returns the first key for an empty collection.
    /// </summary>
    public static FractionalIndex Between(FractionalIndex? before, FractionalIndex? after) =>
        new(Between(before?.Value, after?.Value));

    private static string Between(string? before, string? after)
    {
        if (before is not null && after is not null && string.CompareOrdinal(before, after) >= 0)
            throw new DomainException("`before` must be strictly less than `after`.");

        if (before is null && after is null)
            return Alphabet[Base / 2].ToString();

        if (before is null)
            return Halve(after!);

        if (after is null)
            return Increment(before);

        return Midpoint(before, after);
    }

    /// <summary>Returns a key in the open interval (0, b) — used to prepend before the first item.</summary>
    private static string Halve(string b)
    {
        var digits = new List<int>(b.Length + 1);
        var remainder = 0;
        foreach (var c in b)
        {
            var current = remainder * Base + Index(c);
            digits.Add(current / 2);
            remainder = current % 2;
        }

        if (remainder != 0)
            digits.Add(Base / 2);

        return Stringify(digits);
    }

    /// <summary>Returns a key greater than <paramref name="a"/> — used to append after the last item.</summary>
    private static string Increment(string a)
    {
        var i = 0;
        while (i < a.Length && Index(a[i]) == Base - 1)
            i++;

        if (i == a.Length)
            return a + Alphabet[Base / 2];

        var nextDigit = (Index(a[i]) + Base) / 2;
        return a[..i] + Alphabet[nextDigit];
    }

    /// <summary>Returns the numeric midpoint of two finite fractions a &lt; b.</summary>
    private static string Midpoint(string a, string b)
    {
        var length = Math.Max(a.Length, b.Length);

        var sum = new int[length];
        var carry = 0;
        for (var i = length - 1; i >= 0; i--)
        {
            var da = i < a.Length ? Index(a[i]) : 0;
            var db = i < b.Length ? Index(b[i]) : 0;
            var s = da + db + carry;
            sum[i] = s % Base;
            carry = s / Base;
        }

        var digits = new List<int>(length + 1);
        var remainder = carry;
        for (var i = 0; i < length; i++)
        {
            var current = remainder * Base + sum[i];
            digits.Add(current / 2);
            remainder = current % 2;
        }

        if (remainder != 0)
            digits.Add(Base / 2);

        return Stringify(digits);
    }

    private static string Stringify(List<int> digits)
    {
        var end = digits.Count;
        while (end > 0 && digits[end - 1] == 0)
            end--;

        if (end == 0)
            return Alphabet[Base / 2].ToString();

        var sb = new StringBuilder(end);
        for (var i = 0; i < end; i++)
            sb.Append(Alphabet[digits[i]]);

        return sb.ToString();
    }

    private static int Index(char c) => Alphabet.IndexOf(c);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(FractionalIndex index) => index.Value;
    public override string ToString() => Value;
}
