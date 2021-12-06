namespace UI
{
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Windows;
    using System.Linq;
    using CameraHackTool;

    public partial class ProcessSelection : Window
    {
        public List<ProcessModel> NewSelectedProcesses { get; private set; }
        static List<ProcessModel> ProcessList { get; set; }

        public ProcessSelection()
        {
            InitializeComponent();

            if (ProcessList == null)
                ProcessList = new List<ProcessModel>();

            {
                foreach (var proc in Process.GetProcesses())
                {
                    List<ProcessModel> compared = ProcessList.AsParallel().Where(x => x.Process.Id == proc.Id).ToList();
                    if (compared.Count == 0)
                    {
                        if (string.Equals(proc.ProcessName, "ffxiv_dx11"))
                        {
                            Metadata.Instance.InitializeToRegionFromGamePath(proc.MainModule.FileName);

                            string characterName = Memory.GetCharacterNameFromProcess(proc);
                            if (characterName.Length > 0)
                            {
                                ProcessList.Add(new ProcessModel
                                {
                                    Name = characterName,
                                    Process = proc,
                                    Hooked = false,
                                    Running = false
                                });
                            }
                        }
                    }
                }
            }

            ListBox_Processes.DataContext = ProcessList.Where(t => t.Hooked == false);
        }

        private void Button_OpenAllProcess_Click(object sender, RoutedEventArgs e)
        {
            NewSelectedProcesses = ProcessList.Where(t => t.Hooked == false).ToList();
            DialogResult = true;
        }

        private void Button_OpenThisProcess_Click(object sender, RoutedEventArgs e)
        {
            NewSelectedProcesses = new List<ProcessModel>();
            if (ListBox_Processes.SelectedItems.Count != 0)
            {
                foreach (var proc in ListBox_Processes.SelectedItems)
                {
                    var p = proc as ProcessModel;
                    NewSelectedProcesses.Add(p);
                }
                DialogResult = true;
            }
        }
    }

    public class ProcessModel
    {
        public string Name { get; set; }
        public Process Process { get; set; }
        public bool Hooked { get; set; }
        public bool Running { get; set; }

        public string GetFormattedName {
            get {
                return string.Format("({0})\t| Active: {1} | {2}", Process == null ? 0 : Process.Id, Running ? "✓" : "✗", Name);
            }
        }
    }
}
