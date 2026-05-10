using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using WPF.Models;

namespace WPF.Persistence;

public sealed record HotelData(List<Room> Rooms, List<Guest> Guests, List<Reservation> Reservations);

public class HotelStore
{
    private readonly string _path;
    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public HotelStore()
    {
        var dir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HotelManagement");
        Directory.CreateDirectory(dir);
        _path = System.IO.Path.Combine(dir, "data.json");
    }

    public string Path => _path;

    public HotelData? Load()
    {
        if (!File.Exists(_path)) return null;

        try
        {
            var doc = JsonNode.Parse(File.ReadAllText(_path));
            if (doc is null) return null;

            var rooms = doc["Rooms"]!.Deserialize<List<Room>>(_options)!;
            var guests = doc["Guests"]!.Deserialize<List<Guest>>(_options)!;
            var roomsById = rooms.ToDictionary(r => r.Id);
            var guestsById = guests.ToDictionary(g => g.Id);

            var reservations = doc["Reservations"]!.AsArray().Select(n => new Reservation
            {
                Id = (int)n!["Id"]!,
                Room = roomsById[(int)n["RoomId"]!],
                Guest = guestsById[(int)n["GuestId"]!],
                CheckInDate = n["CheckInDate"]!.GetValue<DateTimeOffset>(),
                CheckOutDate = n["CheckOutDate"]!.GetValue<DateTimeOffset>(),
                Status = Enum.Parse<ReservationStatus>((string)n["Status"]!),
                Notes = (string?)n["Notes"] ?? string.Empty
            }).ToList();

            return new HotelData(rooms, guests, reservations);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Persistence] Failed to load: {ex.Message}. Backing up and reseeding.");
            BackupCorruptFile();
            return null;
        }
    }

    private void BackupCorruptFile()
    {
        if (!File.Exists(_path)) return;
        var backup = _path + $".corrupt-{DateTime.Now:yyyyMMdd-HHmmss}.bak";
        File.Move(_path, backup);
    }

    public void Save(IReadOnlyCollection<Room> rooms, IReadOnlyCollection<Guest> guests, IReadOnlyCollection<Reservation> reservations)
    {
        var data = new
        {
            Rooms = rooms,
            Guests = guests,
            Reservations = reservations.Select(r => new
            {
                r.Id,
                RoomId = r.Room.Id,
                GuestId = r.Guest.Id,
                r.CheckInDate,
                r.CheckOutDate,
                r.Status,
                r.Notes
            })
        };
        File.WriteAllText(_path, JsonSerializer.Serialize(data, _options));
    }
}
