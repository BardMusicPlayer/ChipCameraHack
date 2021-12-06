using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows.Threading;
using System.Reflection;
using CameraHackTool.UI;
using UI;

namespace CameraHackTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<ProcessModel> AllSelectedProcesses { get; } = new List<ProcessModel>();
        private ProcessModel HighlightedProcess;
 
        public MainWindow()
        {
            InitializeComponent();

            // uncomment to test many processes
            //AllSelectedProcesses.Add(new ProcessModel { Name = "1", Running = false });
            //AllSelectedProcesses.Add(new ProcessModel { Name = "2", Running = false });
            //AllSelectedProcesses.Add(new ProcessModel { Name = "3", Running = false });
            //AllSelectedProcesses.Add(new ProcessModel { Name = "4", Running = false });
            //AllSelectedProcesses.Add(new ProcessModel { Name = "5", Running = false });
            //AllSelectedProcesses.Add(new ProcessModel { Name = "6", Running = false });
            //AllSelectedProcesses.Add(new ProcessModel { Name = "7", Running = false });
            //AllSelectedProcesses.Add(new ProcessModel { Name = "8", Running = false });
            //AllSelectedProcesses.Add(new ProcessModel { Name = "9", Running = false });
            //AllSelectedProcesses.Add(new ProcessModel { Name = "10", Running = false });

            // initialize singletons
            Memory.TheMainWindow = this;

            // initialize delegates
            this.Loaded += MainWindow_Loaded;

            // initialize variables
            ListBox_RunningProcesses.DataContext = AllSelectedProcesses;

            // set title to show version information
            this.Title = "ChipCameraHack v" + Assembly.GetExecutingAssembly().GetName().Version.ToString(2);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var res = Metadata.Instance.grabApplicationMetadata();
            switch (res)
            {
                case Metadata.MetadataResult.Failure:
                    Debug.WriteLine("Something has gone terribly wrong");
                    Application.Current.Shutdown();
                    break;
                case Metadata.MetadataResult.RunningBeta:
                    this.Title += " (Beta)";
                    break;
                case Metadata.MetadataResult.UpdateAvailable:
                    var opt = MessageBox.Show(
                        $"You are running version {Metadata.Instance.LocalVersion.ToString(2)}.\n" +
                        $"Update {Metadata.Instance.NewerVersion.ToString(2)} is available for download.\n\n" +
                        $"Go to the downloads page?\n\n",
                        "Update Available", MessageBoxButton.YesNoCancel
                    );

                    switch (opt)
                    {
                        case MessageBoxResult.Yes:
                            Process.Start(new ProcessStartInfo { FileName = Metadata.Instance.DownloadURL, UseShellExecute = true });
                            break;
                        case MessageBoxResult.Cancel:
                            Application.Current.Shutdown();
                            break;
                    }
                    break;
                case Metadata.MetadataResult.Success:
                    break;
            }
        }

        private void Button_DoTheThing_Click(object sender, RoutedEventArgs e)
        {
            foreach (var proc in AllSelectedProcesses)
            {
                StartProcess(proc);
            }

            ListBox_RunningProcesses.Items.Refresh();
        }

        private void Button_LoadProcess_Click(object sender, RoutedEventArgs e)
        {
            ProcessSelection processSelection = new ProcessSelection();
            processSelection.Top = this.Top;
            processSelection.Left = this.Left;
            Nullable<bool> dialogResult = processSelection.ShowDialog();
            if (dialogResult == true)
            {
                if (processSelection.NewSelectedProcesses != null)
                {
                    foreach (var SelectedProcess in processSelection.NewSelectedProcesses)
                    {
                        SelectedProcess.Hooked = true;
                        AllSelectedProcesses.Add(SelectedProcess);
                    }
                    ListBox_RunningProcesses.Items.Refresh();
                }
            }
        }

        private void StartProcess(ProcessModel proc)
        {
            if (proc.Running == false)
            {
                Memory.RunCameraHack(proc.Process);
                proc.Running = true;
            }
        }

        private void StopProcess(ProcessModel proc)
        {
            if (proc.Running == true)
            {
                Memory.StopCameraHack(proc.Process);
                proc.Running = false;
            }
        }

        private void RemoveProcess(ProcessModel proc)
        {
            try
            {
                StopProcess(proc);
                proc.Hooked = false;
                AllSelectedProcesses.Remove(AllSelectedProcesses.Where(x => x.Process.Id == proc.Process.Id).ToList()[0]);
                proc = null;
                ListBox_RunningProcesses.Items.Refresh();
            }
            catch (Exception e)
            {
                MessageBox.Show(
                    "Something FUBAR is happening. Reason: " + e.Message + "\n\n" + e.StackTrace,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error
                );
            }
        }

        public void RemoveProcessFromId(int pid)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
                ProcessModel selectedModel = AllSelectedProcesses.Where(x => x.Process.Id == pid).ToList()[0];
                RemoveProcess(selectedModel);
            }));
        }

        private void Button_StopProcess_Click(object sender, RoutedEventArgs e)
        {
            if (HighlightedProcess != null)
            {
                StopProcess(HighlightedProcess);
                ListBox_RunningProcesses.Items.Refresh();
            }
        }

        private void Button_RemoveProcess_Click(object sender, RoutedEventArgs e)
        {
            if (HighlightedProcess != null)
            {
                RemoveProcess(HighlightedProcess);
                ListBox_RunningProcesses.Items.Refresh();
            }
        }

        private void ListBox_RunningProcesses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1)
            {
                HighlightedProcess = (e.AddedItems[0] as ProcessModel);
            }
        }

        private void Button_StopAllProcesses_Click(object sender, RoutedEventArgs e)
        {
            foreach (var proc in AllSelectedProcesses)
            {
                StopProcess(proc);
            }

            ListBox_RunningProcesses.Items.Refresh();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (var proc in AllSelectedProcesses)
            {
                StopProcess(proc);
            }

            ListBox_RunningProcesses.Items.Refresh();
        }

        private void ListBox_RunningProcesses_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if ((sender as ListBox).SelectedItem != null)
            {
                HighlightedProcess = ((sender as ListBox).SelectedItem as ProcessModel);
                if (HighlightedProcess.Running)
                {
                    StopProcess(HighlightedProcess);
                }
                else
                {
                    StartProcess(HighlightedProcess);
                }

                ListBox_RunningProcesses.Items.Refresh();
            }
        }

        private void Button_Info_Click(object sender, RoutedEventArgs e)
        {
            Information processSelection = new Information();
            processSelection.Top = this.Top;
            processSelection.Left = this.Left;
            Nullable<bool> dialogResult = processSelection.ShowDialog();
            if (dialogResult == true)
            {
                // ???
            }
        }
    }
}
