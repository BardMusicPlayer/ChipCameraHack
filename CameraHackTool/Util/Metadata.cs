using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Xml.Linq;

namespace CameraHackTool
{
    public class Metadata
    {
        public static Metadata Instance { get { return __arbitur__.Value; } }
        private static readonly Lazy<Metadata> __arbitur__ = new Lazy<Metadata>(() => new Metadata());

#if DEBUG
        public string MetadataURL = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, "Resources/AddressAndOffsetMetadata.xml");
#else
        public string MetadataURL = "https://raw.githubusercontent.com/BardMusicPlayer/ChipCameraHack/main/CameraHackTool/Resources/AddressAndOffsetMetadata.xml";
#endif

        public enum MetadataResult
        {
            Success,
            UpdateAvailable,
            RunningBeta,
            Failure
        }

        public enum GameRegion
        {
            LV = 0,
            KR = 1,
            CN = 2,
        }

        protected Metadata()
        {
            this.LocalVersion = Assembly.GetExecutingAssembly().GetName().Version;
        }

        public MetadataResult grabApplicationMetadata()
        {
            try
            {
                XDocument xmlf = XDocument.Load(MetadataURL);
                var root = xmlf.Element("Root");

                foreach (var element in root.Elements())
                {
                    switch (element.Name.LocalName)
                    {
                        case "AppVersion":
                            this.NewerVersion = new Version(element.Value);
                            break;
                        case "DownloadLink":
                            this.DownloadURL = element.Value;
                            break;
                        case "AppMetadata":
                            foreach (var region in Enum.GetValues(typeof(GameRegion)))
                            {
                                string rs = region.ToString();
                                var cElem = element.Element(rs);
                                var oElem = cElem.Element("Offsets");
                                int address = int.Parse(cElem.Element("Address").Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

                                CameraZoomData[rs] = new MemoryAddressAndOffset(
                                    address,
                                    int.Parse(oElem.Element("CameraZoom").Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                                );
                                CameraFOVData[rs] = new MemoryAddressAndOffset(
                                    address,
                                    int.Parse(oElem.Element("CameraFOV").Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                                );
                                CameraAngleXData[rs] = new MemoryAddressAndOffset(
                                    address,
                                    int.Parse(oElem.Element("CameraAngleX").Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                                );
                                CameraAngleYData[rs] = new MemoryAddressAndOffset(
                                    address,
                                    int.Parse(oElem.Element("CameraAngleY").Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                                );
                                PlayerNameData[rs] = int.Parse(oElem.Element("PlayerName").Value, NumberStyles.Integer, CultureInfo.InvariantCulture).ToString();
                                CameraHeightData[rs] = int.Parse(oElem.Element("CameraHeight").Value, NumberStyles.Integer, CultureInfo.InvariantCulture).ToString();
                            }
                            break;
                    }
                }

                // -1 means earlier than
                if (this.LocalVersion.CompareTo(this.NewerVersion) == -1)
                {
                    return MetadataResult.UpdateAvailable;
                }
                // 1 means later than
                else if (this.LocalVersion.CompareTo(this.NewerVersion) == 1)
                {
                    return MetadataResult.RunningBeta;
                }

                return MetadataResult.Success;
            }
            catch (Exception e)
            {
                MessageBox.Show(
                    "Unable to read offsets from server. Reason: " + e.Message + "\n\n" + e.StackTrace,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error
                );
            }

            return MetadataResult.Failure;
        }

        private static bool IsValidGamePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            if (!Directory.Exists(path))
                return false;

            if (File.Exists(Path.Combine(path, "game", "ffxivgame.ver")))
                return File.Exists(Path.Combine(path, "game", "ffxivgame.ver"));

            return false;
        }

        public void InitializeToRegionFromGamePath(string gamePath)
        {
            // find game region from static files
            this.LocalRegion = GameRegion.LV;
            string gameDirectory = Path.GetFullPath(Path.Combine(gamePath, "..", "..")).ToString();
            if (!IsValidGamePath(gameDirectory))
            {
                MessageBox.Show(
                    $"Please make sure ffxivgame.ver is in \n\n{gameDirectory}/game directory.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error
                );
                return;
            }

            if (File.Exists(Path.Combine(gameDirectory, "FFXIVBoot.exe")) || File.Exists(Path.Combine(gameDirectory, "rail_files", "rail_game_identify.json")))
            {
                this.LocalRegion = GameRegion.CN;
            }
            else if (File.Exists(Path.Combine(gameDirectory, "boot", "FFXIV_Boot.exe")))
            {
                this.LocalRegion = GameRegion.KR;
            }
        }

        public class MemoryAddressAndOffset
        {
            public readonly int Address;
            public readonly int Offset;

            public MemoryAddressAndOffset(int addr, int off)
            {
                this.Address = addr;
                this.Offset = off;
            }
        }

        public Version LocalVersion { get; private set; }
        public Version NewerVersion { get; private set; }
        public string DownloadURL { get; private set; }
        public MemoryAddressAndOffset CameraZoom => CameraZoomData[this.LocalRegion.ToString()];
        public MemoryAddressAndOffset CameraFOV => CameraFOVData[this.LocalRegion.ToString()];
        public MemoryAddressAndOffset CameraAngleX => CameraAngleXData[this.LocalRegion.ToString()];
        public MemoryAddressAndOffset CameraAngleY => CameraAngleYData[this.LocalRegion.ToString()];
        public string PlayerNameOffset => PlayerNameData[this.LocalRegion.ToString()];
        public string CameraHeightOffset => CameraHeightData[this.LocalRegion.ToString()];

        // region specific addresses
        public GameRegion LocalRegion;
        private Dictionary<string, MemoryAddressAndOffset> CameraZoomData = new Dictionary<string, MemoryAddressAndOffset>();
        private Dictionary<string, MemoryAddressAndOffset> CameraFOVData = new Dictionary<string, MemoryAddressAndOffset>();
        private Dictionary<string, MemoryAddressAndOffset> CameraAngleXData = new Dictionary<string, MemoryAddressAndOffset>();
        private Dictionary<string, MemoryAddressAndOffset> CameraAngleYData = new Dictionary<string, MemoryAddressAndOffset>();
        private Dictionary<string, string> PlayerNameData = new Dictionary<string, string>();
        private Dictionary<string, string> CameraHeightData = new Dictionary<string, string>();
    }
}
