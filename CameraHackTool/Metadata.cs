using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
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

        // This is SUPER janky but I don't want to spend time implementing something better right now.
        // This function assumes you know what you're doing and is not going to sanity check anything.
        private long GetStaticAddressFromSig(Mem m, long address, int skip, bool baseOffset = false)
        {
            var read = m.readBytes(new UIntPtr((ulong)address), 8 + skip);
            var offset = BitConverter.ToInt32(read, skip);
            if (baseOffset)
                return m.theProc.MainModule.BaseAddress.ToInt64() + offset;
            return address + skip + offset + 4;
        }

        public void FetchOffsets(Process process)
        {
            var m = MemoryManager.Instance.MemLib;
            m.OpenProcess(process.Id);
            var start = m.theProc.MainModule.BaseAddress.ToInt64();
            var end = start + m.theProc.MainModule.ModuleMemorySize;

            // Shorthand function even though I could just make the entire function in this scope I decided not to.
            string GSAFS(string signature, int skip, int adjust = 0, bool baseOffset = false)
            {
                var addr = m.AoBScan(start, end, signature).FirstOrDefault();
                if (addr == 0)
                    throw new Exception("Invalid address found!");

                return (GetStaticAddressFromSig(m, addr, skip, baseOffset) + adjust).ToString("X");
            }

            // Getting static addresses from assembly.
            MemoryManager.Instance.BaseAddress = GSAFS("88 91 ?? ?? ?? ?? 48 8D 3D ?? ?? ?? ??", 9, -8);
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
                            //this.CameraHeight = new MemoryAddressAndOffset(
                            //    int.Parse(element.Element("CameraHeight").Element("Address").Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                            //    int.Parse(element.Element("CameraHeight").Element("Offset").Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                            //);
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

        public MemoryAddressAndOffset CameraZoom { get; set; }
        public MemoryAddressAndOffset CameraFOV { get; set; }
        public MemoryAddressAndOffset CameraAngleX { get; set; }
        public MemoryAddressAndOffset CameraAngleY { get; set; }
        
        public string CameraHeightAddress { get; set; }
    }
}
