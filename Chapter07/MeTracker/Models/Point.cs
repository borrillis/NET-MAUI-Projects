
namespace MeTracker.Models;

public class Point
{
    public Microsoft.Maui.Devices.Sensors.Location Location { get; set; } = new ();
    public int Count { get; set; } = 1;
    public Color Heat { get; set; } = Colors.Transparent;
}
