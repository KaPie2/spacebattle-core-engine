using System.Text.Json;

namespace SpaceBattle.Lib.Server;

public interface ICommandRequestValidator
{
    string? Validate(CommandRequestDto request);
}

public sealed class CommandRequestValidator : ICommandRequestValidator
{
    public string? Validate(CommandRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.GameId)) return "GameId is required.";
        if (string.IsNullOrWhiteSpace(request.Command)) return "Command is required.";
        if (request.Generation < 0) return "Generation must be non-negative.";
        if (request.Arguments.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return "Arguments are required.";

        return null;
    }
}

public interface IGameCommandFactory
{
    ICommand Create(CommandRequestDto request);
}

public interface IGameReceiverRegistry
{
    bool TryGet(string gameId, out ICommandReceiver receiver);
}

public sealed class CommandSubmissionService
{
    private readonly ICommandRequestValidator _validator;
    private readonly IGameCommandFactory _commandFactory;
    private readonly IGameReceiverRegistry _receivers;

    public CommandSubmissionService(
        ICommandRequestValidator validator,
        IGameCommandFactory commandFactory,
        IGameReceiverRegistry receivers)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _commandFactory = commandFactory ?? throw new ArgumentNullException(nameof(commandFactory));
        _receivers = receivers ?? throw new ArgumentNullException(nameof(receivers));
    }

    public CommandAcceptedResponseDto Submit(CommandRequestDto request)
    {
        var validationError = _validator.Validate(request);
        if (validationError is not null)
            return new(false, validationError, request.CommandId);

        if (!_receivers.TryGet(request.GameId, out var receiver))
            return new(false, "Unknown game.", request.CommandId);

        try
        {
            receiver.Receive(_commandFactory.Create(request));
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
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public string Serialize(ServerSnapshotDto snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return JsonSerializer.Serialize(snapshot, Options);
    }

    public ServerSnapshotDto Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("Snapshot JSON is required.", nameof(json));

        return JsonSerializer.Deserialize<ServerSnapshotDto>(json, Options)
            ?? throw new ArgumentException("Snapshot JSON is invalid.", nameof(json));
    }
}
