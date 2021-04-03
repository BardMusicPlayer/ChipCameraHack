using System;
using System.Globalization;
using System.Windows;
using System.Xml.Linq;

namespace CameraHackTool
{
    public class Metadata
    {
        public static Metadata Instance { get { return __arbitur__.Value; } }
        private static readonly Lazy<Metadata> __arbitur__ = new Lazy<Metadata>(() => new Metadata());

        private string UpdateDLUrl = "";
        private string MetadataUrl = "";

        public string LocalVersionString { get; private set; }
        public string NewerVersionString { get; private set; }

        protected Metadata()
        {
            this.LocalVersionString = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        public bool grabApplicationMetadata(string assemblyTitle)
        {
            string finalUrl = MetadataUrl + assemblyTitle;
            
            try
            {
                XDocument xmlf = XDocument.Load(finalUrl);
                var root = xmlf.Element("Root");

                foreach (var element in root.Elements())
                {
                    switch (element.Name.LocalName)
                    {
                        case "AppVersion":
                            this.NewerVersionString = element.Value;
                            break;
                        case "DownloadLink":
                            this.UpdateDLUrl = element.Value;
                            break;
                        case "AppMetadata":
                            this.CameraZoom = new MemoryAddressAndOffset(
                                int.Parse(element.Element("CameraZoom").Element("Address").Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                                int.Parse(element.Element("CameraZoom").Element("Offset").Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                            );
                            this.CameraFOV = new MemoryAddressAndOffset(
                                int.Parse(element.Element("CameraFOV").Element("Address").Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                                int.Parse(element.Element("CameraFOV").Element("Offset").Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                            );
                            this.CameraAngleX = new MemoryAddressAndOffset(
                                int.Parse(element.Element("CameraAngleX").Element("Address").Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                                int.Parse(element.Element("CameraAngleX").Element("Offset").Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                            );
                            this.CameraAngleY = new MemoryAddressAndOffset(
                                int.Parse(element.Element("CameraAngleY").Element("Address").Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                                int.Parse(element.Element("CameraAngleY").Element("Offset").Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                            );

                            this.PlayerNameOffset = int.Parse(element.Element("PlayerNameOffset").Value, NumberStyles.Integer, CultureInfo.InvariantCulture).ToString();
                            this.CameraHeightOffset = int.Parse(element.Element("CameraHeightOffset").Value, NumberStyles.Integer, CultureInfo.InvariantCulture).ToString();
                            break;
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(
                    "Unable to read offsets from server. Reason: " + e.Message + "\n\n" + e.StackTrace,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error
                );
            }

            return false;
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

        public MemoryAddressAndOffset CameraZoom { get; private set; }
        public MemoryAddressAndOffset CameraFOV { get; private set; }
        public MemoryAddressAndOffset CameraAngleX { get; private set; }
        public MemoryAddressAndOffset CameraAngleY { get; private set; }

        public string PlayerNameOffset { get; private set; }
        public string CameraHeightOffset { get; private set; }
    }
}
