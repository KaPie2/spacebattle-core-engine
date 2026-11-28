namespace SpaceBattle.Lib;

public class SoftStopCommand : ICommand
{
    private readonly GameThread _thread;

    public SoftStopCommand(GameThread thread)
    {
        _thread = thread ?? throw new ArgumentNullException(nameof(thread));
    }

    public void Execute()
    {
        // Подменяем цикл потока на опустошение очереди
        _thread.SetStrategy(() =>
        {
            if (_thread.Queue.Count > 0)
            {
                if (_thread.Queue.TryTake(out var cmd))
                {
                    cmd.Execute();
                }
            }
            else
            {
                _thread.StopImmediately();
            }
        });
    }
}
