namespace Bridge.Api.Data;

/// <summary>A player is just a name: no login, the name is the key (compared case-insensitively).</summary>
public class Player
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string NormalizedName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

/// <summary>How far a player got with one lesson. The lessons themselves live in the frontend.</summary>
public class LessonProgress
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;
    public string LessonId { get; set; } = "";
    /// <summary>Best share of right answers at the first try, 0-100.</summary>
    public int BestScore { get; set; }
    public DateTime CompletedAt { get; set; }
}

/// <summary>A finished game. The result is worked out by the server from the request, never taken from the client.</summary>
public class GameRecord
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;
    /// <summary>"mini" (mini-bridge) or "full" (bidding and play).</summary>
    public string Mode { get; set; } = "";
    public int Number { get; set; }
    public string Level { get; set; } = "";
    /// <summary>The game request as JSON, so the game can be rebuilt.</summary>
    public string RequestJson { get; set; } = "";
    /// <summary>Contract like "4S" (empty when passed out).</summary>
    public string Contract { get; set; } = "";
    public string Declarer { get; set; } = "";
    /// <summary>Whether North-South (the player's side) declared.</summary>
    public bool OurContract { get; set; }
    public int DeclarerTricks { get; set; }
    public bool Made { get; set; }
    public int ScoreNorthSouth { get; set; }
    public DateTime FinishedAt { get; set; }
}
