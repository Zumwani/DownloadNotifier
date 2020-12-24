//Taken from separate project (personal), put it here so that this project can be built from source.

using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Threading;

namespace Common.Wpf
{

    public class SetupInfo
    { }

    [ContentProperty(nameof(ContextMenu))]
    public class NotifyIcon : TaskbarIcon
    { }

    public class AppExtension : Binding
    {

        public AppExtension()
        {
            Initialize();
        }

        public AppExtension(string path) : base(path)
        {
            Initialize();
        }

        private void Initialize()
        {
            Source = App.Current;
        }

    }

    public class App : Application
    {

        public App() : base()
        {

            RegisterPackUriScheme();
            Current = this;

        }

        public new static App Current { get; private set; }

        public delegate void ProcessArgumentsHandler(string[] args, bool isSecondInstance);
        public event ProcessArgumentsHandler ProcessArguments;

        private static Mutex mutex;
        private static readonly Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        public static bool IsFirstInstance { get; private set; }

        public string PackageName { get; set; }
        public NotifyIcon NotifyIcon { get; set; }
        public bool AllowMultipleInstances { get; set; } = false;
        public SetupInfo Setup { get; set; }

        #region Run

        protected override void OnStartup(StartupEventArgs e)
        {

            mutex = new Mutex(false, PackageName);

            if (!mutex.WaitOne(0, false))
            {
                IsFirstInstance = false;
                SendArgs();
                if (!AllowMultipleInstances)
                    Shutdown();
                else
                    ProcessArguments?.Invoke(Environment.GetCommandLineArgs(), true);
            }
            else
            {

                IsFirstInstance = true;
                ProcessArguments?.Invoke(Environment.GetCommandLineArgs(), false);
                ListenForArgs();

            }

            base.OnStartup(e);
            Dispatcher.Run();

        }

        protected override void OnExit(ExitEventArgs e)
        {

            mutex?.Dispose();
            NotifyIcon?.Dispose();

            base.OnExit(e);

        }

        #endregion
        #region Info

        public InfoHelper Info { get; }

        public struct InfoHelper
        {

            private FileVersionInfo FileVersionInfo => FileVersionInfo.GetVersionInfo(ExecutablePath);

            public string Publisher => FileVersionInfo.CompanyName;
            public string Name      => FileVersionInfo.ProductName;
            public string Version   => FileVersionInfo.ProductVersion;

        }

        #endregion
        #region Autostart

        public static string ExecutablePath => Assembly.GetEntryAssembly().Location;
        public static string ExecutablePathWithQuotes => @"""" + ExecutablePath + @"""";

        public bool IsAutoStartEnabled
        {
            get => AutoStartKey(k => (string)k.GetValue(PackageName, "") == ExecutablePathWithQuotes);
            set => AutoStartKey(k => k.SetValue(PackageName, value ? ExecutablePathWithQuotes : string.Empty));
        }

        private static void AutoStartKey(Action<RegistryKey> action)
        {
            using var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            action?.Invoke(key);
        }

        private static dynamic AutoStartKey(Func<RegistryKey, dynamic> func)
        {
            using var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            return func?.Invoke(key);
        }

        #endregion
        #region Args

        private static void ListenForArgs()
        {
            Task.Run(() =>
            {

                using (var server = new NamedPipeServerStream(Current.PackageName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.None))
                {
                    server.WaitForConnection();
                    using var reader = new StreamReader(server);
                    var args = reader.ReadToEnd().Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    dispatcher.Invoke(() => Current.ProcessArguments?.Invoke(args, true));
                }

                ListenForArgs();

            });

        }

        private static void SendArgs()
        {
            try
            {
                var client = new NamedPipeClientStream(".", Current.PackageName, PipeDirection.Out, PipeOptions.None);
                client.Connect(1000);
                using var writer = new StreamWriter(client);
                writer.Write(string.Join(Environment.NewLine, Environment.GetCommandLineArgs().Skip(1).ToArray()));
            }
            catch
            { }
        }

        #endregion

        const string PackScheme = "pack";
        private void RegisterPackUriScheme()
        {
            if (!UriParser.IsKnownScheme(PackScheme))
                UriParser.Register(new GenericUriParser(GenericUriParserOptions.GenericAuthority), PackScheme, -1);
        }

        public void Restart()
        {
            mutex?.Dispose();
            Process.Start(Process.GetCurrentProcess().MainModule.FileName);
            Shutdown();
        }

    }

}
