namespace Bridge.Api.Players;

public sealed record PlayerRequest(string Name);
public sealed record PlayerDto(string Name);

public sealed record LessonResultRequest(int Score);
public sealed record LessonProgressDto(string LessonId, int BestScore, DateTime CompletedAt);

public sealed record GameRecordDto(
    int Id, string Mode, int Number, string Level, string Contract, string Declarer, bool OurContract,
    int DeclarerTricks, bool Made, int ScoreNorthSouth, DateTime FinishedAt);

/// <summary>How a group of games went: as declarer (contracts made) and as defenders (contracts beaten).</summary>
public sealed record StatsGroupDto(string Level, int Games, int Declared, int Made, int Defended, int Beaten, int TotalScore);

public sealed record StatsDto(int Games, int PassedOut, IReadOnlyList<StatsGroupDto> ByLevel, IReadOnlyList<GameRecordDto> Recent);
