using NodaTime;
using System;

namespace N3O.Umbraco.Localization;

public partial class DateTimeFormatter {
    public string FormatDate(LocalDateTime? localDateTime, DateFormat dateFormat = null) {
        return Format(localDateTime, dateFormat, FormatDate);
    }

    public string FormatDate(LocalDateTime? localDateTime, string specifier) {
        return Format(localDateTime, (DateFormat) null, (dt, _) => FormatDate(dt, specifier));
    }

    public string FormatTime(LocalDateTime? localDateTime, TimeFormat timeFormat = null) {
        return Format(localDateTime, timeFormat, FormatTime);
    }

    public string FormatTime(LocalDateTime? localDateTime, string specifier) {
        return Format(localDateTime, (DateFormat) null, (dt, _) => FormatTime(dt, specifier));
    }

    public string FormatDateWithTime(LocalDateTime? localDateTime) {
        if (localDateTime == null) {
            return null;
        }

        return $"{FormatDate(localDateTime)} {FormatTime(localDateTime)}";
    }

    private string Format<TDateOrTimeFormat>(LocalDateTime? localDateTime,
                                             TDateOrTimeFormat format,
                                             Func<DateTime?, TDateOrTimeFormat, string> formatter) {
        if (localDateTime == null) {
            return null;
        }

        var dateTime = localDateTime.Value.ToDateTimeUnspecified();

        return formatter(dateTime, format);
    }
}
