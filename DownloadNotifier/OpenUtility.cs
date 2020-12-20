using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Syroot.Windows.IO;

namespace DownloadNotifier
{

    public static class OpenUtility
    {

        #region Pinvoke

        [DllImport("shell32.dll", EntryPoint = "SHOpenWithDialog", CharSet = CharSet.Unicode)]
        private static extern int SHOpenWithDialog(IntPtr hWndParent, ref OPENASINFO oOAI);

        private struct OPENASINFO
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string cszFile;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string cszClass;

            [MarshalAs(UnmanagedType.I4)]
            public OPEN_AS_INFO_FLAGS oaifInFlags;
        }

        [Flags]
        private enum OPEN_AS_INFO_FLAGS
        {
            OAIF_ALLOW_REGISTRATION = 0x00000001,   // Show "Always" checkbox
            OAIF_REGISTER_EXT = 0x00000002,   // Perform registration when user hits OK
            OAIF_EXEC = 0x00000004,   // Exec file after registering
            OAIF_FORCE_REGISTRATION = 0x00000008,   // Force the checkbox to be registration
            OAIF_HIDE_REGISTRATION = 0x00000020,   // Vista+: Hide the "always use this file" checkbox
            OAIF_URL_PROTOCOL = 0x00000040,   // Vista+: cszFile is actually a URI scheme; show handlers for that scheme
            OAIF_FILE_IS_URI = 0x00000080    // Win8+: The location pointed to by the pcszFile parameter is given as a URI
        }

        #endregion

        /// <summary>Opens the downloads folder in explorer.</summary>
        public static void ViewDownloadsFolder() =>
            Open(KnownFolders.Downloads.Path);

        /// <summary>Opens the file or opens the folder in explorer.</summary>
        public static void Open(string file)
        {
            if (File.Exists(file) || Directory.Exists(file))
                Process.Start("explorer", file);
        }

        /// <summary>Opens the file using the open with dialog.</summary>
        public static void OpenWith(string file)
        {

            if (!File.Exists(file))
                return;

            var info = new OPENASINFO
            {
                cszFile = file,
                oaifInFlags = OPEN_AS_INFO_FLAGS.OAIF_ALLOW_REGISTRATION | OPEN_AS_INFO_FLAGS.OAIF_REGISTER_EXT | OPEN_AS_INFO_FLAGS.OAIF_EXEC
            };

            SHOpenWithDialog(IntPtr.Zero, ref info);

        }

        /// <summary>View and select the file or folder in explorer.</summary>
        public static void ViewInExplorer(string file)
        {

            if (!(File.Exists(file) || Directory.Exists(file)))
                return;

            var args = string.Format("/e, /select, \"{0}\"", file);

            var info = new ProcessStartInfo
            {
                FileName = "explorer",
                Arguments = args
            };

            try
            {
                Process.Start(info);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }

        }

    }

}
