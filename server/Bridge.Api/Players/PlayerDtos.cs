namespace Bridge.Api.Players;

public sealed record PlayerRequest(string Name);
public sealed record PlayerDto(string Name);

public sealed record LessonResultRequest(int Score);
public sealed record LessonProgressDto(string LessonId, int BestScore, DateTime CompletedAt);
