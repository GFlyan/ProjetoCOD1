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

    // Mouse para analógico direito
    private static double smoothRightX = 0;
    private static double smoothRightY = 0;

    // Configurações ajustáveis
    private const int mouseDeadzonePixels = 3;
    private const double sensitivityMultiplier = 300.0;
    private const double smoothingFactor = 0.3;
    private const int updateDelayMs = 2;
    private const short moveSpeed = 32767;

    [DllImport("user32.dll")]
    static extern bool GetCursorPos(out Point lpPoint);

    [DllImport("user32.dll")]
    static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(Keys vKey);

    static void Main()
    {
        var client = new ViGEmClient();
        controller = client.CreateXbox360Controller();
        controller.Connect();

        // Inicializa posição do cursor no centro da tela
        fixedCursorPosition = new Point(
            Screen.PrimaryScreen.Bounds.Width / 2,
            Screen.PrimaryScreen.Bounds.Height / 2
        );
        SetCursorPos(fixedCursorPosition.X, fixedCursorPosition.Y);

        Console.WriteLine("Controle virtual iniciado.");
        Console.WriteLine("Mouse = analógico direito | WASD = andar | INSERT = sair");

        // Configura hook para cliques do mouse
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

            controller!.SubmitReport();
            Thread.Sleep(updateDelayMs);
        }

        controller.Disconnect();
        client.Dispose();
    }

    static Point previousMousePos = Point.Empty;

    private static void AtualizarAnalogicoDireitoComMouse()
    {
        GetCursorPos(out Point currentPos);

        if (previousMousePos == Point.Empty)
            previousMousePos = currentPos;

        int deltaX = currentPos.X - previousMousePos.X;
        int deltaY = currentPos.Y - previousMousePos.Y;

        if (Math.Abs(deltaX) < mouseDeadzonePixels) deltaX = 0;
        if (Math.Abs(deltaY) < mouseDeadzonePixels) deltaY = 0;

        double targetX = deltaX * sensitivityMultiplier;
        double targetY = -deltaY * sensitivityMultiplier;

        smoothRightX = Lerp(smoothRightX, targetX, smoothingFactor);
        smoothRightY = Lerp(smoothRightY, targetY, smoothingFactor);

        short stickX = (short)Math.Clamp(smoothRightX, -32767, 32767);
        short stickY = (short)Math.Clamp(smoothRightY, -32767, 32767);

        controller!.SetAxisValue(Xbox360Axis.RightThumbX, stickX);
        controller.SetAxisValue(Xbox360Axis.RightThumbY, stickY);

        previousMousePos = currentPos;
    }

    private static void AtualizarAnalogicoEsquerdoComTeclado()
    {
        short x = 0;
        short y = 0;

        if (TeclaPressionada(Keys.W))
            y = moveSpeed;
        else if (TeclaPressionada(Keys.S))
            y = (short)-moveSpeed;

        if (TeclaPressionada(Keys.A))
            x = (short)-moveSpeed;
        else if (TeclaPressionada(Keys.D))
            x = moveSpeed;

        controller!.SetAxisValue(Xbox360Axis.LeftThumbX, x);
        controller.SetAxisValue(Xbox360Axis.LeftThumbY, y);
    }

    private static bool TeclaPressionada(Keys key)
    {
        return (GetAsyncKeyState(key) & 0x8000) != 0;
    }

    private static void OnMousePressed(object? sender, MouseHookEventArgs e)
    {
        if (controller == null) return;

        switch (e.Data.Button)
        {
            case MouseButton.Button1: // Botão esquerdo
                controller.SetButtonState(Xbox360Button.A, true);
                break;
            case MouseButton.Button2: // Botão direito
                controller.SetButtonState(Xbox360Button.B, true);
                break;
        }
    }

    private static void OnMouseReleased(object? sender, MouseHookEventArgs e)
    {
        if (controller == null) return;

        switch (e.Data.Button)
        {
            case MouseButton.Button1:
                controller.SetButtonState(Xbox360Button.A, false);
                break;
            case MouseButton.Button2:
                controller.SetButtonState(Xbox360Button.B, false);
                break;
        }
    }

    private static double Lerp(double start, double end, double amount)
    {
        return start + (end - start) * amount;
    }
}

