using System.Windows.Input;

namespace SpaceBattle.Lib;

public class RotateCommand : ICommand
{
    private readonly IRotatingObject rotatingObject;
    public RotateCommand(IRotatingObject rotatingObject)
    {
        this.rotatingObject = rotatingObject;
    }

    public void Execute()
    {
        var angle = rotatingObject.Angle ?? throw new ArgumentNullException(nameof(rotatingObject.Angle));
        var angle_v = rotatingObject.AngleVelocity ?? throw new ArgumentNullException(nameof(rotatingObject.AngleVelocity));

        var newAng = angle + angle_v;
        rotatingObject.Angle = newAng ?? throw new InvalidOperationException(nameof(newAng));
    }
}