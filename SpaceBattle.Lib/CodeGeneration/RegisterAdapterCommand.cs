namespace SpaceBattle.Lib;

public class RegisterAdapterCommand<T> : ICommand
{
    public void Execute()
    {
        AdapterRegistrationBuilder.Build(typeof(T)).Execute();
    }
}
