using Sharlayan;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using static CameraHackTool.Metadata;

namespace CameraHackTool
{
    public class Memory
    {
        public static MainWindow TheMainWindow = null;
        private static Dictionary<int, ThreadHandler> CurrentRunningProcesses { get; } = new Dictionary<int, ThreadHandler>();

        private static string GAS(params string[] args) => MemoryManager.GetAddressString(args);

        public static bool floatEquals(float a, float b, float epsilon)
        {
            const float floatNormal = (1 << 23) * float.Epsilon;
            float absA = Math.Abs(a);
            float absB = Math.Abs(b);
            float diff = Math.Abs(a - b);

            if (a == b)
            {
                // Shortcut, handles infinities
                return true;
            }

            if (a == 0.0f || b == 0.0f || diff < floatNormal)
            {
                // a or b is zero, or both are extremely close to it.
                // relative error is less meaningful here
                return diff < (epsilon * floatNormal);
            }

            // use relative error
            return diff / Math.Min((absA + absB), float.MaxValue) < epsilon;
        }

        public static void RunCameraHack(Process ffxivGame)
        {
            ffxivGame.Exited += GameExited;

            int pid = ffxivGame.Id;
            if (CurrentRunningProcesses.ContainsKey(pid))
            {
                // do nothing
                return;
            }

            CurrentRunningProcesses.Add(pid, new ThreadHandler());

            ThreadHandler th = CurrentRunningProcesses[pid];
            th.Process = ffxivGame;
            th.Handle = new Thread(new ParameterizedThreadStart(SpamMemoryWritesThread));
            th.Handle.Start(th);
        }

        private static void GameExited(object sender, EventArgs e)
        {
            Debug.WriteLine("Stopping thread, because the game was closed");
            StopCameraHack(sender as Process);
        }

        public static void StopCameraHack(Process ffxivGame)
        {
            ffxivGame.Exited -= GameExited;

            int pid = ffxivGame.Id;
            if (CurrentRunningProcesses.ContainsKey(pid))
            {
                CurrentRunningProcesses[pid].CloseAndJoinThread();
                CurrentRunningProcesses.Remove(pid);
            }
        }

        private static void SpamMemoryWritesThread(object handler)
        {
            try
            {
                Process.EnterDebugMode();
            }
            catch (Exception ex)
            {
                throw new Exception("Could not get debugging rights: " + ex.Message, ex);
            }

            var hProcess = IntPtr.Zero;
            try
            {
                hProcess = OpenProcess(ProcessFlags, false, (handler as ThreadHandler).Process.Id);
                if (hProcess == null)
                {
                    throw new Exception("Unable to OpenProcess");
                }

                // read the local params
                float cameraCurFov, cameraCurZoom, cameraAngleX, cameraAngleY, cameraHeight;

                ReadX64(DX11_CameraCurFOVAccess, hProcess, out cameraCurFov);
                ReadX64(DX11_CameraCurZoomAccess, hProcess, out cameraCurZoom);
                ReadX64(DX11_CameraAngleXAccess, hProcess, out cameraAngleX);
                ReadX64(DX11_CameraAngleYAccess, hProcess, out cameraAngleY);
                //ReadX64(DX11_CameraHeightAccess, hProcess, out cameraHeight);

                var m = MemoryManager.Instance.MemLib;
                cameraHeight = m.readFloat(Metadata.Instance.CameraHeightAddress);
                Console.WriteLine("current camera height = " + cameraHeight.ToString());

                // and i run
                // i run so far away
                while ((handler as ThreadHandler).shouldRun)
                {
                    try
                    {
                        ApplyX64(DX11_CameraCurFOVAccess, 0.01f, hProcess);
                        ApplyX64(DX11_CameraCurZoomAccess, 0.01f, hProcess);
                        ApplyX64(DX11_CameraAngleXAccess, 0.00f, hProcess);
                        ApplyX64(DX11_CameraAngleYAccess, 1.00f, hProcess);
                        //ApplyX64(DX11_CameraHeightAccess, 3000.0f, hProcess);

                        m.writeMemory(Metadata.Instance.CameraHeightAddress, "float", (3000.0f).ToString());

                        Thread.Sleep(100);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Something happened, probably best to just stop everything.");
                        // StopCameraHack((handler as ThreadHandler).Process);
                        TheMainWindow.RemoveProcessFromId((handler as ThreadHandler).Process.Id);

                        // we still have to do this
                        if (hProcess != IntPtr.Zero)
                        {
                            CloseHandle(hProcess);
                            hProcess = IntPtr.Zero;
                        }
                        break;
                    }
                }

                // we could have a null process here, so check for that
                // also, fuck threading
                if (hProcess != IntPtr.Zero)
                {
                    // reapply the params
                    ApplyX64(DX11_CameraCurFOVAccess, cameraCurFov, hProcess);
                    ApplyX64(DX11_CameraCurZoomAccess, cameraCurZoom, hProcess);
                    ApplyX64(DX11_CameraAngleXAccess, cameraAngleX, hProcess);
                    ApplyX64(DX11_CameraAngleYAccess, cameraAngleY, hProcess);
                    //ApplyX64(DX11_CameraHeightAccess, cameraHeight, hProcess);
                    m.writeMemory(Metadata.Instance.CameraHeightAddress, "float", cameraHeight.ToString());
                }
            }
            finally
            {
                if (hProcess != IntPtr.Zero)
                {
                    CloseHandle(hProcess);
                    hProcess = IntPtr.Zero;
                }
            }
        }

