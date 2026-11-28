namespace SpaceBattle.Lib;

public sealed record GameCommandMessage(string GameId, ICommand Command);
