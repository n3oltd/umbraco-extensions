using N3O.Umbraco.Financial;
using System;
using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.Extensions;

public static class MoneyExtensions {
    public static int GetAmountInLowestDenomination(this Money money) {
        var factor = (decimal) Math.Pow(10, money.Currency.DecimalDigits);

        var amount = (int) (money.Amount * factor);

        return amount;
    }

    public static bool IsZero(this Money money) {
        return money.Amount == 0;
    }

    public static Money RoundUpToWholeNumber(this Money money) {
        return new Money(Math.Ceiling(money.Amount), money.Currency);
    }

    public static IEnumerable<Money> SafeDivide(this Money money, int shares) {
        return SafeDivide(money, shares, MidpointRounding.ToEven);
    }

    public static IEnumerable<Money> SafeDivide(this Money money, int shares, MidpointRounding rounding) {
        if (shares <= 1) {
            throw new ArgumentOutOfRangeException(nameof(shares), "Number of shares must be greater than 1");
        }

        return SafeDivideIterator();

        IEnumerable<Money> SafeDivideIterator() {
            var shareAmount = Math.Round(money.Amount / shares, (int) money.Currency.DecimalDigits, rounding);
            var remainder = money.Amount;

            for (int i = 0; i < shares - 1; i++) {
                remainder -= shareAmount;
            
                yield return new Money(shareAmount, money.Currency);
            }

            yield return new Money(remainder, money.Currency);
        }
    }

    public static IEnumerable<Money> SafeDivide(this Money money, int[] ratios) {
        return SafeDivide(money, ratios, MidpointRounding.ToEven);
    }

    public static IEnumerable<Money> SafeDivide(this Money money, int[] ratios, MidpointRounding rounding) {
        if (ratios == null) {
            throw new ArgumentNullException(nameof(ratios));
        }

        if (ratios.Any(ratio => ratio < 1)) {
            throw new ArgumentOutOfRangeException(nameof(ratios), "All ratios must be greater or equal than 1");
        }

        return SafeDivideIterator();

        IEnumerable<Money> SafeDivideIterator() {
            decimal remainder = money.Amount;

            for (int i = 0; i < ratios.Length - 1; i++) {
                var ratioAmount = Math.Round(money.Amount * ratios[i] / ratios.Sum(),
                                             (int) money.Currency.DecimalDigits,
                                             rounding);

                remainder -= ratioAmount;

                yield return new Money(ratioAmount, money.Currency);
            }

            yield return new Money(remainder, money.Currency);
        }
    }

    public static IReadOnlyList<Money> Split(this Money money, int shares) {
        var result = money.SafeDivide(shares).ToList();

        return result;
    }

    public static IEnumerable<(Money Split, T Item)> SplitAcross<T>(this Money money, IEnumerable<T> items) {
        var list = items.ToList();
        var shares = list.Count;
        var splitMoney = money.Split(shares);

        for (var i = 0; i < shares; i++) {
            yield return (splitMoney[i], list[i]);
        }
    }

    public static Money Sum(this IEnumerable<Money> collection) {
        var list = collection.OrEmpty()
                             .ToList();

        if (list.None()) {
            throw new Exception($"{nameof(collection)} must contain at least one element");
        }

        var first = list.First();
        var rest = list.Skip(1)
                       .ToList();

        var sum = first;
        foreach (var item in rest) {
            sum += item;
        }

        return sum;
    }

    public static Money Zero(this Money money) {
        return new Money(0, money.Currency);
    }
}
