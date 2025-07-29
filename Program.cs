using System;
using SharpHook;
using SharpHook.Data;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

class Program
{
    private static ViGEmClient client = new();
    private static IXbox360Controller controller;
    private static SimpleGlobalHook hook;

    // Para acumular movimento do mouse e transformar em posição do joystick direito (valores de -32768 a 32767)
    private static int rightThumbX = 0;
    private static int rightThumbY = 0;

    static void Main()
    {
        controller = client.CreateXbox360Controller();
        controller.Connect();

        Console.WriteLine("Controle Xbox virtual criado e conectado.");
        Console.WriteLine("Pressione ESC para sair.");

        hook = new SimpleGlobalHook();

        hook.KeyPressed += OnKeyPressed;
        hook.KeyReleased += OnKeyReleased;

        hook.MouseMoved += OnMouseMoved;
        hook.MousePressed += OnMousePressed;
        hook.MouseReleased += OnMouseReleased;

        hook.Run();

        controller.Disconnect();
        client.Dispose();
    }

    private static void OnKeyPressed(object sender, KeyboardHookEventArgs e)
    {
        switch (e.Data.KeyCode)
        {
            case KeyCode.VcW:
                controller.SetButtonState(Xbox360Button.Up, true);
                break;
            case KeyCode.VcA:
                controller.SetButtonState(Xbox360Button.Left, true);
                break;
            case KeyCode.VcS:
                controller.SetButtonState(Xbox360Button.Down, true);
                break;
            case KeyCode.VcD:
                controller.SetButtonState(Xbox360Button.Right, true);
                break;
            case KeyCode.VcSpace:
                controller.SetButtonState(Xbox360Button.A, true);
                break;
            case KeyCode.VcEscape:
                Console.WriteLine("Saindo...");
                Environment.Exit(0);
                break;
        }
    }

    private static void OnKeyReleased(object sender, KeyboardHookEventArgs e)
    {
        switch (e.Data.KeyCode)
        {
            case KeyCode.VcW:
                controller.SetButtonState(Xbox360Button.Up, false);
                break;
            case KeyCode.VcA:
                controller.SetButtonState(Xbox360Button.Left, false);
                break;
            case KeyCode.VcS:
                controller.SetButtonState(Xbox360Button.Down, false);
                break;
            case KeyCode.VcD:
                controller.SetButtonState(Xbox360Button.Right, false);
                break;
            case KeyCode.VcSpace:
                controller.SetButtonState(Xbox360Button.A, false);
                break;
        }
    }

    private static void OnMouseMoved(object sender, MouseHookEventArgs e)
    {
        // Movimento relativo do mouse (delta X e Y)
        int deltaX = e.Data.X; // Relativo a última posição
        int deltaY = e.Data.Y;

        // Ajuste de sensibilidade (teste e ajuste esse valor)
        int sensitivity = 500;

        // Atualiza a posição do joystick direito acumulando os deltas multiplicados
        rightThumbX += deltaX * sensitivity;
        rightThumbY -= deltaY * sensitivity; // negativo para ajustar o eixo Y (para cima positivo)

        // Limita valores entre -32768 e 32767 (range do joystick)
        rightThumbX = Math.Clamp(rightThumbX, -32768, 32767);
        rightThumbY = Math.Clamp(rightThumbY, -32768, 32767);

        // Atualiza estado do joystick direito no controle virtual
        controller.SetAxisValue(Xbox360Axis.RightThumbX, (short)rightThumbX);
        controller.SetAxisValue(Xbox360Axis.RightThumbY, (short)rightThumbY);
    }

    private static void OnMousePressed(object sender, MouseHookEventArgs e)
    {
        switch (e.Data.Button)
        {
            case MouseButton.Button1: // Clique esquerdo
                controller.SetButtonState(Xbox360Button.A, true);
                break;
            case MouseButton.Button2: // Clique direito
                controller.SetButtonState(Xbox360Button.B, true);
                break;
        }
    }

    private static void OnMouseReleased(object sender, MouseHookEventArgs e)
    {
        switch (e.Data.Button)
        {
            case MouseButton.Button1: // Clique esquerdo
                controller.SetButtonState(Xbox360Button.A, false);
                break;
            case MouseButton.Button2: // Clique direito
                controller.SetButtonState(Xbox360Button.B, false);
                break;
        }
    }
}
