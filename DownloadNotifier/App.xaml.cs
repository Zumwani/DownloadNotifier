using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Windows;
using Microsoft.Toolkit.Uwp.Notifications;
using Syroot.Windows.IO;

namespace DownloadNotifier
{

    partial class App : Common.Wpf.App
    {

        protected override void OnStartup(StartupEventArgs e)
        {

            ToastNotificationManagerCompat.OnActivated += OnToastActivate;

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

        }

        static void OnFileChanged(object sender, FileSystemEventArgs e)
        {

            if (e.FullPath.EndsWith(".part") || e.FullPath.EndsWith(".crdownload"))
                return;

            SendToast(e.FullPath);

        }

        #endregion
        #region Toast

        public enum ActivateAction
        {

            [Display(Name = "Open")]
            Open,

            [Display(Name = "Open with...")]
            OpenWith,

            [Display(Name = "View in downloads")]
            View

        }

        static void OnToastActivate(ToastNotificationActivatedEventArgsCompat e)
        {

            if (Enum.TryParse<ActivateAction>(e.Argument[..e.Argument.IndexOf(":")], out var action))
                GetAction()?.Invoke();

            Action GetAction() =>
                action switch
                {
                    ActivateAction.Open =>      () => OpenUtility.Open(e.Argument["Open:".Length..]),
                    ActivateAction.OpenWith =>  () => OpenUtility.OpenWith(e.Argument["OpenWith:".Length..]),
                    ActivateAction.View =>      () => OpenUtility.ViewInExplorer(e.Argument["View:".Length..]),
                    _ => null,
                };

        }

        static void SendToast(string file) =>
            new ToastContentBuilder().
                SetToastScenario(ToastScenario.Reminder).
                AddText($"'{Path.GetFileName(file)}' just got downloaded.").
                AddAction(ActivateAction.Open,      file).
                AddButton(ActivateAction.OpenWith,  file).
                AddButton(ActivateAction.View,      file).
                Send();

        #endregion

    }

}