        private class ThreadHandler
        {
            public Process Process = null;
            public Thread Handle = null;
            public bool shouldRun = true;

            public void CloseAndJoinThread()
            {
                if (Handle != null)
                {
                    shouldRun = false;
                    if (Handle.ThreadState == System.Threading.ThreadState.Running)
                        Handle.Join();
                }
            }
        }

        private const ProcessAccessFlags ProcessFlags =
            ProcessAccessFlags.VirtualMemoryRead |
            ProcessAccessFlags.VirtualMemoryWrite |
            ProcessAccessFlags.VirtualMemoryOperation |
            ProcessAccessFlags.QueryInformation;

        private static MemoryAddressAndOffset DX11_CameraCurZoomAccess { get { return Metadata.Instance.CameraZoom; } }
        private static MemoryAddressAndOffset DX11_CameraCurFOVAccess { get { return Metadata.Instance.CameraFOV; } }
        private static MemoryAddressAndOffset DX11_CameraAngleXAccess { get { return Metadata.Instance.CameraAngleX; } }
        private static MemoryAddressAndOffset DX11_CameraAngleYAccess { get { return Metadata.Instance.CameraAngleY; } }

        public static string GetCharacterNameFromProcess(Process process)
        {
            string playerName = "(Unknown)";

            Metadata.Instance.FetchOffsets(process);
            var m = MemoryManager.Instance.MemLib;
            var addrBase = MemoryManager.Instance.BaseAddress;
            var currentBase = MemoryManager.Add(addrBase, (8).ToString("X"));

            playerName = m.readString(GAS(currentBase, "30"));

            Metadata.Instance.CameraHeightAddress = GAS(currentBase, "124");
            for (int skip = 30; skip < 500; skip += 1)
            {
                float cameraHeight = m.readFloat(GAS(currentBase, skip.ToString()));
                if (floatEquals(cameraHeight, 1.42069f, 0.0001f))
                    Console.WriteLine("height at skip: " + skip + " = " + cameraHeight);
            }

            return playerName;
        }

        private static void ApplyX64(MemoryAddressAndOffset data, float value, IntPtr hProcess)
        {
            var addr = GetAddress(8, hProcess, data.Address, data.Offset);
            Write(value, hProcess, addr);
        }

        private static void ReadX64(MemoryAddressAndOffset data, IntPtr hProcess, out float read)
        {
            var addr = GetAddress(8, hProcess, data.Address, data.Offset);
            Read(out read, hProcess, addr);
        }

