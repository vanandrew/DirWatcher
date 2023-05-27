using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace DirWatcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        FileSystemWatcher? watcher;
        public MainWindow()
        {
            InitializeComponent();
            // Initialize text of components
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
                if (!Directory.Exists(MonitorDirectory))
                {
                    MessageBox.Show("Monitor Directory does not exist!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (!Directory.Exists(CopyDirectory))
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
                watcher = new FileSystemWatcher();

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
                            File.Copy(e.FullPath, CopyDirectory + e.Name, true);
                            success = true;
                            break;
                        }
                        catch (IOException)
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
    }
}
