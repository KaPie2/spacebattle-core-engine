using App;
using App.Scopes;
using Moq;

namespace SpaceBattle.Lib.Tests;

public class BurnFuelCommandTests
{
    public BurnFuelCommandTests()
    {
        new InitCommand().Execute();
        var scope = Ioc.Resolve<object>("IoC.Scope.Create");
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", scope).Execute();
    }

    [Fact]
    public void Constructors_NullFuelable_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new BurnFuelCommand(null!));
        Assert.Throws<ArgumentNullException>(() => new CheckFuelCommand(null!));
    }

    [Fact]
    public void BurnFuelCommand_SufficientFuel_DecreasesFuelLevel()
    {
        var fuelableMock = new Mock<IFuelable>();
        fuelableMock.SetupProperty(f => f.FuelLevel, 10);
        fuelableMock.SetupGet(f => f.FuelConsumption).Returns(3);

        var command = new BurnFuelCommand(fuelableMock.Object);
        command.Execute();

        Assert.Equal(7, fuelableMock.Object.FuelLevel);
        fuelableMock.VerifySet(f => f.FuelLevel = 7, Times.Once);
    }

    [Fact]
    public void BurnFuelCommand_ExactFuel_SetsFuelLevelToZero()
    {
        var fuelableMock = new Mock<IFuelable>();
        fuelableMock.SetupProperty(f => f.FuelLevel, 5);
        fuelableMock.SetupGet(f => f.FuelConsumption).Returns(5);

        var command = new BurnFuelCommand(fuelableMock.Object);
        command.Execute();

        Assert.Equal(0, fuelableMock.Object.FuelLevel);
    }

    [Fact]
    public void BurnFuelCommand_InsufficientFuel_ThrowsInvalidOperationExceptionWithoutMutating()
    {
        var fuelableMock = new Mock<IFuelable>();
        fuelableMock.SetupProperty(f => f.FuelLevel, 2);
        fuelableMock.SetupGet(f => f.FuelConsumption).Returns(5);

        var command = new BurnFuelCommand(fuelableMock.Object);

        Assert.Throws<InvalidOperationException>(() => command.Execute());
        Assert.Equal(2, fuelableMock.Object.FuelLevel);
        fuelableMock.VerifySet(f => f.FuelLevel = It.IsAny<int>(), Times.Never);
    }

    [Fact]
    public void BurnFuelCommand_NegativeConsumption_ThrowsInvalidOperationException()
    {
        var fuelableMock = new Mock<IFuelable>();
        fuelableMock.SetupGet(f => f.FuelLevel).Returns(10);
        fuelableMock.SetupGet(f => f.FuelConsumption).Returns(-1);

        var command = new BurnFuelCommand(fuelableMock.Object);

        Assert.Throws<InvalidOperationException>(() => command.Execute());
    }

    [Fact]
    public void CheckFuelCommand_SufficientFuel_DoesNotThrow()
    {
        var fuelableMock = new Mock<IFuelable>();
        fuelableMock.SetupGet(f => f.FuelLevel).Returns(10);
        fuelableMock.SetupGet(f => f.FuelConsumption).Returns(4);

        var command = new CheckFuelCommand(fuelableMock.Object);
        command.Execute();
    }

    [Fact]
    public void CheckFuelCommand_InsufficientFuel_ThrowsInvalidOperationException()
    {
        var fuelableMock = new Mock<IFuelable>();
        fuelableMock.SetupGet(f => f.FuelLevel).Returns(3);
        fuelableMock.SetupGet(f => f.FuelConsumption).Returns(4);

        var command = new CheckFuelCommand(fuelableMock.Object);

        Assert.Throws<InvalidOperationException>(() => command.Execute());
    }

    [Fact]
    public void CheckFuelCommand_NegativeConsumption_ThrowsInvalidOperationException()
    {
        var fuelableMock = new Mock<IFuelable>();
        fuelableMock.SetupGet(f => f.FuelLevel).Returns(10);
        fuelableMock.SetupGet(f => f.FuelConsumption).Returns(-2);

        var command = new CheckFuelCommand(fuelableMock.Object);

        Assert.Throws<InvalidOperationException>(() => command.Execute());
    }

    [Fact]
    public void RegisterIoCDependencyBurnFuelCommand_ResolvesCheckAndBurnCommands()
    {
        var fuelableMock = new Mock<IFuelable>();
        fuelableMock.SetupProperty(f => f.FuelLevel, 10);
        fuelableMock.SetupGet(f => f.FuelConsumption).Returns(2);

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Adapters.IFuelable",
            (object[] args) => fuelableMock.Object
        ).Execute();

        new RegisterIoCDependencyBurnFuelCommand().Execute();

        var objDict = new UObject
        {
            ["FuelLevel"] = 10,
            ["FuelConsumption"] = 2
        };

        var checkCmd = Ioc.Resolve<ICommand>("Commands.CheckFuel", objDict);
        var burnCmd = Ioc.Resolve<ICommand>("Commands.BurnFuel", objDict);

        Assert.IsType<CheckFuelCommand>(checkCmd);
        Assert.IsType<BurnFuelCommand>(burnCmd);

        checkCmd.Execute();
        burnCmd.Execute();

        Assert.Equal(8, fuelableMock.Object.FuelLevel);
    }
}
