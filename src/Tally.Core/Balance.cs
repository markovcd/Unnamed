using System.Collections.Immutable;

namespace Tally.Core;

/// <summary>A total held per commodity. A commodity that sums to zero is absent.</summary>
internal sealed class Balance
{
    private readonly ImmutableSortedDictionary<string, decimal> totals;

    private Balance(ImmutableSortedDictionary<string, decimal> totals) => this.totals = totals;

    public static Balance Empty { get; } = new(ImmutableSortedDictionary.Create<string, decimal>(StringComparer.Ordinal));

    public bool IsZero => totals.IsEmpty;

    /// <summary>One amount per commodity, ordered by commodity.</summary>
    public IEnumerable<Amount> Amounts => totals.Select(pair => new Amount(pair.Value, pair.Key));

    public decimal Of(string commodity) => totals.GetValueOrDefault(commodity);

    public Balance Plus(Amount amount)
    {
        var sum = Of(amount.Commodity) + amount.Quantity;
        return new Balance(sum == 0 ? totals.Remove(amount.Commodity) : totals.SetItem(amount.Commodity, sum));
    }

    public Balance Plus(Balance other) => other.Amounts.Aggregate(this, (balance, amount) => balance.Plus(amount));

    public Balance Negated() => Amounts.Aggregate(Empty, (balance, amount) => balance.Plus(amount with { Quantity = -amount.Quantity }));

    public override string ToString() => IsZero ? "0" : string.Join(", ", Amounts);
}
