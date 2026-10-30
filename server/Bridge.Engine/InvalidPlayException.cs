namespace Bridge.Engine;

/// <summary>A card or choice that breaks the rules. The message is shown to the player.</summary>
public sealed class InvalidPlayException(string message) : Exception(message);
