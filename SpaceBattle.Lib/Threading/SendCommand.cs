namespace SpaceBattle.Lib;

public class SendCommand : ICommand
{
    private readonly ICommand _command;
    private readonly ICommandReceiver _receiver;

    public SendCommand(ICommand command, ICommandReceiver receiver)
    {
        _command = command;
        _receiver = receiver;
    }

    public void Execute()
    {
        _receiver.Receive(_command);
    }
}
