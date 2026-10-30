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
