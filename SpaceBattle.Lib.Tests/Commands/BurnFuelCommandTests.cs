using App;
using Moq;
using Xunit;

namespace SpaceBattle.Lib.Tests;

public class BurnFuelCommandTests
{
    [Fact]
    public void BurnFuelCommand_DecrementsFuel_WhenEnoughFuel()
    {
        // T3.1: BurnFuelCommand_DecrementsFuel_WhenEnoughFuel (проверка корректного расхода)
        var fuelableMock = new Mock<IFuelable>();
        fuelableMock.SetupProperty(f => f.FuelLevel, 100);
        fuelableMock.SetupGet(f => f.FuelConsumption).Returns(30);

        var command = new BurnFuelCommand(fuelableMock.Object);
        command.Execute();

        Assert.Equal(70, fuelableMock.Object.FuelLevel);
    }

    [Fact]
    public void BurnFuelCommand_ThrowsCommandException_WhenFuelInsufficient()
    {
        // T3.2: BurnFuelCommand_ThrowsCommandException_WhenFuelInsufficient (проверка ошибки нехватки)
        var fuelableMock = new Mock<IFuelable>();
        fuelableMock.SetupProperty(f => f.FuelLevel, 20);
        fuelableMock.SetupGet(f => f.FuelConsumption).Returns(50);

        var command = new BurnFuelCommand(fuelableMock.Object);

        var exception = Assert.Throws<CommandException>(() => command.Execute());
        Assert.Equal("Недостаточно топлива для выполнения маневра.", exception.Message);
        Assert.Equal(20, fuelableMock.Object.FuelLevel);
    }

    [Fact]
    public void BurnFuelCommand_DecrementsFuelToZero_WhenFuelLevelEqualsConsumption()
    {
        var fuelableMock = new Mock<IFuelable>();
        fuelableMock.SetupProperty(f => f.FuelLevel, 45);
        fuelableMock.SetupGet(f => f.FuelConsumption).Returns(45);

        var command = new BurnFuelCommand(fuelableMock.Object);
        command.Execute();

        Assert.Equal(0, fuelableMock.Object.FuelLevel);
    }

    [Fact]
    public void BurnFuelCommand_ThrowsArgumentNullException_WhenFuelableIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new BurnFuelCommand(null!));
    }

    [Fact]
    public void CommandException_Constructors_WorkCorrectly()
    {
        var ex1 = new CommandException();
        Assert.NotNull(ex1);

        var ex2 = new CommandException("Test message");
        Assert.Equal("Test message", ex2.Message);

        var inner = new InvalidOperationException("Inner");
        var ex3 = new CommandException("Outer message", inner);
        Assert.Equal("Outer message", ex3.Message);
        Assert.Same(inner, ex3.InnerException);
    }

    [Fact]
    public void RegisterIoCDependencyBurnFuelCommand_RegistersSuccessfully()
    {
        new App.Scopes.InitCommand().Execute();
        var scope = Ioc.Resolve<object>("IoC.Scope.Create");
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", scope).Execute();

        new RegisterIoCDependencyBurnFuelCommand().Execute();

        var fuelableMock = new Mock<IFuelable>();
        fuelableMock.SetupProperty(f => f.FuelLevel, 50);
        fuelableMock.SetupGet(f => f.FuelConsumption).Returns(10);

        var commandFromFuelable = Ioc.Resolve<ICommand>("Commands.BurnFuel", fuelableMock.Object);
        commandFromFuelable.Execute();
        Assert.Equal(40, fuelableMock.Object.FuelLevel);

        var dict = new Dictionary<string, object>
        {
            ["FuelLevel"] = 100,
            ["FuelConsumption"] = 25
        };

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Adapters.IFuelable",
            (object[] args) => fuelableMock.Object
        ).Execute();

        var commandFromDict = Ioc.Resolve<ICommand>("Commands.BurnFuel", dict);
        Assert.NotNull(commandFromDict);
    }
}
