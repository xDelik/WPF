using System;
using System.Collections.Generic;
using WPF.Models;

namespace WPF.Services;

public static class HotelRules
{
    public static bool HasOverlap(
        IReadOnlyCollection<Reservation> existing,
        Room room,
        DateTimeOffset checkIn,
        DateTimeOffset checkOut,
        int? excludingId = null)
    {
        foreach (var r in existing)
        {
            if (r.Status == ReservationStatus.Cancelled) continue;
            if (excludingId.HasValue && r.Id == excludingId.Value) continue;
            if (r.Room.Id != room.Id) continue;

            if (checkIn < r.CheckOutDate && r.CheckInDate < checkOut)
                return true;
        }
        return false;
    }

    public static bool IsGuestInUse(IReadOnlyCollection<Reservation> reservations, Guest guest)
    {
        foreach (var r in reservations)
        {
            if (r.Status == ReservationStatus.Cancelled) continue;
            if (r.Guest.Id == guest.Id) return true;
        }
        return false;
    }

    public static bool IsRoomInUse(IReadOnlyCollection<Reservation> reservations, Room room)
    {
        foreach (var r in reservations)
        {
            if (r.Status == ReservationStatus.Cancelled) continue;
            if (r.Room.Id == room.Id) return true;
        }
        return false;
    }

    public static bool RefreshStatuses(
        IReadOnlyCollection<Room> rooms,
        IReadOnlyCollection<Reservation> reservations,
        DateTime today)
    {
        var changed = false;
        var justCheckedOut = new HashSet<int>();

        foreach (var r in reservations)
        {
            if (r.Status != ReservationStatus.Confirmed) continue;
            if (r.CheckOutDate.Date > today) continue;

            r.Status = ReservationStatus.Completed;
            justCheckedOut.Add(r.Room.Id);
            changed = true;
        }

        foreach (var room in rooms)
        {
            var hasCurrentGuest = false;
            foreach (var r in reservations)
            {
                if (r.Status != ReservationStatus.Confirmed) continue;
                if (r.Room.Id != room.Id) continue;
                if (r.CheckInDate.Date <= today && today < r.CheckOutDate.Date)
                {
                    hasCurrentGuest = true;
                    break;
                }
            }

            var target = room.Status;
            if (hasCurrentGuest)
                target = RoomStatus.Occupied;
            else if (room.Status == RoomStatus.Occupied || justCheckedOut.Contains(room.Id))
                target = RoomStatus.NeedsCleaning;

            if (room.Status != target)
            {
                room.Status = target;
                changed = true;
            }
        }

        return changed;
    }

    public static bool IsRoomNumberUnique(
        IReadOnlyCollection<Room> rooms,
        string number,
        int? excludingId = null)
    {
        foreach (var r in rooms)
        {
            if (excludingId.HasValue && r.Id == excludingId.Value) continue;
            if (string.Equals(r.Number, number, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }
}
