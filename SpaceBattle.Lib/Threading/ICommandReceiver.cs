namespace SpaceBattle.Lib;

public interface ICommandReceiver
{
    void Receive(ICommand command);
}
