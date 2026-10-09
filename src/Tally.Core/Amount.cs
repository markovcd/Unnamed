using System.Globalization;

namespace Tally.Core;

/// <summary>A quantity of one commodity. An empty commodity is a bare number.</summary>
internal readonly record struct Amount(decimal Quantity, string Commodity)
{
    /// <summary>Largest absolute quantity; keeps sums over a whole journal far from decimal overflow.</summary>
    public const decimal MaxQuantity = 1_000_000_000_000m;

    private const int MaxCommodityLength = 16;

    public static Result<Amount> Parse(string text)
    {
        var trimmed = text.Trim();
        var numberLength = 0;
        while (numberLength < trimmed.Length && trimmed[numberLength] is '+' or '-' or '.' or (>= '0' and <= '9'))
        {
            numberLength++;
        }

        var number = trimmed[..numberLength];
        var commodity = trimmed[numberLength..].TrimStart();

        if (!IsPlainNumber(number))
        {
            return Result<Amount>.Fail($"'{Excerpt(trimmed)}' is not an amount; expected a number such as 12.50 USD");
        }

        if (!decimal.TryParse(number, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var quantity)
            || Math.Abs(quantity) > MaxQuantity)
        {
            return Result<Amount>.Fail($"'{Excerpt(number)}' is outside the range of -{MaxQuantity:0} to {MaxQuantity:0}");
        }

        var touching = commodity.Length > 0 && numberLength < trimmed.Length && !char.IsWhiteSpace(trimmed[numberLength]);
        if (touching && commodity.Any(char.IsAsciiDigit))
        {
            return Result<Amount>.Fail($"'{Excerpt(trimmed)}' is ambiguous; put a space between the number and a commodity that contains digits");
        }

        if (!IsCommodity(commodity))
        {
            return Result<Amount>.Fail($"'{Excerpt(commodity)}' is not a commodity; expected letters and digits starting with a letter, at most {MaxCommodityLength} long");
        }

        return Result<Amount>.Ok(new Amount(quantity, commodity));
    }

    public override string ToString()
    {
        var number = Quantity.ToString(CultureInfo.InvariantCulture);
        return Commodity.Length == 0 ? number : $"{number} {Commodity}";
    }

    private static bool IsPlainNumber(string text)
    {
        var rest = text.AsSpan();
        if (rest.Length > 0 && rest[0] is '+' or '-')
        {
            rest = rest[1..];
        }

        var point = rest.IndexOf('.');
        var whole = point < 0 ? rest : rest[..point];
        var fraction = point < 0 ? [] : rest[(point + 1)..];

        return whole.Length > 0 && IsDigits(whole) && (point < 0 || (fraction.Length > 0 && IsDigits(fraction)));
    }

    private static bool IsDigits(ReadOnlySpan<char> text)
    {
        foreach (var c in text)
        {
            if (!char.IsAsciiDigit(c))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsCommodity(string text)
    {
        if (text.Length == 0)
        {
            return true;
        }

        return text.Length <= MaxCommodityLength
            && char.IsAsciiLetter(text[0])
            && text.All(char.IsAsciiLetterOrDigit);
    }

    private static string Excerpt(string text) => text.Length <= 40 ? text : text[..40] + "...";
}
