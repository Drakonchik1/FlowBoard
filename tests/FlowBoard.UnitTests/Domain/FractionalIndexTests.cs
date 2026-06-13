using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.ValueObjects;

namespace FlowBoard.UnitTests.Domain;

public sealed class FractionalIndexTests
{
    private static int Compare(FractionalIndex a, FractionalIndex b) =>
        string.CompareOrdinal(a.Value, b.Value);

    [Fact]
    public void Between_NullNull_ReturnsStableStartKey()
    {
        var first = FractionalIndex.Between(null, null);
        Assert.Equal(FractionalIndex.Start().Value, first.Value);
    }

    [Fact]
    public void Between_AppendingRepeatedly_StaysAscending()
    {
        FractionalIndex? last = null;
        var keys = new List<FractionalIndex>();

        for (var i = 0; i < 200; i++)
        {
            var next = FractionalIndex.Between(last, null);
            if (last is not null)
                Assert.True(Compare(last, next) < 0, $"append {i}: {last.Value} !< {next.Value}");
            keys.Add(next);
            last = next;
        }

        AssertStrictlyAscending(keys);
    }

    [Fact]
    public void Between_PrependingRepeatedly_StaysAscending()
    {
        FractionalIndex? first = null;
        var keys = new List<FractionalIndex>();

        for (var i = 0; i < 200; i++)
        {
            var next = FractionalIndex.Between(null, first);
            if (first is not null)
                Assert.True(Compare(next, first) < 0, $"prepend {i}: {next.Value} !< {first.Value}");
            keys.Add(next);
            first = next;
        }
    }

    [Fact]
    public void Between_TwoKeys_ProducesValueStrictlyBetween()
    {
        var a = FractionalIndex.Between(null, null);
        var c = FractionalIndex.Between(a, null);

        var b = FractionalIndex.Between(a, c);

        Assert.True(Compare(a, b) < 0);
        Assert.True(Compare(b, c) < 0);
    }

    [Fact]
    public void Between_RepeatedMidpointInsertion_AlwaysStaysBetween()
    {
        var low = FractionalIndex.Between(null, null);
        var high = FractionalIndex.Between(low, null);

        // Insert 500 times into the same gap; each must remain strictly between the latest neighbours.
        for (var i = 0; i < 500; i++)
        {
            var mid = FractionalIndex.Between(low, high);
            Assert.True(Compare(low, mid) < 0, $"iteration {i}: {low.Value} !< {mid.Value}");
            Assert.True(Compare(mid, high) < 0, $"iteration {i}: {mid.Value} !< {high.Value}");
            high = mid; // keep shrinking the gap toward `low`
        }
    }

    [Fact]
    public void Between_SimulatedDragReordering_KeepsGlobalOrder()
    {
        // Build an initial ordered list, then repeatedly move random items to random positions
        // and assert the position keys always reflect the intended order.
        var rng = new Random(12345);
        var items = new List<FractionalIndex>();

        FractionalIndex? last = null;
        for (var i = 0; i < 20; i++)
        {
            last = FractionalIndex.Between(last, null);
            items.Add(last);
        }

        for (var move = 0; move < 1000; move++)
        {
            var fromIndex = rng.Next(items.Count);
            var item = items[fromIndex];
            items.RemoveAt(fromIndex);

            var toIndex = rng.Next(items.Count + 1);
            var before = toIndex > 0 ? items[toIndex - 1] : null;
            var after = toIndex < items.Count ? items[toIndex] : null;

            var newKey = FractionalIndex.Between(before, after);
            items.Insert(toIndex, newKey);
        }

        AssertStrictlyAscending(items);
    }

    [Fact]
    public void Create_RejectsEmpty()
    {
        Assert.Throws<DomainException>(() => FractionalIndex.Create(""));
    }

    [Fact]
    public void Create_RejectsInvalidCharacters()
    {
        Assert.Throws<DomainException>(() => FractionalIndex.Create("ab*"));
    }

    [Fact]
    public void Create_RejectsTrailingLowestDigit()
    {
        // Lowest digit is '0'; trailing it would make a non-canonical key.
        Assert.Throws<DomainException>(() => FractionalIndex.Create("V0"));
    }

    [Fact]
    public void Between_RejectsOutOfOrderBounds()
    {
        var a = FractionalIndex.Create("k");
        var b = FractionalIndex.Create("V"); // 'V' < 'k', so (a, b) is out of order
        Assert.Throws<DomainException>(() => FractionalIndex.Between(a, b));
    }

    private static void AssertStrictlyAscending(IReadOnlyList<FractionalIndex> keys)
    {
        for (var i = 1; i < keys.Count; i++)
        {
            Assert.True(
                string.CompareOrdinal(keys[i - 1].Value, keys[i].Value) < 0,
                $"keys not ascending at {i}: '{keys[i - 1].Value}' !< '{keys[i].Value}'");
        }
    }
}
