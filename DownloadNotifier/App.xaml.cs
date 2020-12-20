using System.IO;
using System.Windows;
using Microsoft.Toolkit.Uwp.Notifications;
using Syroot.Windows.IO;
using Windows.UI.Notifications;

namespace DownloadNotifier
{

    partial class App : Common.Wpf.App
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            StartWatcher();
            base.OnStartup(e);
        }

        void ViewDownloads(object sender, RoutedEventArgs e) =>
            OpenUtility.ViewDownloadsFolder();

        void Shutdown(object sender, RoutedEventArgs e) =>
            Shutdown();

        #region File system watcher

        static FileSystemWatcher watcher;

        static void StartWatcher()
        {

            watcher?.Dispose();

            watcher = new FileSystemWatcher(KnownFolders.Downloads.Path) { EnableRaisingEvents = true };

            watcher.Changed += OnFileChanged;
            watcher.Error += (s, e) => StartWatcher();

            ToastNotificationManagerCompat.OnActivated += (e) =>
            {
                if (e.Argument.StartsWith("View:"))
                    OpenUtility.ViewInExplorer(e.Argument["View:".Length..]);
                else if (e.Argument.StartsWith("Open:"))
                    OpenUtility.Open(e.Argument["Open:".Length..]);
                else if (e.Argument.StartsWith("OpenWith:"))
                    OpenUtility.OpenWith(e.Argument["OpenWith:".Length..]);
            };

        }

        static void OnFileChanged(object sender, FileSystemEventArgs e)
        {

            if (e.FullPath.EndsWith(".part"))
                return;

            var content = new ToastContentBuilder().
                AddToastActivationInfo("Open:" + e.FullPath, ToastActivationType.Background).
                SetToastScenario(ToastScenario.Reminder).
                AddText("'" + e.Name + "' just got downloaded.").
                AddButton("Open with...", ToastActivationType.Background, "OpenWith:" + e.FullPath).
                AddButton("View in downloads", ToastActivationType.Background, "View:" + e.FullPath).
                GetToastContent();

            var toast = new ToastNotification(content.GetXml());

            ToastNotificationManagerCompat.CreateToastNotifier().Show(toast);

        }

        #endregion

    }

}
