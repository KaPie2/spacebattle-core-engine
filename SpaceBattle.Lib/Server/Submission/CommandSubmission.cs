using System.Text.Json;
using SpaceBattle.Lib;

namespace SpaceBattle.Lib.Server;

public sealed record CommandRequestDto(string GameId, string Command, JsonElement Arguments, long Generation, string? CommandId = null);
public sealed record CommandAcceptedResponseDto(bool Accepted, string? Reason = null, string? CommandId = null);
public sealed record GameSnapshotDto(string GameId, long Generation, JsonElement State);
public sealed record ServerSnapshotDto(int FormatVersion, long Generation, IReadOnlyList<GameSnapshotDto> Games, IReadOnlyDictionary<string, string> Routes);

public interface ICommandRequestValidator { string? Validate(CommandRequestDto request); }

public sealed class CommandRequestValidator : ICommandRequestValidator
{
    public string? Validate(CommandRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.GameId)) return "GameId is required.";
        if (string.IsNullOrWhiteSpace(request.Command)) return "Command is required.";
        if (request.Generation < 0) return "Generation must be non-negative.";
        if (request.Arguments.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) return "Arguments are required.";
        return null;
    }
}

public interface IGameCommandFactory { ICommand Create(CommandRequestDto request); }
public interface IGameReceiverRegistry
{
    bool TryGet(string gameId, out ICommandReceiver receiver);
}

public sealed class CommandSubmissionService
{
    private readonly ICommandRequestValidator _validator;
    private readonly IGameCommandFactory _factory;
    private readonly IGameReceiverRegistry _registry;

    public CommandSubmissionService(ICommandRequestValidator validator, IGameCommandFactory factory, IGameReceiverRegistry registry)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public CommandAcceptedResponseDto Submit(CommandRequestDto request)
    {
        var error = _validator.Validate(request);
        if (error is not null) return new(false, error, request.CommandId);
        if (!_registry.TryGet(request.GameId, out var receiver)) return new(false, "Unknown game.", request.CommandId);
        try
        {
            receiver.Receive(_factory.Create(request));
            return new(true, CommandId: request.CommandId);
        }
        catch (InvalidOperationException exception)
        {
            return new(false, exception.Message, request.CommandId);
        }
    }
}

public sealed class JsonServerSnapshotSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    public string Serialize(ServerSnapshotDto snapshot) => JsonSerializer.Serialize(snapshot ?? throw new ArgumentNullException(nameof(snapshot)), Options);
    public ServerSnapshotDto Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Snapshot JSON is required.", nameof(json));
        return JsonSerializer.Deserialize<ServerSnapshotDto>(json, Options) ?? throw new ArgumentException("Snapshot JSON is invalid.", nameof(json));
    }
}




