using System;
using System.Collections.Generic;

namespace NetMapper.Tests.Fixtures;

public enum ReservationStatus
{
    Tentative,
    Confirmed,
    Cancelled
}

public sealed class Reservation
{
    public Guid ReservationId { get; set; }

    public string? GuestFirstName { get; set; }

    public string? GuestLastName { get; set; }

    public DateTime CheckInUtc { get; set; }

    public DateTime CheckOutUtc { get; set; }

    public int RoomCount { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Currency { get; set; }

    public ReservationStatus Status { get; set; }

    public IDictionary<string, string> Attributes { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed class PartnerReservation
{
    public string? ConfirmationCode { get; set; }

    public string? GuestName { get; set; }

    public string? ArrivalDate { get; set; }

    public string? DepartureDate { get; set; }

    public int Nights { get; set; }

    public int Units { get; set; }

    public double Amount { get; set; }

    public string? CurrencyCode { get; set; }

    public string? ReservationState { get; set; }

    public IDictionary<string, string> Meta { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
