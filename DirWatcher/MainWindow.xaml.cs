using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace DirWatcher
{
    public class NetworkResourceMounter
    {
        private const int RESOURCETYPE_DISK = 0x00000001;
        private const int CONNECT_UPDATE_PROFILE = 0x00000001;
        private const int ERROR_SUCCESS = 0;

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetAddConnection2([In] NETRESOURCE netResource, string password, string username, int flags);

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetCancelConnection2(string name, int flags, bool force);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class NETRESOURCE
        {
            public int dwScope;
            public int dwType;
            public int dwDisplayType;
            public int dwUsage;
            public string? lpLocalName;
            public string? lpRemoteName;
            public string? lpComment;
            public string? lpProvider;
        }

        public bool MountNetworkResource(string driveLetter, string remotePath, string username, string password)
        {
            var netResource = new NETRESOURCE
            {
                dwType = RESOURCETYPE_DISK,
                lpLocalName = driveLetter,
                lpRemoteName = remotePath
            };

            int result = WNetAddConnection2(netResource, password, username, CONNECT_UPDATE_PROFILE);
            if (result != ERROR_SUCCESS)
            {
                string errorMessage = new Win32Exception(result).Message;
                MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        public bool UnmountNetworkResource(string driveLetter)
        {
            int result = WNetCancelConnection2(driveLetter, CONNECT_UPDATE_PROFILE, false);
            if (result != ERROR_SUCCESS)
            {
                string errorMessage = new Win32Exception(result).Message;
                MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }
    }

    /// Interaction logic for MainWindow.xaml
    public partial class MainWindow : Window
    {
        // Define members
        System.IO.FileSystemWatcher? watcher;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize text of components of Mounter
            cbDriveLetter.Items.Add("T:");
            cbDriveLetter.Items.Add("U:");
            cbDriveLetter.Items.Add("V:");
            cbDriveLetter.Items.Add("W:");
            cbDriveLetter.Items.Add("X:");
            cbDriveLetter.Items.Add("Y:");
            cbDriveLetter.Items.Add("Z:");
            cbDriveLetter.SelectedIndex = 5;
            txtbxServerPath.Text = "\\\\10.20.145.192\\Data";
            txtbxUsername.Text = "sambauser";
            passbxPassword.Password = "sambapasswd";

            // Initialize text of components for Watcher
            statusBar.Items.Add(new StatusBarItem()
            {
                Content = "Idle"
            });
            txtbxMonitorDirectory.Text = "C:\\ProgramData\\Siemens\\Numaris\\SimMeasData\\NORDIC";
            txtbxCopyDirectory.Text = "Y:\\";
        }

        private void StartStop(object sender, RoutedEventArgs e)
        {
            // Are we in start mode or stop mode?
            if (btnStartStop.Content.ToString() == "Start")
            {
                // Grab the directories
                String MonitorDirectory = txtbxMonitorDirectory.Text;
                String CopyDirectory = txtbxCopyDirectory.Text;

                // Check if the directories are valid
                if (!System.IO.Directory.Exists(MonitorDirectory))
                {
                    MessageBox.Show("Monitor Directory does not exist!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (!System.IO.Directory.Exists(CopyDirectory))
                {
                    MessageBox.Show("Copy Directory does not exist!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Change the button to Stop
                btnStartStop.Content = "Stop";

                // Disable the inputs
                txtbxMonitorDirectory.IsEnabled = false;
                txtbxCopyDirectory.IsEnabled = false;

                // Update the Status Bar
                statusBar.Items.Clear();
                statusBar.Items.Add(new StatusBarItem()
                {
                    Content = "Waiting for .dat files..."
                });

                // Create a file watcher
                watcher = new System.IO.FileSystemWatcher();

                // Set the path to the directory to watch
                watcher.Path = MonitorDirectory;
                // filter for .dat files
                watcher.Filter = "*.dat";
                // Subscribe to the Created event
                watcher.Created += (s, e) =>
                {
                    bool success = false;
                    const int maxRetries = 100;
                    const int retryDelayMilliseconds = 100;

                    for (int i = 0; i < maxRetries; i++)
                    {
                        try
                        {
                            // Copy the file
                            System.IO.File.Copy(e.FullPath, CopyDirectory + e.Name, true);
                            success = true;
                            break;
                        }
                        catch (System.IO.IOException)
                        {
                            // File is still in use, retry after a delay
                            Thread.Sleep(retryDelayMilliseconds);
                        }
                    }

                    // Update the Status Bar
                    Dispatcher.Invoke(() =>
                    {
                        statusBar.Items.Clear();
                        statusBar.Items.Add(new StatusBarItem()
                        {
                            Content = success ? "Copied " + e.Name + "!" : "Failed to copy " + e.Name + "!"
                        });
                    });
                };

                // Start listening for events
                watcher.EnableRaisingEvents = true;
            }
            else
            {
                // Stop event listening if watcher defined
                if (watcher != null)
                    watcher.EnableRaisingEvents = false;

                // Reenable the inputs
                txtbxMonitorDirectory.IsEnabled = true;
                txtbxCopyDirectory.IsEnabled = true;

                // Update the Status Bar
                statusBar.Items.Clear();
                statusBar.Items.Add(new StatusBarItem()
                {
                    Content = "Idle"
                });
                // Change the button to Start
                btnStartStop.Content = "Start";
            }
        }

        private void Mount(object sender, RoutedEventArgs e)
        {
            if (btnMount.Content.ToString() == "Mount")
            {
                // Disable the button until we are done
                btnMount.IsEnabled = false;

                // Disable the inputs
                cbDriveLetter.IsEnabled = false;
                txtbxServerPath.IsEnabled = false;
                txtbxUsername.IsEnabled = false;
                passbxPassword.IsEnabled = false;

                // Get the strings
                String DriveLetter = cbDriveLetter.Text;
                String ServerPath = txtbxServerPath.Text;
                String Username = txtbxUsername.Text;
                String Password = passbxPassword.Password;

                // Run the mounting in a separate thread
                Task.Run(() =>
                {
                    // Update the Status Bar with mounting attempt
                    Dispatcher.Invoke(() =>
                    {
                        serverStatusBar.Items.Clear();
                        serverStatusBar.Items.Add(new StatusBarItem()
                        {
                            Content = "Mounting " + txtbxServerPath.Text + " to " + cbDriveLetter.Text
                        });
                    });

                    // Mount the network drive
                    NetworkResourceMounter networkResourceMounter = new NetworkResourceMounter();
                    if (!networkResourceMounter.MountNetworkResource(DriveLetter, ServerPath, Username, Password))
                    {
                        // Mounting failed
                        Dispatcher.Invoke(() =>
                        {
                            // reenable the button
                            btnMount.IsEnabled = true;
                            // Update the status
                            serverStatusBar.Items.Clear();
                            serverStatusBar.Items.Add(new StatusBarItem()
                            {
                                Content = "Mounting " + txtbxServerPath.Text + " failed!"
                            });
                            // Reenable the inputs
                            cbDriveLetter.IsEnabled = true;
                            txtbxServerPath.IsEnabled = true;
                            txtbxUsername.IsEnabled = true;
                            passbxPassword.IsEnabled = true;
                        });
                        return;
                    }

                    // Mounting succeeded
                    Dispatcher.Invoke(() =>
                    {
                        // Update the Status Bar if successful
                        serverStatusBar.Items.Clear();
                        serverStatusBar.Items.Add(new StatusBarItem()
                        {
                            Content = "Mounted " + txtbxServerPath.Text + " to " + cbDriveLetter.Text
                        });

                        // Change the button to Unmount
                        btnMount.Content = "Unmount";
                        btnMount.IsEnabled = true;
                    });
                });
            }
            else
            {
                // Disable the button until we are done
                btnMount.IsEnabled = false;

                // Enable the inputs
                cbDriveLetter.IsEnabled = true;
                txtbxServerPath.IsEnabled = true;
                txtbxUsername.IsEnabled = true;
                passbxPassword.IsEnabled = true;

                // Unmount the network drive
                NetworkResourceMounter networkResourceMounter = new NetworkResourceMounter();
                if (!networkResourceMounter.UnmountNetworkResource(cbDriveLetter.Text))
                {
                    btnMount.IsEnabled = true;
                    return;
                }

                // Update the status bar
                Dispatcher.Invoke(() =>
                {
                    // Update the Status Bar
                    serverStatusBar.Items.Clear();
                    serverStatusBar.Items.Add(new StatusBarItem()
                    {
                        Content = "Unmounted " + cbDriveLetter.Text
                    });
                });

                // Change the button to Mount
                btnMount.Content = "Mount";
                btnMount.IsEnabled = true;
            }
        }
    }
}
