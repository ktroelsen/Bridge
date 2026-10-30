namespace Bridge.Engine;

/// <summary>The four seats in clockwise order, so the next to act is (seat + 1) % 4.</summary>
public enum Seat { North, East, South, West }

public enum Vulnerability { None, NorthSouth, EastWest, Both }

public static class SeatExtensions
{
    public static Seat Next(this Seat seat) => (Seat)(((int)seat + 1) % 4);

    public static Seat Partner(this Seat seat) => (Seat)(((int)seat + 2) % 4);

    public static bool IsNorthSouth(this Seat seat) => seat is Seat.North or Seat.South;
}
