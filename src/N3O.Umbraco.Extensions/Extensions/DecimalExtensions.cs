using System;
using System.Collections.Generic;

namespace N3O.Umbraco.Extensions;

public static class DecimalExtensions {
    public static bool HasValue(this decimal? value) {
        if (value == null) {
            return false;
        }

        return true;
    }

    public static bool IsGreaterThanZero(this decimal value) {
        return value > 0;
    }

    public static bool IsZero(this decimal value) {
        return value == 0;
    }

    public static decimal? RoundMoney(this decimal? amount) {
        if (amount == null) {
            return null;
        } else {
            return RoundMoney(amount.Value);
        }
    }
  
    public static decimal RoundMoney(this decimal amount) {
        return Math.Round(amount, 2, MidpointRounding.AwayFromZero);
    }
    
    public static IEnumerable<decimal> SafeDivide(this decimal value, int shares) {
        if (shares < 1) {
            throw new ArgumentOutOfRangeException(nameof(shares), "Number of shares must be greater than 1");
        }

        if (shares == 1) {
            yield return value;
          
            yield break;
        }

        var shareAmount = value / shares;

        var remainder = value;
      
        for (var i = 0; i < shares - 1; i++) {
            remainder -= shareAmount;
            
            yield return shareAmount;
        }

        yield return remainder;
    }
}