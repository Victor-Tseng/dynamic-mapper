using System;
using System.Collections.Generic;
using NetMapper.Core;
using NetMapper.Tests.Fixtures;
using NetMapper.Tests.Profiles;

namespace NetMapper.Tests;

public class ReservationMappingTests
{
    [Fact]
    public void Maps_Internal_To_Partner_With_Contextual_Settings()
    {
        var configuration = new MapperConfiguration().AddProfile(new ReservationMappingProfile());
        var mapper = configuration.BuildMapper();

        var reservation = new Reservation
        {
            ReservationId = Guid.NewGuid(),
            GuestFirstName = "Alex",
            GuestLastName = "Morgan",
            CheckInUtc = new DateTime(2025, 10, 24, 15, 0, 0, DateTimeKind.Utc),
            CheckOutUtc = new DateTime(2025, 10, 27, 11, 0, 0, DateTimeKind.Utc),
            RoomCount = 2,
            TotalAmount = 745.50m,
            Currency = "USD",
            Status = ReservationStatus.Confirmed,
            Attributes =
            {
                ["channel"] = "partner-x",
                ["vip"] = "true"
            }
        };

        var context = new MappingContext();
        context.Set(ReservationMappingProfile.DateFormatKey, "yyyy/MM/dd");
        context.Set(ReservationMappingProfile.DefaultCurrencyKey, "EUR");

        var partner = mapper.Map<Reservation, PartnerReservation>(reservation, context);

        Assert.NotNull(partner);
        Assert.Equal(reservation.ReservationId.ToString("N"), partner!.ConfirmationCode);
        Assert.Equal("Alex Morgan", partner.GuestName);
        Assert.Equal("2025/10/24", partner.ArrivalDate);
        Assert.Equal("2025/10/27", partner.DepartureDate);
        Assert.Equal(3, partner.Nights);
        Assert.Equal(2, partner.Units);
        Assert.Equal(745.50d, partner.Amount);
        Assert.Equal("USD", partner.CurrencyCode);
        Assert.Equal("CONFIRMED", partner.ReservationState);
        Assert.Equal("partner-x", partner.Meta["channel"]);
        Assert.Equal("true", partner.Meta["vip"]);
    }

    [Fact]
    public void Maps_Partner_To_Internal_With_Default_Context()
    {
        var configuration = new MapperConfiguration().AddProfile(new ReservationMappingProfile());
        var mapper = configuration.BuildMapper();

        var partner = new PartnerReservation
        {
            ConfirmationCode = Guid.NewGuid().ToString("N"),
            GuestName = "Jamie Rivera",
            ArrivalDate = "2025-12-01",
            DepartureDate = "2025-12-05",
            Nights = 4,
            Units = 0,
            Amount = 1234.56d,
            CurrencyCode = null,
            ReservationState = "cancelled",
            Meta =
            {
                ["source"] = "partner-y"
            }
        };

        var context = new MappingContext();
        context.Set(ReservationMappingProfile.DefaultCurrencyKey, "GBP");

        var reservation = mapper.Map<PartnerReservation, Reservation>(partner, context);

        Assert.NotNull(reservation);
        Assert.Equal(partner.ConfirmationCode, reservation!.ReservationId.ToString("N"));
        Assert.Equal("Jamie", reservation.GuestFirstName);
        Assert.Equal("Rivera", reservation.GuestLastName);
        Assert.Equal(new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc), reservation.CheckInUtc);
        Assert.Equal(new DateTime(2025, 12, 5, 0, 0, 0, DateTimeKind.Utc), reservation.CheckOutUtc);
        Assert.Equal(4, reservation.RoomCount);
        Assert.Equal(1234.56m, reservation.TotalAmount);
        Assert.Equal("GBP", reservation.Currency);
        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
        Assert.Equal("partner-y", reservation.Attributes["source"]);
    }

    [Fact]
    public void Throws_When_Mapping_Not_Registered()
    {
        var mapper = new DynamicMapper();

        Assert.Throws<InvalidOperationException>(() => mapper.Map<string, int>("123"));
    }
}
