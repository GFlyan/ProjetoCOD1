using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using SharpHook;
using SharpHook.Data;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

class Program
{
    private static IXbox360Controller? controller;
    private static Point fixedCursorPosition;
    private static SimpleGlobalHook hook;

    private static double smoothRightX = 0;
    private static double smoothRightY = 0;

    private const int mouseDeadzonePixels = 3;
    private const double sensitivityMultiplier = 10000.0;
    private const double smoothingFactor = 0.3;
    private const int updateDelayMs = 2;
    private const short moveSpeed = 32767;

    private static Point? enemyPosition = new Point(
        Screen.PrimaryScreen.Bounds.Width / 2 + 100,
        Screen.PrimaryScreen.Bounds.Height / 2 - 50
    );
    private const int aimAssistRadius = 100;
    private const double aimAssistStrength = 0.5;

    private static DateTime lastActionTime = DateTime.MinValue;
    private static readonly TimeSpan debounceDuration = TimeSpan.FromMilliseconds(100);

    [DllImport("user32.dll")] static extern bool GetCursorPos(out Point lpPoint);
    [DllImport("user32.dll")] static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(Keys vKey);

    static void Main()
    {
        var client = new ViGEmClient();
        controller = client.CreateXbox360Controller();
        controller.Connect();

        fixedCursorPosition = new Point(
            Screen.PrimaryScreen.Bounds.Width / 2,
            Screen.PrimaryScreen.Bounds.Height / 2
        );

        SetCursorPos(fixedCursorPosition.X, fixedCursorPosition.Y);
        Cursor.Hide();

        Console.WriteLine("Controle virtual iniciado.");
        Console.WriteLine("Mouse = analógico direito + aim assist");
        Console.WriteLine("Botão esquerdo = atirar | Botão direito = mirar | INSERT = sair");

        hook = new SimpleGlobalHook();
        hook.MousePressed += OnMousePressed;
        hook.MouseReleased += OnMouseReleased;
        hook.RunAsync();

        while (true)
        {
            if ((GetAsyncKeyState(Keys.Insert) & 0x8000) != 0)
                break;

            AtualizarAnalogicoDireitoComMouse();
            AtualizarAnalogicoEsquerdoComTeclado();
            AtualizarAcoesComTeclado();

            controller!.SubmitReport();
            Thread.Sleep(updateDelayMs);
        }

        controller.Disconnect();
        client.Dispose();
        Cursor.Show();
    }

    private static void AtualizarAnalogicoDireitoComMouse()
    {
        GetCursorPos(out Point currentPos);

        int deltaX = currentPos.X - fixedCursorPosition.X;
        int deltaY = currentPos.Y - fixedCursorPosition.Y;

        if (Math.Abs(deltaX) < mouseDeadzonePixels) deltaX = 0;
        if (Math.Abs(deltaY) < mouseDeadzonePixels) deltaY = 0;

        double targetX = deltaX * sensitivityMultiplier;
        double targetY = -deltaY * sensitivityMultiplier;

        if (enemyPosition != null && DentroDaAreaAssistida(currentPos, enemyPosition.Value))
        {
            int assistX = enemyPosition.Value.X - currentPos.X;
            int assistY = enemyPosition.Value.Y - currentPos.Y;

            targetX += assistX * aimAssistStrength;
            targetY -= assistY * aimAssistStrength;
        }

        smoothRightX = Lerp(smoothRightX, targetX, smoothingFactor);
        smoothRightY = Lerp(smoothRightY, targetY, smoothingFactor);

        short stickX = (short)Math.Clamp(smoothRightX, -32767, 32767);
        short stickY = (short)Math.Clamp(smoothRightY, -32767, 32767);

        controller!.SetAxisValue(Xbox360Axis.RightThumbX, stickX);
        controller.SetAxisValue(Xbox360Axis.RightThumbY, stickY);

        SetCursorPos(fixedCursorPosition.X, fixedCursorPosition.Y);
    }

    private static void AtualizarAnalogicoEsquerdoComTeclado()
    {
        short x = 0, y = 0;

        if (TeclaPressionada(Keys.W)) y = moveSpeed;
        else if (TeclaPressionada(Keys.S)) y = (short)-moveSpeed;
        if (TeclaPressionada(Keys.A)) x = (short)-moveSpeed;
        else if (TeclaPressionada(Keys.D)) x = moveSpeed;

        controller!.SetAxisValue(Xbox360Axis.LeftThumbX, x);
        controller.SetAxisValue(Xbox360Axis.LeftThumbY, y);
    }

    private static void AtualizarAcoesComTeclado()
    {
        controller!.SetButtonState(Xbox360Button.Y, TeclaPressionada(Keys.Space));
        controller.SetButtonState(Xbox360Button.X, TeclaPressionada(Keys.ControlKey));
    }

    private static bool TeclaPressionada(Keys key) =>
        (GetAsyncKeyState(key) & 0x8000) != 0;

    private static bool DentroDaAreaAssistida(Point cursor, Point target)
    {
        int dx = cursor.X - target.X;
        int dy = cursor.Y - target.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);
        return distance < aimAssistRadius;
    }

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;

    private static bool PodeExecutarAcao()
    {
        if (DateTime.Now - lastActionTime > debounceDuration)
        {
            lastActionTime = DateTime.Now;
            return true;
        }
        return false;
    }

    private static void OnMousePressed(object? sender, MouseHookEventArgs e)
    {
        if (controller == null || !PodeExecutarAcao()) return;

        switch (e.Data.Button)
        {
            case MouseButton.Button1: // Clique esquerdo → RT (atirar)
                controller.SetSliderValue(Xbox360Slider.RightTrigger, 255);
                break;
            case MouseButton.Button2: // Clique direito → LT (mirar)
                controller.SetSliderValue(Xbox360Slider.LeftTrigger, 255);
                break;
        }
    }

    private static void OnMouseReleased(object? sender, MouseHookEventArgs e)
    {
        if (controller == null) return;

        switch (e.Data.Button)
        {
            case MouseButton.Button1:
                controller.SetSliderValue(Xbox360Slider.RightTrigger, 0);
                break;
            case MouseButton.Button2:
                controller.SetSliderValue(Xbox360Slider.LeftTrigger, 0);
                break;
        }
    }
}
