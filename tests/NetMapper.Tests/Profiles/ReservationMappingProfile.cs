using System;
using System.Globalization;
using NetMapper.Core;
using NetMapper.Tests.Fixtures;

namespace NetMapper.Tests.Profiles;

public sealed class ReservationMappingProfile : IMappingProfile
{
    public const string DateFormatKey = "reservation:partner:date-format";
    public const string DefaultCurrencyKey = "reservation:partner:default-currency";

    public void Configure(IMappingRegistry registry)
    {
        registry.Register<Reservation, PartnerReservation>(builder =>
        {
            builder.MapForward((source, context) =>
            {
                if (source == null)
                {
                    return null;
                }

                context ??= MappingContext.Empty;
                var dateFormat = ResolveDateFormat(context);
                var partner = new PartnerReservation
                {
                    ConfirmationCode = source.ReservationId.ToString("N", CultureInfo.InvariantCulture),
                    GuestName = BuildGuestName(source.GuestFirstName, source.GuestLastName),
                    ArrivalDate = source.CheckInUtc.ToString(dateFormat, CultureInfo.InvariantCulture),
                    DepartureDate = source.CheckOutUtc.ToString(dateFormat, CultureInfo.InvariantCulture),
                    Nights = CalculateNights(source.CheckInUtc, source.CheckOutUtc),
                    Units = source.RoomCount,
                    Amount = Convert.ToDouble(source.TotalAmount, CultureInfo.InvariantCulture),
                    CurrencyCode = ResolveCurrency(context, source.Currency),
                    ReservationState = source.Status.ToString().ToUpperInvariant()
                };

                foreach (var pair in source.Attributes)
                {
                    partner.Meta[pair.Key] = pair.Value;
                }

                return partner;
            });

            builder.MapReverse((partner, context) =>
            {
                if (partner == null)
                {
                    return null;
                }

                context ??= MappingContext.Empty;
                var dateFormat = ResolveDateFormat(context);
                var reservation = new Reservation
                {
                    ReservationId = ParseReservationId(partner.ConfirmationCode),
                    GuestFirstName = ExtractFirstName(partner.GuestName),
                    GuestLastName = ExtractLastName(partner.GuestName),
                    CheckInUtc = ParseDate(partner.ArrivalDate, dateFormat),
                    CheckOutUtc = ParseDate(partner.DepartureDate, dateFormat),
                    RoomCount = partner.Units > 0 ? partner.Units : Math.Max(1, partner.Nights),
                    TotalAmount = Convert.ToDecimal(partner.Amount, CultureInfo.InvariantCulture),
                    Currency = string.IsNullOrWhiteSpace(partner.CurrencyCode) ? ResolveCurrency(context, null) : partner.CurrencyCode,
                    Status = ParseStatus(partner.ReservationState)
                };

                foreach (var pair in partner.Meta)
                {
                    reservation.Attributes[pair.Key] = pair.Value;
                }

                return reservation;
            });
        });
    }

    private static string ResolveDateFormat(MappingContext context)
    {
        return context.TryGet<string>(DateFormatKey, out var format) && !string.IsNullOrWhiteSpace(format)
            ? format
            : "yyyy-MM-dd";
    }

    private static string? ResolveCurrency(MappingContext context, string? fallback)
    {
        if (!string.IsNullOrWhiteSpace(fallback))
        {
            return fallback;
        }

        return context.TryGet<string>(DefaultCurrencyKey, out var currency) ? currency : null;
    }

    private static int CalculateNights(DateTime checkInUtc, DateTime checkOutUtc)
    {
        var duration = checkOutUtc.Date - checkInUtc.Date;
        return Math.Max(0, (int)Math.Round(duration.TotalDays));
    }

    private static string? BuildGuestName(string? first, string? last)
    {
        if (string.IsNullOrWhiteSpace(first))
        {
            return last;
        }

        if (string.IsNullOrWhiteSpace(last))
        {
            return first;
        }

        return string.Create(first.Length + 1 + last.Length, (first, last), static (span, names) =>
        {
            names.first.AsSpan().CopyTo(span);
            span[names.first.Length] = ' ';
            names.last.AsSpan().CopyTo(span[(names.first.Length + 1)..]);
        });
    }

    private static Guid ParseReservationId(string? value)
    {
        return Guid.TryParse(value, out var parsed) ? parsed : Guid.Empty;
    }

    private static DateTime ParseDate(string? value, string format)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        }

        if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
        {
            return parsed;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsed))
        {
            return parsed;
        }

        return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
    }

    private static ReservationStatus ParseStatus(string? state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            return ReservationStatus.Tentative;
        }

        return Enum.TryParse(state, true, out ReservationStatus result) ? result : ReservationStatus.Tentative;
    }

    private static string? ExtractFirstName(string? guestName)
    {
        if (string.IsNullOrWhiteSpace(guestName))
        {
            return null;
        }

        var parts = guestName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 0 ? parts[0] : null;
    }

    private static string? ExtractLastName(string? guestName)
    {
        if (string.IsNullOrWhiteSpace(guestName))
        {
            return null;
        }

        var parts = guestName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 2 ? parts[1] : null;
    }
}
