using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace IPAClipboard;

public partial class App : Application
{
    private Mutex? instance;
    private EventWaitHandle? wake;
    private RegisteredWaitHandle? listener;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Length == 2 && e.Args[0] == "--self-test")
        {
            var window = new MainWindow(new SettingsStore(Path.Combine(e.Args[1], "settings")));
            MainWindow = window; window.Show();
            bool started = false;
            window.ContentRendered += async (_, _) =>
            {
                if (started) return; started = true;
                try { await SelfTests.RunAsync(window, e.Args[1]); Shutdown(0); }
                catch (Exception ex) { Directory.CreateDirectory(e.Args[1]); File.WriteAllText(Path.Combine(e.Args[1], "FAILED.txt"), ex.ToString()); Shutdown(1); }
            };
            return;
        }
        instance = new Mutex(false, @"Local\IPAClipboard.Main.v1", out bool first);
        if (!first)
        {
            AllowSetForegroundWindow(-1);
            for (int i = 0; i < 20; i++)
            {
                try { using var signal = EventWaitHandle.OpenExisting(@"Local\IPAClipboard.Wake.v1"); signal.Set(); break; }
                catch (WaitHandleCannotBeOpenedException) { Thread.Sleep(50); }
            }
            Shutdown(); return;
        }
        wake = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\IPAClipboard.Wake.v1");
        var main = new MainWindow(); MainWindow = main;
        listener = ThreadPool.RegisterWaitForSingleObject(wake, (_, _) => Dispatcher.BeginInvoke(() =>
        {
            if (main.WindowState == WindowState.Minimized) main.WindowState = WindowState.Normal;
            main.Show(); main.Activate(); SetForegroundWindow(new System.Windows.Interop.WindowInteropHelper(main).Handle);
        }), null, Timeout.Infinite, false);
        DispatcherUnhandledException += (_, args) =>
        {
            try { var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IPAClipboard"); Directory.CreateDirectory(dir); File.AppendAllText(Path.Combine(dir, "error.log"), $"{DateTime.Now:u} {args.Exception}\n"); } catch { }
            if (MainWindow is MainWindow m) m.ShowStatus("操作未完成，请重试。", true);
            args.Handled = true;
        };
        main.Show();
    }
    protected override void OnExit(ExitEventArgs e) { listener?.Unregister(null); wake?.Dispose(); instance?.Dispose(); base.OnExit(e); }
    [DllImport("user32.dll")] private static extern bool AllowSetForegroundWindow(int processId);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
}
