using IWshRuntimeLibrary;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace DirWatcher
{
    /// Interaction logic for MainWindow.xaml
    public partial class MainWindow : Window
    {
        // Define members
        System.IO.FileSystemWatcher? watcher;
        WshNetwork? network;

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

                // Mount the network drive
                network = new WshNetwork();
                try
                {
                    network.MapNetworkDrive(cbDriveLetter.Text, txtbxServerPath.Text, false, txtbxUsername.Text, passbxPassword.Password);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    btnMount.IsEnabled = true;
                    return;
                }

                // Disable the inputs
                cbDriveLetter.IsEnabled = false;
                txtbxServerPath.IsEnabled = false;
                txtbxUsername.IsEnabled = false;
                passbxPassword.IsEnabled = false;
                Dispatcher.Invoke(() =>
                {
                    // Update the Status Bar
                    serverStatusBar.Items.Clear();
                    serverStatusBar.Items.Add(new StatusBarItem()
                    {
                        Content = "Mounted " + txtbxServerPath.Text + " to " + cbDriveLetter.Text
                    });
                });

                // Change the button to Unmount
                btnMount.Content = "Unmount";
                btnMount.IsEnabled = true;
            }
            else
            {
                // Disable the button until we are done
                btnMount.IsEnabled = false;

                // Unmount the network drive
                if (network != null)
                {
                    try
                    {
                        network.RemoveNetworkDrive(cbDriveLetter.Text, true, true);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        // return to Mount state
                        btnMount.Content = "Mount";
                        btnMount.IsEnabled = true;
                        return;
                    }
                }

                // Enable the inputs
                cbDriveLetter.IsEnabled = true;
                txtbxServerPath.IsEnabled = true;
                txtbxUsername.IsEnabled = true;
                passbxPassword.IsEnabled = true;
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
