namespace SpaceBattle.Lib;

public interface IWorkerInbox
{
    /// <summary>Attempts to enqueue a message immediately and returns false when the inbox is full.</summary>
    /// <remarks>Request handlers may call this method concurrently.</remarks>
    bool TryEnqueue(GameCommandMessage message);
}
