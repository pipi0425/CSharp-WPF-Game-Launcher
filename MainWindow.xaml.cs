using System;
using System.IO;
using System.IO.Compression;
using System.ComponentModel;
using System.Windows;
using System.Diagnostics;
using System.Configuration;
using System.Net;

namespace PZ_s_Game_Launcher
{
    enum LauncherStatus
    {
        ready,
        failed,
        downloadingGame,
        downloadingUpdate
    }

    public partial class MainWindow : Window
    {
        //vars: file and path
        private string rootPath;
        private string remotePath;

        private string versionSuffix = "Version.txt";
        private string versionFile;
        private string remoteVersionFile;

        private string patchNoteSuffix = "PatchNote.txt";
        private string remotePatchNote;

        private string gameZipSuffix = "Build.zip";
        private string gameZip;
        private string remoteGameZip;

        private string gameExeSuffix;
        private string gameExe;


        //Launcher Status
        private LauncherStatus _status;
        internal LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.ready:
                        PlayButton.Content = "Play";
                        PlayButton.IsEnabled = true;
                        break;
                    case LauncherStatus.failed:
                        PlayButton.Content = "Update Failed - Retry";
                        PlayButton.IsEnabled = true;
                        break;
                    case LauncherStatus.downloadingGame:
                        PlayButton.Content = "Downloading Game";
                        PlayButton.IsEnabled = false;
                        break;
                    case LauncherStatus.downloadingUpdate:
                        PlayButton.Content = "Downloading Update";
                        PlayButton.IsEnabled = false;
                        break;
                    default:
                        break;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            //set vars
            rootPath = Path.Combine(Directory.GetCurrentDirectory(), "Game");
            remotePath = ConfigurationManager.AppSettings["downloadLink"];
            Directory.CreateDirectory("Game");

            versionFile = Path.Combine(rootPath, versionSuffix);
            remoteVersionFile = remotePath + versionSuffix;

            remotePatchNote = remotePath + patchNoteSuffix;

            gameZip = Path.Combine(rootPath, gameZipSuffix);
            remoteGameZip = remotePath + gameZipSuffix;

            gameExeSuffix = ConfigurationManager.AppSettings["exeName"];
            gameExe = Path.Combine(rootPath, gameExeSuffix);
        }

        //fetch patch notes
        private void FetchPatchNote()
        {
            try
            {
                WebClient webClient = new WebClient();
                PatchNoteText.Text = webClient.DownloadString(new Uri(remotePatchNote, UriKind.Absolute));
            }
            catch (Exception ex)
            {
                PatchNoteText.Text = "Connection to remote server failed...";
                Status = LauncherStatus.failed;
                MessageBox.Show($"Fetch Patch Note Failed: {ex}");
            }
        }

        //check for updates and call InstallGameFiles
        private void CheckForUpdates()
        {
            if (File.Exists(versionFile))
            {
                Version localVer = new Version(File.ReadAllText(versionFile));
                VersionText.Text = localVer.ToString();

                try
                {
                    WebClient webClient = new WebClient();
                    Version onlineVer = new Version(webClient.DownloadString(new Uri(remoteVersionFile, UriKind.Absolute)));

                    if (onlineVer.IsDiff(localVer))
                    {
                        InstallGameFiles(true, onlineVer);
                    }
                    else
                    {
                        Status = LauncherStatus.ready;
                    }
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.failed;
                    MessageBox.Show($"Error checking for game updates: {ex}");
                }
            }
            else
            {
                InstallGameFiles(false, Version.zero);
            }
        }

        //start async download
        private void InstallGameFiles(bool _isUpdate, Version _onlineVer)
        {
            try
            {
                WebClient webClient = new WebClient();

                if (_isUpdate)
                {
                    Status = LauncherStatus.downloadingUpdate;
                }
                else
                {
                    Status = LauncherStatus.downloadingGame;
                    _onlineVer = new Version(webClient.DownloadString(new Uri(remoteVersionFile, UriKind.Absolute)));
                }

                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressChangeCallback);
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                webClient.DownloadFileAsync(new Uri(remoteGameZip, UriKind.Absolute), gameZip, _onlineVer);
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error installing game files: {ex}");
            }
        }

        private void DownloadProgressChangeCallback(object sender, DownloadProgressChangedEventArgs e)
        {
            long MBRecv = e.BytesReceived / 1048576;
            long MBTotal = e.TotalBytesToReceive / 1048576;
            int percentage = e.ProgressPercentage;
            PlayButton.Content = $"{MBRecv}MB / {MBTotal}MB ( {percentage}% )";
        }

        private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            PlayButton.Content = "Extracting Content...";
            try
            {
                string onlineVer = ((Version)e.UserState).ToString();
                ZipFile.ExtractToDirectory(gameZip, rootPath, true);
                File.Delete(gameZip);

                File.WriteAllText(versionFile, onlineVer);

                VersionText.Text = onlineVer;
                Status = LauncherStatus.ready;
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error finishing download: {ex}");
            }
        }

        //execute on content rendered
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            FetchPatchNote();
            CheckForUpdates();
        }

        //on button clicked
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                startInfo.WorkingDirectory = rootPath;
                Process.Start(startInfo);

                this.WindowState = WindowState.Minimized;
            }
            else if (Status == LauncherStatus.failed)
            {
                CheckForUpdates();
            }
        }

        //struct: version
        struct Version
        {
            internal static Version zero = new Version(0, 0, 0);

            private short major;
            private short minor;
            private short fix;

            internal Version(short _major, short _minor, short _fix)
            {
                major = _major;
                minor = _minor;
                fix = _fix;
            }

            internal Version(string _version)
            {
                string[] versionStrings = _version.Split('.');
                if (versionStrings.Length != 3)
                {
                    major = 0;
                    minor = 0;
                    fix = 0;
                    return;
                }
                major = short.Parse(versionStrings[0]);
                minor = short.Parse(versionStrings[1]);
                fix = short.Parse(versionStrings[2]);
            }

            internal bool IsDiff(Version _otherVer)
            {
                if (major != _otherVer.major)
                {
                    return true;
                }
                else
                {
                    if (minor != _otherVer.minor)
                    {
                        return true;
                    }
                    else
                    {
                        if (fix != _otherVer.fix)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            public override string ToString()
            {
                return $"{major}.{minor}.{fix}";
            }
        }

    }
}
