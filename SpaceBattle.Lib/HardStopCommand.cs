namespace SpaceBattle.Lib;

public class HardStopCommand : ICommand
{
    private readonly IGameThread _thread;

    public HardStopCommand(IGameThread thread)
    {
        _thread = thread ?? throw new ArgumentNullException(nameof(thread));
    }

    public void Execute()
    {
        _thread.StopImmediately();
    }
}
