using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CameraHackTool.UI
{
    public partial class Information : Window
    {
        public Information()
        {
            InitializeComponent();

            TextBlock_Info.Inlines.Clear();
            TextBlock_Info.Inlines.Add($"ChipCameraHack v" + Assembly.GetExecutingAssembly().GetName().Version.ToString(2) + ".\n");
            TextBlock_Info.Inlines.Add($"Developed by Chipotle Ismylife.\n");
            TextBlock_Info.Inlines.Add($"Ko-fi (buy me a drink!): ");
            Hyperlink kflink = new Hyperlink()
            {
                NavigateUri = new Uri("https://ko-fi.com/trotlinebeercan")
            };
            kflink.Inlines.Add("https://ko-fi.com/trotlinebeercan");
            kflink.RequestNavigate += Hyperlink_RequestNavigate;
            TextBlock_Info.Inlines.Add(kflink);
            TextBlock_Info.Inlines.Add("\n");
            TextBlock_Info.Inlines.Add($"Github: ");
            Hyperlink ghlink = new Hyperlink()
            {
                NavigateUri = new Uri("https://github.com/BardMusicPlayer/ChipCameraHack")
            };
            ghlink.Inlines.Add("https://github.com/BardMusicPlayer/ChipCameraHack");
            ghlink.RequestNavigate += Hyperlink_RequestNavigate;
            TextBlock_Info.Inlines.Add(ghlink);
            TextBlock_Info.Inlines.Add("\n");
            TextBlock_Info.Inlines.Add($"Metadata (for nerds):\n");
            TextBlock_Info.Inlines.Add($"\tLocalRegion:\t{Metadata.Instance.LocalRegion}\n");
            TextBlock_Info.Inlines.Add($"\tLocalVersion:\t{Metadata.Instance.LocalVersion}\n");
            TextBlock_Info.Inlines.Add($"\tNewerVersion:\t{Metadata.Instance.NewerVersion}\n");
            TextBlock_Info.Inlines.Add($"\tDownloadURL:\t{Metadata.Instance.DownloadURL}\n");
            TextBlock_Info.Inlines.Add($"\tCameraZoom:\tffxiv_dx11.exe+0x{Metadata.Instance.CameraZoom.Address:X} + 0x{Metadata.Instance.CameraZoom.Offset:X}\n");
            TextBlock_Info.Inlines.Add($"\tCameraFOV:\tffxiv_dx11.exe+0x{Metadata.Instance.CameraFOV.Address:X} + 0x{Metadata.Instance.CameraFOV.Offset:X}\n");
            TextBlock_Info.Inlines.Add($"\tCameraAngleX:\tffxiv_dx11.exe+0x{Metadata.Instance.CameraAngleX.Address:X} + 0x{Metadata.Instance.CameraAngleX.Offset:X}\n");
            TextBlock_Info.Inlines.Add($"\tCameraAngleY:\tffxiv_dx11.exe+0x{Metadata.Instance.CameraAngleY.Address:X} + 0x{Metadata.Instance.CameraAngleY.Offset:X}\n");
            TextBlock_Info.Inlines.Add($"\tNameOffset:\t{Metadata.Instance.PlayerNameOffset}\n");
            TextBlock_Info.Inlines.Add($"\tHeightOffset:\t{Metadata.Instance.CameraHeightOffset}\n");
            TextBlock_Info.Inlines.Add($"\tMetadataURL:\t{Metadata.Instance.MetadataURL}\n");
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.ToString());
        }
    }
}
