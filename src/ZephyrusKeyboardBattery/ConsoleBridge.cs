using System.Runtime.InteropServices;
using System.Text;

namespace ZephyrusKeyboardBattery;

internal static class ConsoleBridge
{
    private const int AttachParentProcess = -1;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    private const int StdOutputHandle = -11;
    private const int StdErrorHandle = -12;

    public static void AttachParentConsole()
    {
        AttachConsole(AttachParentProcess);

        try
        {
            var stdout = Console.OpenStandardOutput();
            var stderr = Console.OpenStandardError();
            Console.SetOut(new StreamWriter(stdout, Encoding.UTF8) { AutoFlush = true });
            Console.SetError(new StreamWriter(stderr, Encoding.UTF8) { AutoFlush = true });
        }
        catch
        {
            // Best effort only. The tray app path does not need a console.
        }
    }
}
