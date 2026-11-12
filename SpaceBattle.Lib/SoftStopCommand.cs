using App;

namespace SpaceBattle.Lib;

public class SoftStopCommand : ICommand
{
    private readonly IGameThread _thread;

    public SoftStopCommand(IGameThread thread)
    {
        _thread = thread ?? throw new ArgumentNullException(nameof(thread));
    }

    public void Execute()
    {
        _thread.Queue.CompleteAdding();

        _thread.SetStrategy(() =>
        {
            if (_thread.Queue.TryTake(out var cmd))
            {
                try
                {
                    cmd.Execute();
                }
                catch (Exception ex)
                {
                    Ioc.Resolve<ICommand>("Exception.Handler", cmd, ex).Execute();
                }
            }
            else
            {
                _thread.StopImmediately();
            }
        });
    }
}
