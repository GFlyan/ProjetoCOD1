using System;
using System.Threading;
using SharpHook;
using SharpHook.Native;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

class Program
{
    private static IXbox360Controller controller = new ViGEmClient().CreateXbox360Controller();

    private static float scaleX = 5000f;
    private static float scaleY = 400f;
    private static float smoothing = 9f;
    private static float noiseFilter = 14f;

    private static float deltaX = 0;
    private static float deltaY = 0;

    static void Main()
    {
        controller.Connect();
        Console.WriteLine("Aim Assist ativo com base na config reWASD.");

        var hook = new SimpleGlobalHook();
        hook.MouseMoved += OnMouseMoved;
        hook.RunAsync();

        while (true)
        {
            short rightX = ApplyCurveAndScale(deltaX, scaleX);
            short rightY = ApplyCurveAndScale(-deltaY, scaleY);

            controller.SetAxisValue(Xbox360Axis.RightThumbX, rightX);
            controller.SetAxisValue(Xbox360Axis.RightThumbY, rightY);
            controller.SubmitReport();

            deltaX *= (1f - (1f / smoothing));
            deltaY *= (1f - (1f / smoothing));

            Thread.Sleep(5);
        }
    }

    private static void OnMouseMoved(object? sender, MouseHookEventArgs e)
    {
        float dx = e.Data.X;
        float dy = e.Data.Y;

        if (Math.Abs(dx) > noiseFilter) deltaX += dx;
        if (Math.Abs(dy) > noiseFilter) deltaY += dy;
    }

    private static short ApplyCurveAndScale(float value, float scale)
    {
        float curved = CustomCurve(value);
        float scaled = curved * scale / 10000f;
        return (short)Math.Clamp(scaled, -32767, 32767);
    }

    private static float CustomCurve(float input)
    {
        float abs = Math.Abs(input);
        float sign = Math.Sign(input);

        if (abs < 7000) return 0;
        else if (abs < 18000) return sign * 1337f;
        else if (abs < 25000) return sign * 6000f;
        else if (abs < 28888) return sign * 12337f;
        else return sign * 19000f;
    }
}
