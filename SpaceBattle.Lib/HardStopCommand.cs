namespace SpaceBattle.Lib;

public class HardStopCommand : ICommand
{
    private readonly GameThread _thread;

    public HardStopCommand(GameThread thread)
    {
        _thread = thread ?? throw new ArgumentNullException(nameof(thread));
    }

    public void Execute()
    {
        _thread.StopImmediately();
    }
}