        private static void Read(out float read_, IntPtr hProcess, IntPtr address)
        {
            var buffer = new byte[4];
            if (!ReadProcessMemory(hProcess, address, buffer, buffer.Length, out var read))
            {
                throw new Exception("Unable to read process memory: " + Marshal.GetLastWin32Error());
            }

            read_ = BitConverter.ToSingle(buffer, 0);
        }

        private static void Write(float value, IntPtr hProcess, IntPtr address)
        {
            var buffer = BitConverter.GetBytes(value);
            if (!WriteProcessMemory(hProcess, address, buffer, buffer.Length, out var written))
            {
                throw new Exception("Could not write process memory: " + Marshal.GetLastWin32Error());
            }
        }

        private static IntPtr GetAddress(int size, IntPtr hProcess, int offset, int finalOffset)
        {
            var addr = GetBaseAddress(hProcess);
            var buffer = new byte[size];
            if (!ReadProcessMemory(hProcess, IntPtr.Add(addr, offset), buffer, buffer.Length, out var read))
            {
                throw new Exception("Unable to read process memory");
            }
            addr = (size == 8)
                ? new IntPtr(BitConverter.ToInt64(buffer, 0))
                : new IntPtr(BitConverter.ToInt32(buffer, 0));
            return IntPtr.Add(addr, finalOffset);
        }

        private static IntPtr GetBaseAddress(IntPtr hProcess)
        {
            var hModules = new IntPtr[1024];
            var uiSize = (uint)(Marshal.SizeOf(typeof(IntPtr)) * hModules.Length);
            var gch = GCHandle.Alloc(hModules, GCHandleType.Pinned);
            try
            {
                var pModules = gch.AddrOfPinnedObject();
                if (EnumProcessModules(hProcess, pModules, uiSize, out var cbNeeded) != 1)
                {
                    throw new Exception("Could not enumerate modules: " + Marshal.GetLastWin32Error());
                }

                var mainModule = IntPtr.Zero;
                var modulesLoaded = (int)(cbNeeded / Marshal.SizeOf(typeof(IntPtr)));
                for (var i = 0; i < modulesLoaded; i++)
                {
                    var moduleFilenameBuilder = new StringBuilder(1024);
                    if (GetModuleFileNameEx(hProcess, hModules[i], moduleFilenameBuilder, moduleFilenameBuilder.Capacity) == 0)
                    {
                        throw new Exception("Could not get module filename: " + Marshal.GetLastWin32Error());
                    }

                    var moduleFilename = moduleFilenameBuilder.ToString();
                    if (!string.IsNullOrEmpty(moduleFilename) && moduleFilename.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        mainModule = hModules[i];
                        break;
                    }
                }

                if (mainModule == IntPtr.Zero)
                {
                    throw new Exception("Could not find module for executable");
                }

                if (!GetModuleInformation(hProcess, mainModule, out var moduleInfo, (uint)Marshal.SizeOf<ModuleInfo>()))
                {
                    throw new Exception("Could not get module information from process" + Marshal.GetLastWin32Error());
                }

                return moduleInfo.lpBaseOfDll;
            }
            finally
            {
                gch.Free();
            }
        }

        #region Windows imports
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);
        
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr processHandle, IntPtr lpBaseAddress, [In][Out] byte[] lpBuffer, IntPtr regionSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool GetModuleInformation(IntPtr hProcess, IntPtr hModule, out ModuleInfo lpmodinfo, uint cb);

        [DllImport("psapi.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern int EnumProcessModules(IntPtr hProcess, [Out] IntPtr lphModule, uint cb, out uint lpcbNeeded);

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern int GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, StringBuilder lpFilename, int nSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct ModuleInfo
        {
            public IntPtr lpBaseOfDll;
            public uint SizeOfImage;
            public IntPtr EntryPoint;
        }

        [Flags]
        private enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }
        #endregion
    }
}
