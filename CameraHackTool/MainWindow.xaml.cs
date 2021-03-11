using FFXIVUtil;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

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

            ListBox_RunningProcesses.DataContext = AllSelectedProcesses;
        }

        private void Button_DoTheThing_Click(object sender, RoutedEventArgs e)
        {
            foreach (var proc in AllSelectedProcesses)
            {
                if (proc.Running == false)
                {
                    Memory.RunCameraHack(proc.Process);
                    proc.Running = true;
                }
            }

            ListBox_RunningProcesses.Items.Refresh();
        }

        private void Button_LoadProcess_Click(object sender, RoutedEventArgs e)
        {
            ProcessSelection processSelection = new ProcessSelection();
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

        private void StopProcess(ProcessModel proc)
        {
            Memory.StopCameraHack(proc.Process);
            proc.Running = false;
        }

        private void RemoveProcess(ProcessModel proc)
        {
            StopProcess(proc);
            proc.Hooked = false;
            AllSelectedProcesses.Remove(AllSelectedProcesses.Where(x => x.Process.Id == proc.Process.Id).ToList()[0]);
            proc = null;
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
    }
}
