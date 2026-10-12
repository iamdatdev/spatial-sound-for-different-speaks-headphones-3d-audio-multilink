using NAudio.CoreAudioApi;
using NAudio.Wave;
using DoubleHeadphones3D.Audio;

Console.Title = "Double Headphones 3D - Starter";
Console.WriteLine("Double Headphones 3D (starter)");
Console.WriteLine("Mirrors Windows system audio to two output devices.");
Console.WriteLine();

using var enumerator = new MMDeviceEnumerator();
var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();

if (devices.Count < 2)
{
    Console.WriteLine("Windows reports fewer than two active playback devices.");
    Console.WriteLine("Pair/connect both Bluetooth headphones in Settings > Bluetooth & devices, then retry.");
    return;
}

Console.WriteLine("Active playback devices:");
for (var i = 0; i < devices.Count; i++)
{
    var isDefault = devices[i].ID == enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID;
    Console.WriteLine($"  [{i}] {devices[i].FriendlyName}{(isDefault ? "  (Windows default)" : "")}");
}

Console.WriteLine();
Console.WriteLine("Important: keep Windows' default output DIFFERENT from both selected outputs.");
Console.WriteLine("The app captures audio from the Windows default output using loopback.");
Console.WriteLine("If you select that same endpoint as an output, feedback/echo can occur.");
Console.WriteLine("For testing, leave Windows output on laptop speakers and select both Bluetooth devices.");
Console.WriteLine();

var first = ReadIndex("Choose first headphone index: ", devices.Count);
var second = ReadIndex("Choose second headphone index: ", devices.Count);
if (first == second)
{
    Console.WriteLine("Choose two different devices. Exiting.");
    return;
}

var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
if (devices[first].ID == defaultDevice.ID || devices[second].ID == defaultDevice.ID)
{
    Console.WriteLine("WARNING: One selected output is also the loopback capture source.");
    Console.WriteLine("This can cause feedback. Change Windows default output or select different outputs.");
    Console.WriteLine("Press Enter to continue anyway, or Ctrl+C to cancel.");
    Console.ReadLine();
}

using var router = new DualOutputRouter(devices[first], devices[second]);
router.Start();

Console.WriteLine();
Console.WriteLine("Running. Play audio in any Windows app.");
Console.WriteLine("Press [S] to toggle the spatial-width effect; [Q] to quit.");
Console.WriteLine("Note: this starter's DSP is stereo widening/crossfeed, not a full measured-HRTF renderer.");
Console.WriteLine();

while (true)
{
    var key = Console.ReadKey(intercept: true).Key;
    if (key == ConsoleKey.Q)
        break;

    if (key == ConsoleKey.S)
    {
        router.SpatialEnabled = !router.SpatialEnabled;
        Console.WriteLine($"Spatial-width effect: {(router.SpatialEnabled ? "ON" : "OFF")}");
    }
}

router.Stop();

static int ReadIndex(string prompt, int count)
{
    while (true)
    {
        Console.Write(prompt);
        var input = Console.ReadLine();
        if (int.TryParse(input, out var index) && index >= 0 && index < count)
            return index;
        Console.WriteLine($"Enter a number from 0 to {count - 1}.");
    }
}
