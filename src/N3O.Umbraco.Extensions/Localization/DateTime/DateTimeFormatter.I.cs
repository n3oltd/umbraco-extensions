using NodaTime;
using System;

namespace N3O.Umbraco.Localization;

public interface IDateTimeFormatter {
    string FormatDate(DateTime? dateTime, string specifier);
    string FormatDate(DateTime? dateTime, DateFormat dateFormat = null);
    
    string FormatDate(Instant? instant, DateFormat dateFormat = null);
    string FormatDate(Instant? instant, string specifier);
    
    string FormatDate(LocalDate? localDate, DateFormat dateFormat = null);
    string FormatDate(LocalDate? localDate, string specifier);
    
    string FormatDate(LocalDateTime? localDateTime, DateFormat dateFormat = null);
    string FormatDate(LocalDateTime? localDateTime, string specifier);
    
    string FormatDate(ZonedDateTime? zonedDateTime, DateFormat dateFormat = null);
    string FormatDate(ZonedDateTime? zonedDateTime, string specifier);

    string FormatTime(DateTime? dateTime, TimeFormat timeFormat = null);
    string FormatTime(DateTime? dateTime, string specifier);
    
    string FormatTime(Instant? instant, TimeFormat timeFormat = null);
    string FormatTime(Instant? instant, string specifier);
    
    string FormatTime(LocalTime? localTime, TimeFormat timeFormat = null);
    string FormatTime(LocalTime? localTime, string specifier);
    
    string FormatTime(LocalDateTime? localDateTime, TimeFormat timeFormat = null);
    string FormatTime(LocalDateTime? localDateTime, string specifier);
    
    string FormatTime(ZonedDateTime? zonedDateTime, TimeFormat timeFormat = null);
    string FormatTime(ZonedDateTime? zonedDateTime, string specifier);

    string FormatDateWithTime(DateTime? instant);
    string FormatDateWithTime(Instant? instant);
    string FormatDateWithTime(LocalDateTime? instant);
    string FormatDateWithTime(ZonedDateTime? instant);
    
    DateFormat DateFormat { get; }
    TimeFormat TimeFormat { get; }
    Timezone Timezone { get; }
}
