using System.Text.Json;

namespace SpaceBattle.Lib.Server;

public sealed record CommandRequestDto(
    string GameId,
    string Command,
    JsonElement Arguments,
    long Generation,
    string? CommandId = null);

public sealed record CommandAcceptedResponseDto(
    bool Accepted,
    string? Reason = null,
    string? CommandId = null);

public sealed record GameSnapshotDto(
    string GameId,
    long Generation,
    JsonElement State);

public sealed record ServerSnapshotDto(
    int FormatVersion,
    long Generation,
    IReadOnlyList<GameSnapshotDto> Games,
    IReadOnlyDictionary<string, string> Routes);
