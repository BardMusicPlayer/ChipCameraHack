﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CameraHackTool
{
    struct MemoryRegionResult
    {
        public UIntPtr CurrentBaseAddress { get; set; }
        public long RegionSize { get; set; }
        public UIntPtr RegionBase { get; set; }

    }
    public class Mem
    {
        #region DllImports
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(
            UInt32 dwDesiredAccess,
            bool bInheritHandle,
            Int32 dwProcessId
            );

#if WINXP
#else
        [DllImport("kernel32.dll", EntryPoint = "VirtualQueryEx")]
        public static extern UIntPtr Native_VirtualQueryEx(IntPtr hProcess, UIntPtr lpAddress,
            out MEMORY_BASIC_INFORMATION32 lpBuffer, UIntPtr dwLength);

        [DllImport("kernel32.dll", EntryPoint = "VirtualQueryEx")]
        public static extern UIntPtr Native_VirtualQueryEx(IntPtr hProcess, UIntPtr lpAddress,
            out MEMORY_BASIC_INFORMATION64 lpBuffer, UIntPtr dwLength);

        [DllImport("kernel32.dll")]
        static extern uint GetLastError();

        public UIntPtr VirtualQueryEx(IntPtr hProcess, UIntPtr lpAddress,
            out MEMORY_BASIC_INFORMATION lpBuffer)
        {
            UIntPtr retVal;

            // TODO: Need to change this to only check once.
            if (Is64Bit || IntPtr.Size == 8)
            {
                // 64 bit
                MEMORY_BASIC_INFORMATION64 tmp64 = new MEMORY_BASIC_INFORMATION64();
                retVal = Native_VirtualQueryEx(hProcess, lpAddress, out tmp64, new UIntPtr((uint)Marshal.SizeOf(tmp64)));

                lpBuffer.BaseAddress = tmp64.BaseAddress;
                lpBuffer.AllocationBase = tmp64.AllocationBase;
                lpBuffer.AllocationProtect = tmp64.AllocationProtect;
                lpBuffer.RegionSize = (long)tmp64.RegionSize;
                lpBuffer.State = tmp64.State;
                lpBuffer.Protect = tmp64.Protect;
                lpBuffer.Type = tmp64.Type;

                return retVal;
            }

            MEMORY_BASIC_INFORMATION32 tmp32 = new MEMORY_BASIC_INFORMATION32();

            retVal = Native_VirtualQueryEx(hProcess, lpAddress, out tmp32, new UIntPtr((uint)Marshal.SizeOf(tmp32)));

            lpBuffer.BaseAddress = tmp32.BaseAddress;
            lpBuffer.AllocationBase = tmp32.AllocationBase;
            lpBuffer.AllocationProtect = tmp32.AllocationProtect;
            lpBuffer.RegionSize = tmp32.RegionSize;
            lpBuffer.State = tmp32.State;
            lpBuffer.Protect = tmp32.Protect;
            lpBuffer.Type = tmp32.Type;

            return retVal;
        }

        [DllImport("kernel32.dll")]
        static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);
#endif

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);

        [DllImport("dbghelp.dll")]
        static extern bool MiniDumpWriteDump(
            IntPtr hProcess,
            Int32 ProcessId,
            IntPtr hFile,
            MINIDUMP_TYPE DumpType,
            IntPtr ExceptionParam,
            IntPtr UserStreamParam,
            IntPtr CallackParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr w, IntPtr l);

        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(
            IntPtr hProcess,
            UIntPtr lpBaseAddress,
            string lpBuffer,
            UIntPtr nSize,
            out IntPtr lpNumberOfBytesWritten
        );

        [DllImport("kernel32.dll")]
        static extern int GetProcessId(IntPtr handle);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern uint GetPrivateProfileString(
           string lpAppName,
           string lpKeyName,
           string lpDefault,
           StringBuilder lpReturnedString,
           uint nSize,
           string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool VirtualFreeEx(
            IntPtr hProcess,
            UIntPtr lpAddress,
            UIntPtr dwSize,
            uint dwFreeType
            );

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] byte[] lpBuffer, UIntPtr nSize, IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] byte[] lpBuffer, UIntPtr nSize, out ulong lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern UIntPtr VirtualAllocEx(
            IntPtr hProcess,
            UIntPtr lpAddress,
            uint dwSize,
            uint flAllocationType,
            uint flProtect
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
        public static extern UIntPtr GetProcAddress(
            IntPtr hModule,
            string procName
        );

        [DllImport("kernel32.dll", EntryPoint = "CloseHandle")]
        private static extern bool _CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        public static extern Int32 CloseHandle(
        IntPtr hObject
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(
            string lpModuleName
        );

        [DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
        internal static extern Int32 WaitForSingleObject(
            IntPtr handle,
            Int32 milliseconds
        );

        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, byte[] lpBuffer, UIntPtr nSize, IntPtr lpNumberOfBytesWritten);

        // Added to avoid casting to UIntPtr
        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, byte[] lpBuffer, UIntPtr nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32")]
        public static extern IntPtr CreateRemoteThread(
          IntPtr hProcess,
          IntPtr lpThreadAttributes,
          uint dwStackSize,
          UIntPtr lpStartAddress, // raw Pointer into remote process  
          UIntPtr lpParameter,
          uint dwCreationFlags,
          out IntPtr lpThreadId
        );

        [DllImport("kernel32")]
        public static extern bool IsWow64Process(IntPtr hProcess, out bool lpSystemInfo);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        // privileges
        const int PROCESS_CREATE_THREAD = 0x0002;
        const int PROCESS_QUERY_INFORMATION = 0x0400;
        const int PROCESS_VM_OPERATION = 0x0008;
        const int PROCESS_VM_WRITE = 0x0020;
        const int PROCESS_VM_READ = 0x0010;

        // used for memory allocation
        const uint MEM_FREE = 0x10000;
        const uint MEM_COMMIT = 0x00001000;
        const uint MEM_RESERVE = 0x00002000;

        const uint PAGE_READWRITE = 0x04;
        const uint PAGE_WRITECOPY = 0x08;
        private const uint PAGE_EXECUTE_READWRITE = 0x40;
        private const uint PAGE_EXECUTE_WRITECOPY = 0x80;
        private const uint PAGE_EXECUTE = 0x10;
        private const uint PAGE_EXECUTE_READ = 0x20;

        private const uint PAGE_GUARD = 0x100;
        private const uint PAGE_NOACCESS = 0x01;

        private uint MEM_PRIVATE = 0x20000;
        private uint MEM_IMAGE = 0x1000000;

        #endregion

        /// <summary>
        /// The process handle that was opened. (Use OpenProcess function to populate this variable)
        /// </summary>
        public IntPtr pHandle;

        public Process theProc = null;

        internal enum MINIDUMP_TYPE
        {
            MiniDumpNormal = 0x00000000,
            MiniDumpWithDataSegs = 0x00000001,
            MiniDumpWithFullMemory = 0x00000002,
            MiniDumpWithHandleData = 0x00000004,
            MiniDumpFilterMemory = 0x00000008,
            MiniDumpScanMemory = 0x00000010,
            MiniDumpWithUnloadedModules = 0x00000020,
            MiniDumpWithIndirectlyReferencedMemory = 0x00000040,
            MiniDumpFilterModulePaths = 0x00000080,
            MiniDumpWithProcessThreadData = 0x00000100,
            MiniDumpWithPrivateReadWriteMemory = 0x00000200,
            MiniDumpWithoutOptionalData = 0x00000400,
            MiniDumpWithFullMemoryInfo = 0x00000800,
            MiniDumpWithThreadInfo = 0x00001000,
            MiniDumpWithCodeSegs = 0x00002000
        }

        bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (c < '0' || c > '9')
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Open the PC game process with all security and access rights.
        /// </summary>
        /// <param name="proc">Use process name or process ID here.</param>
        /// <returns></returns>
        public bool OpenProcess(int pid)
        {
            if (!isAdmin())
            {
                Debug.WriteLine("WARNING: You are NOT running this program as admin! Visit https://github.com/erfg12/memory.dll/wiki/Administrative-Privileges");
                MessageBox.Show("WARNING: You are NOT running this program as admin!");
            }

            try
            {
                if (theProc != null && theProc.Id == pid)
                    return true;

                if (pid <= 0)
                {
                    Debug.WriteLine("ERROR: OpenProcess given proc ID 0.");
                    return false;
                }

                theProc = Process.GetProcessById(pid);

                if (theProc != null && !theProc.Responding)
                {
                    Debug.WriteLine("ERROR: OpenProcess: Process is not responding or null.");
                    return false;
                }
                Process.EnterDebugMode();
                pHandle = OpenProcess(0x1F0FFF, true, pid);

                if (pHandle == IntPtr.Zero)
                {
                    var eCode = Marshal.GetLastWin32Error();
                }

                mainModule = theProc.MainModule;

                getModules();

                // Lets set the process to 64bit or not here (cuts down on api calls)
                Is64Bit = Environment.Is64BitOperatingSystem && (IsWow64Process(pHandle, out bool retVal) && !retVal);

                Debug.WriteLine("Program is operating at Administrative level. Process #" + theProc + " is open and modules are stored.");

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "OpenProcess Failed");
                Debug.WriteLine("ERROR: OpenProcess has crashed.");
                return false;
            }
        }

        #region CheckSeDebugPrivilege
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool OpenProcessToken(
            IntPtr ProcessHandle,
            UInt32 DesiredAccess,
            out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, ref LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool PrivilegeCheck(
            IntPtr ClientToken,
            ref PRIVILEGE_SET RequiredPrivileges,
            out bool pfResult);

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID
        {
            public UInt32 LowPart;
            public Int32 HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PRIVILEGE_SET
        {
            public UInt32 PrivilegeCount;
            public UInt32 Control;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public LUID_AND_ATTRIBUTES[] Privilege;
        }
        public struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public UInt32 Attributes;
        }

        private static int CheckSeDebugPrivilege(out bool IsDebugEnabled)
        {
            IsDebugEnabled = false;

            if (!OpenProcessToken(GetCurrentProcess(), 0x8 /*TOKEN_QUERY*/, out var TokenHandle))
                return Marshal.GetLastWin32Error();

            var luidDebugPrivilege = new LUID();
            if (!LookupPrivilegeValue(null, "SeDebugPrivilege", ref luidDebugPrivilege))
                return Marshal.GetLastWin32Error();

            var RequiredPrivileges = new PRIVILEGE_SET
            {
                PrivilegeCount = 1,
                Control = 1 /* PRIVILEGE_SET_ALL_NECESSARY */,
                Privilege = new LUID_AND_ATTRIBUTES[1]
            };

            RequiredPrivileges.Privilege[0].Luid = luidDebugPrivilege;
            RequiredPrivileges.Privilege[0].Attributes = 2 /* SE_PRIVILEGE_ENABLED */;

            if (!PrivilegeCheck(TokenHandle, ref RequiredPrivileges, out var bResult))
                return Marshal.GetLastWin32Error();

            // bResult == true => SeDebugPrivilege is on; otherwise it's off
            IsDebugEnabled = bResult;

            CloseHandle(TokenHandle);

            return 0;
        }
        #endregion


        /// <summary>
        /// Open the PC game process with all security and access rights.
        /// </summary>
        /// <param name="proc">Use process name or process ID here.</param>
        /// <returns></returns>
        public bool OpenProcess(string proc)
        {
            return OpenProcess(getProcIDFromName(proc));
        }

        /// <summary>
        /// Check if program is running with administrative privileges. Read about it here: https://github.com/erfg12/memory.dll/wiki/Administrative-Privileges
        /// </summary>
        /// <returns></returns>
        public bool isAdmin()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        /// <summary>
        /// Check if opened process is 64bit. Used primarily for get64bitCode().
        /// </summary>
        /// <returns>True if 64bit false if 32bit.</returns>
        public bool is64bit()
        {
            return Is64Bit;
        }

        private bool _is64Bit;
        public bool Is64Bit
        {
            get { return _is64Bit; }
            private set { _is64Bit = value; }
        }


        /// <summary>
        /// Builds the process modules dictionary (names with addresses).
        /// </summary>
        public void getModules()
        {
            if (theProc == null)
                return;

            modules.Clear();
            foreach (ProcessModule Module in theProc.Modules)
            {
                if (!string.IsNullOrEmpty(Module.ModuleName) && !modules.ContainsKey(Module.ModuleName))
                    modules.Add(Module.ModuleName, Module.BaseAddress);
            }
        }

        public void setFocus()
        {
            //int style = GetWindowLong(procs.MainWindowHandle, -16);
            //if ((style & 0x20000000) == 0x20000000) //minimized
            //    SendMessage(procs.Handle, 0x0112, (IntPtr)0xF120, IntPtr.Zero);
            SetForegroundWindow(theProc.MainWindowHandle);
        }

        /// <summary>
        /// Get the process ID number by process name.
        /// </summary>
        /// <param name="name">Example: "eqgame". Use task manager to find the name. Do not include .exe</param>
        /// <returns></returns>
        public int getProcIDFromName(string name) //new 1.0.2 function
        {
            Process[] processlist = Process.GetProcesses();

            if (name.Contains(".exe"))
                name = name.Replace(".exe", "");

            foreach (Process theprocess in processlist)
            {
                if (theprocess.ProcessName.Equals(name, StringComparison.CurrentCultureIgnoreCase)) //find (name).exe in the process list (use task manager to find the name)
                    return theprocess.Id;
            }

            return 0; //if we fail to find it
        }

        /// <summary>
        /// Convert a byte array to a literal string
        /// </summary>
        /// <param name="buffer">Byte array to convert to byte string</param>
        /// <returns></returns>
        public string byteArrayToString(byte[] buffer)
        {
            StringBuilder build = new StringBuilder();
            int i = 1;
            foreach (byte b in buffer)
            {
                build.Append(String.Format("0x{0:X}", b));
                if (i < buffer.Count())
                    build.Append(" ");
                i++;
            }
            return build.ToString();
        }

        /// <summary>
        /// Get code from ini file.
        /// </summary>
        /// <param name="name">label for address or code</param>
        /// <param name="file">path and name of ini file</param>
        /// <returns></returns>
        public string LoadCode(string name, string file)
        {
            StringBuilder returnCode = new StringBuilder(1024);
            uint read_ini_result;

            if (file != "")
                read_ini_result = GetPrivateProfileString("codes", name, "", returnCode, (uint)returnCode.Capacity, file);
            else
                returnCode.Append(name);

            return returnCode.ToString();
        }

        private int LoadIntCode(string name, string path)
        {
            try
            {
                int intValue = Convert.ToInt32(LoadCode(name, path), 16);
                if (intValue >= 0)
                    return intValue;
                else
                    return 0;
            }
            catch
            {
                Debug.WriteLine("ERROR: LoadIntCode function crashed!");
                return 0;
            }
        }

        /// <summary>
        /// Dictionary with our opened process module names with addresses.
        /// </summary>
        public Dictionary<string, IntPtr> modules = new Dictionary<string, IntPtr>();

        /// <summary>
        /// Make a named pipe (if not already made) and call to a remote function.
        /// </summary>
        /// <param name="func">remote function to call</param>
        /// <param name="name">name of the thread</param>
        public void ThreadStartClient(string func, string name)
        {
            //ManualResetEvent SyncClientServer = (ManualResetEvent)obj;
            using (NamedPipeClientStream pipeStream = new NamedPipeClientStream(name))
            {
                if (!pipeStream.IsConnected)
                    pipeStream.Connect();

                //MessageBox.Show("[Client] Pipe connection established");
                using (StreamWriter sw = new StreamWriter(pipeStream))
                {
                    if (!sw.AutoFlush)
                        sw.AutoFlush = true;
                    sw.WriteLine(func);
                }
            }
        }

        private ProcessModule mainModule;

        /// <summary>
        /// Cut a string that goes on for too long or one that is possibly merged with another string.
        /// </summary>
        /// <param name="str">The string you want to cut.</param>
        /// <returns></returns>
        public string CutString(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if (c >= ' ' && c <= '~')
                    sb.Append(c);
                else
                    break;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Clean up a string that has bad characters in it.
        /// </summary>
        /// <param name="str">The string you want to sanitize.</param>
        /// <returns></returns>
        public string sanitizeString(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if (c >= ' ' && c <= '~')
                    sb.Append(c);
            }
            return sb.ToString();
        }

        #region readMemory
        /// <summary>
        /// Reads up to `length ` bytes from an address.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="length">The maximum bytes to read.</param>
        /// <param name="file">path and name of ini file.</param>
        /// <returns>The bytes read or null</returns>
        public byte[] readBytes(string code, long length, string file = "")
        {
            byte[] memory = new byte[length];
            UIntPtr theCode = get64bitCode(code, file);

            if (!ReadProcessMemory(pHandle, theCode, memory, (UIntPtr)length, IntPtr.Zero))
            {
                Array.Clear(memory, 0, memory.Length);
                return memory;
            }

            return memory;
        }

        /// <summary>
        /// Reads up to `length ` bytes from an address.
        /// </summary>
        /// <param name="length">The maximum bytes to read.</param>
        /// <param name="file">path and name of ini file.</param>
        /// <returns>The bytes read or null</returns>
        public byte[] readBytes(UIntPtr address, long length, string file = "")
        {
            byte[] memory = new byte[length];

            if (!ReadProcessMemory(pHandle, address, memory, (UIntPtr)length, IntPtr.Zero))
            {
                Array.Clear(memory, 0, memory.Length);
                return memory;
            }

            return memory;
        }

        /// <summary>
        /// Read a float value from an address.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="file">path and name of ini file. (OPTIONAL)</param>
        /// <param name="round">Round the value to 2 decimal places</param>
        /// <returns></returns>
        public float readFloat(string code, string file = "", bool round = false)
        {
            byte[] memory = new byte[4];

            UIntPtr theCode;
            theCode = get64bitCode(code, file);
            try
            {
                if (ReadProcessMemory(pHandle, theCode, memory, (UIntPtr)4, IntPtr.Zero))
                {
                    float address = BitConverter.ToSingle(memory, 0);
                    float returnValue = (float)address;
                    if (round)
                        returnValue = (float)Math.Round(address, 2);
                    return returnValue;
                }
                else
                    return 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Read a string value from an address.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="file">path and name of ini file. (OPTIONAL)</param>
        /// <param name="length">length of bytes to read (OPTIONAL)</param>
        /// <param name="zeroTerminated">terminate string at null char</param>
        /// <returns></returns>
        public string readString(string code, string file = "", int length = 32, bool zeroTerminated = true)
        {
            byte[] memoryNormal = new byte[length];
            UIntPtr theCode;
            theCode = get64bitCode(code, file);
            if (ReadProcessMemory(pHandle, theCode, memoryNormal, (UIntPtr)length, IntPtr.Zero))
                return (zeroTerminated) ? Encoding.UTF8.GetString(memoryNormal).Split('\0')[0] : Encoding.UTF8.GetString(memoryNormal);
            else
                return "";
        }

        /// <summary>
        /// Read a double value
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="file">path and name of ini file. (OPTIONAL)</param>
        /// <param name="round">Round the value to 2 decimal places</param>
        /// <returns></returns>
        public double readDouble(string code, string file = "", bool round = true)
        {
            byte[] memory = new byte[8];

            UIntPtr theCode;
            theCode = get64bitCode(code, file);
            try
            {
                if (ReadProcessMemory(pHandle, theCode, memory, (UIntPtr)8, IntPtr.Zero))
                {
                    double address = BitConverter.ToDouble(memory, 0);
                    double returnValue = (double)address;
                    if (round)
                        returnValue = (double)Math.Round(address, 2);
                    return returnValue;
                }
                else
                    return 0;
            }
            catch
            {
                return 0;
            }
        }

        public int readUIntPtr(UIntPtr code)
        {
            byte[] memory = new byte[4];
            if (ReadProcessMemory(pHandle, code, memory, (UIntPtr)4, IntPtr.Zero))
                return BitConverter.ToInt32(memory, 0);
            else
                return 0;
        }

        /// <summary>
        /// Read an integer from an address.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="file">path and name of ini file. (OPTIONAL)</param>
        /// <returns></returns>
        public int readInt(string code, string file = "")
        {
            byte[] memory = new byte[4];
            UIntPtr theCode;
            theCode = get64bitCode(code, file);
            try
            {
                if (ReadProcessMemory(pHandle, theCode, memory, (UIntPtr)4, IntPtr.Zero))
                    return BitConverter.ToInt32(memory, 0);
                else
                    return 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Read a long value from an address.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="file">path and name of ini file. (OPTIONAL)</param>
        /// <returns></returns>
        public long readLong(string code, string file = "")
        {
            byte[] memory = new byte[16];
            UIntPtr theCode;

            theCode = get64bitCode(code, file);
            try
            {
                if (ReadProcessMemory(pHandle, theCode, memory, (UIntPtr)16, IntPtr.Zero))
                    return BitConverter.ToInt64(memory, 0);
                else
                    return 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Read a UInt value from address.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="file">path and name of ini file. (OPTIONAL)</param>
        /// <returns></returns>
        public UInt64 readUInt(string code, string file = "")
        {
            byte[] memory = new byte[8];
            UIntPtr theCode;
            theCode = get64bitCode(code, file);
            try
            {
                if (ReadProcessMemory(pHandle, theCode, memory, (UIntPtr)8, IntPtr.Zero))
                    return BitConverter.ToUInt64(memory, 0);
                else
                    return 0;
            }
            catch
            {
                return 0;
            }

        }

        /// <summary>
        /// Reads a 2 byte value from an address and moves the address.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="moveQty">Quantity to move.</param>
        /// <param name="file">path and name of ini file (OPTIONAL)</param>
        /// <returns></returns>
        public int read2ByteMove(string code, int moveQty, string file = "")
        {
            byte[] memory = new byte[4];
            UIntPtr theCode;
            theCode = get64bitCode(code, file);

            UIntPtr newCode = UIntPtr.Add(theCode, moveQty);

            if (ReadProcessMemory(pHandle, newCode, memory, (UIntPtr)2, IntPtr.Zero))
                return BitConverter.ToInt32(memory, 0);
            else
                return 0;
        }

        /// <summary>
        /// Reads an integer value from address and moves the address.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="moveQty">Quantity to move.</param>
        /// <param name="file">path and name of ini file (OPTIONAL)</param>
        /// <returns></returns>
        public int readIntMove(string code, int moveQty, string file = "")
        {
            byte[] memory = new byte[4];
            UIntPtr theCode;
            theCode = get64bitCode(code, file);

            UIntPtr newCode = UIntPtr.Add(theCode, moveQty);

            if (ReadProcessMemory(pHandle, newCode, memory, (UIntPtr)4, IntPtr.Zero))
                return BitConverter.ToInt32(memory, 0);
            else
                return 0;
        }

        /// <summary>
        /// Get UInt and move to another address by moveQty. Use in a for loop.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="moveQty">Quantity to move.</param>
        /// <param name="file">path and name of ini file (OPTIONAL)</param>
        /// <returns></returns>
        public ulong readUIntMove(string code, int moveQty, string file = "")
        {
            byte[] memory = new byte[8];
            UIntPtr theCode;
            theCode = get64bitCode(code, file, 8);

            UIntPtr newCode = UIntPtr.Add(theCode, moveQty);

            if (ReadProcessMemory(pHandle, newCode, memory, (UIntPtr)8, IntPtr.Zero))
                return BitConverter.ToUInt64(memory, 0);
            else
                return 0;
        }

        /// <summary>
        /// Read a 2 byte value from an address. Returns an integer.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="file">path and file name to ini file. (OPTIONAL)</param>
        /// <returns></returns>
        public int read2Byte(string code, string file = "")
        {
            byte[] memoryTiny = new byte[4];

            UIntPtr theCode;
            theCode = get64bitCode(code, file);

            try
            {
                if (ReadProcessMemory(pHandle, theCode, memoryTiny, (UIntPtr)2, IntPtr.Zero))
                    return BitConverter.ToInt32(memoryTiny, 0);
                else
                    return 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Read 1 byte from address.
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        /// <param name="file">path and file name of ini file. (OPTIONAL)</param>
        /// <returns></returns>
        public int readByte(string code, string file = "")
        {
            byte[] memoryTiny = new byte[4];

            UIntPtr theCode;
            theCode = get64bitCode(code, file);
            try
            {
                if (ReadProcessMemory(pHandle, theCode, memoryTiny, (UIntPtr)1, IntPtr.Zero))
                    return BitConverter.ToInt32(memoryTiny, 0);
                else
                    return 0;
            }
            catch
            {
                return 0;
            }
        }

        public int readPByte(UIntPtr address, string code, string file = "")
        {
            byte[] memory = new byte[4];
            if (ReadProcessMemory(pHandle, address + LoadIntCode(code, file), memory, (UIntPtr)1, IntPtr.Zero))
                return BitConverter.ToInt32(memory, 0);
            else
                return 0;
        }

        public float readPFloat(UIntPtr address, string code, string file = "")
        {
            byte[] memory = new byte[4];
            if (ReadProcessMemory(pHandle, address + LoadIntCode(code, file), memory, (UIntPtr)4, IntPtr.Zero))
            {
                float spawn = BitConverter.ToSingle(memory, 0);
                return (float)Math.Round(spawn, 2);
            }
            else
                return 0;
        }

        public int readPInt(UIntPtr address, string code, string file = "")
        {
            byte[] memory = new byte[4];
            if (ReadProcessMemory(pHandle, address + LoadIntCode(code, file), memory, (UIntPtr)4, IntPtr.Zero))
                return BitConverter.ToInt32(memory, 0);
            else
                return 0;
        }

        public string readPString(UIntPtr address, string code, string file = "")
        {
            byte[] memoryNormal = new byte[32];
            if (ReadProcessMemory(pHandle, address + LoadIntCode(code, file), memoryNormal, (UIntPtr)32, IntPtr.Zero))
                return CutString(System.Text.Encoding.ASCII.GetString(memoryNormal));
            else
                return "";
        }
        #endregion

        #region writeMemory
        ///<summary>
        ///Write to memory address. See https://github.com/erfg12/memory.dll/wiki/writeMemory() for more information.
        ///</summary>
        ///<param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        ///<param name="type">byte, 2bytes, bytes, float, int, string, double or long.</param>
        ///<param name="write">value to write to address.</param>
        ///<param name="file">path and name of .ini file (OPTIONAL)</param>
        public bool writeMemory(string code, string type, string write, string file = "")
        {
            byte[] memory = new byte[4];
            int size = 4;

            UIntPtr theCode;
            theCode = get64bitCode(code, file);

            if (type == "float")
            {
                memory = BitConverter.GetBytes(Convert.ToSingle(write));
                size = 4;
            }
            else if (type == "int")
            {
                memory = BitConverter.GetBytes(Convert.ToInt32(write));
                size = 4;
            }
            else if (type == "byte")
            {
                memory = new byte[1];
                memory[0] = Convert.ToByte(write, 16);
                size = 1;
            }
            else if (type == "2bytes")
            {
                memory = new byte[2];
                memory[0] = (byte)(Convert.ToInt32(write) % 256);
                memory[1] = (byte)(Convert.ToInt32(write) / 256);
                size = 2;
            }
            else if (type == "bytes")
            {
                if (write.Contains(",") || write.Contains(" ")) //check if it's a proper array
                {
                    string[] stringBytes;
                    if (write.Contains(","))
                        stringBytes = write.Split(',');
                    else
                        stringBytes = write.Split(' ');
                    //Debug.WriteLine("write:" + write + " stringBytes:" + stringBytes);

                    int c = stringBytes.Count();
                    memory = new byte[c];
                    for (int i = 0; i < c; i++)
                    {
                        memory[i] = Convert.ToByte(stringBytes[i], 16);
                    }
                    size = stringBytes.Count();
                }
                else //wasnt array, only 1 byte
                {
                    memory = new byte[1];
                    memory[0] = Convert.ToByte(write, 16);
                    size = 1;
                }
            }
            else if (type == "double")
            {
                memory = BitConverter.GetBytes(Convert.ToDouble(write));
                size = 8;
            }
            else if (type == "long")
            {
                memory = BitConverter.GetBytes(Convert.ToInt64(write));
                size = 8;
            }
            else if (type == "string")
            {
                memory = new byte[write.Length];
                memory = System.Text.Encoding.UTF8.GetBytes(write);
                size = memory.Length;
            }
#if DEBUG
            StackTrace stackTrace = new StackTrace(true);
            StackFrame sf = stackTrace.GetFrame(1);
            Debug.Write("DEBUG: Writing bytes[" + sf.GetMethod().Name + "():L" + sf.GetFileLineNumber() + "] [TYPE:" + type + " ADDR:" + theCode.ToUInt64().ToString("X") + "] " + String.Join(",", memory) + Environment.NewLine);
#endif
            return WriteProcessMemory(pHandle, theCode, memory, (UIntPtr)size, IntPtr.Zero);
        }

        /// <summary>
        /// Write to address and move by moveQty. Good for byte arrays. See https://github.com/erfg12/memory.dll/wiki/Writing-a-Byte-Array for more information.
        /// </summary>
        ///<param name="code">address, module + pointer + offset, module + offset OR label in .ini file.</param>
        ///<param name="type">byte, bytes, float, int, string or long.</param>
        /// <param name="write">byte to write</param>
        /// <param name="moveQty">quantity to move</param>
        /// <param name="file">path and name of .ini file (OPTIONAL)</param>
        /// <returns></returns>
        public bool writeMove(string code, string type, string write, int moveQty, string file = "")
        {
            byte[] memory = new byte[4];
            int size = 4;

            UIntPtr theCode;
            theCode = get64bitCode(code, file);

            if (type == "float")
            {
                memory = new byte[write.Length];
                memory = BitConverter.GetBytes(Convert.ToSingle(write));
                size = write.Length;
            }
            else if (type == "int")
            {
                memory = BitConverter.GetBytes(Convert.ToInt32(write));
                size = 4;
            }
            else if (type == "double")
            {
                memory = BitConverter.GetBytes(Convert.ToDouble(write));
                size = 8;
            }
            else if (type == "long")
            {
                memory = BitConverter.GetBytes(Convert.ToInt64(write));
                size = 8;
            }
            else if (type == "byte")
            {
                memory = new byte[1];
                memory[0] = Convert.ToByte(write, 16);
                size = 1;
            }
            else if (type == "string")
            {
                memory = new byte[write.Length];
                memory = System.Text.Encoding.UTF8.GetBytes(write);
                size = write.Length;
            }

            UIntPtr newCode = UIntPtr.Add(theCode, moveQty);

            Debug.Write("DEBUG: Writing bytes [TYPE:" + type + " ADDR:[O]" + theCode + " [N]" + newCode + " MQTY:" + moveQty + "] " + String.Join(",", memory) + Environment.NewLine);
            Thread.Sleep(1000);
            return WriteProcessMemory(pHandle, newCode, memory, (UIntPtr)size, IntPtr.Zero);
        }

        /// <summary>
        /// Write byte array to addresses.
        /// </summary>
        /// <param name="code">address to write to</param>
        /// <param name="write">byte array to write</param>
        /// <param name="file">path and name of ini file. (OPTIONAL)</param>
        public void writeBytes(string code, byte[] write, string file = "")
        {
            UIntPtr theCode;
            theCode = get64bitCode(code, file);
            WriteProcessMemory(pHandle, theCode, write, (UIntPtr)write.Length, IntPtr.Zero);
        }

        /// <summary>
        /// Write byte array to address
        /// </summary>
        /// <param name="address">Address to write to</param>
        /// <param name="write">Byte array to write to</param>
        public void writeBytes(UIntPtr address, params byte[] write)
        {
            WriteProcessMemory(pHandle, address, write, (UIntPtr)write.Length, out IntPtr bytesRead);
        }

        #endregion
        /// <summary>
        /// Convert code from string to real address. If path is not blank, will pull from ini file.
        /// </summary>
        /// <param name="name">label in ini file OR code</param>
        /// <param name="path">path to ini file (OPTIONAL)</param>
        /// <param name="size">size of address (default is 16)</param>
        /// <returns></returns>
        public UIntPtr get64bitCode(string name, string path = "", int size = 16)
        {
            string theCode = "";
            if (path != "")
                theCode = LoadCode(name, path);
            else
                theCode = name;

            if (theCode == "")
                return UIntPtr.Zero;
            string newOffsets = theCode;
            if (theCode.Contains("+"))
                newOffsets = theCode.Substring(theCode.IndexOf('+') + 1);

            byte[] memoryAddress = new byte[size];

            if (!theCode.Contains("+") && !theCode.Contains(",")) return new UIntPtr(Convert.ToUInt64(theCode, 16));

            if (newOffsets.Contains(','))
            {
                List<Int64> offsetsList = new List<Int64>();

                string[] newerOffsets = newOffsets.Split(',');
                foreach (string oldOffsets in newerOffsets)
                {
                    string test = oldOffsets;
                    if (oldOffsets.Contains("0x")) test = oldOffsets.Replace("0x", "");
                    Int64 preParse = 0;
                    if (!oldOffsets.Contains("-"))
                        preParse = Int64.Parse(test, NumberStyles.AllowHexSpecifier);
                    else
                    {
                        test = test.Replace("-", "");
                        preParse = Int64.Parse(test, NumberStyles.AllowHexSpecifier);
                        preParse = preParse * -1;
                    }
                    offsetsList.Add(preParse);
                }
                Int64[] offsets = offsetsList.ToArray();

                if (theCode.Contains("base") || theCode.Contains("main"))
                    ReadProcessMemory(pHandle, (UIntPtr)((Int64)mainModule.BaseAddress + offsets[0]), memoryAddress, (UIntPtr)size, IntPtr.Zero);
                else if (!theCode.Contains("base") && !theCode.Contains("main") && theCode.Contains("+"))
                {
                    string[] moduleName = theCode.Split('+');
                    IntPtr altModule = IntPtr.Zero;
                    if (!moduleName[0].Contains(".dll") && !moduleName[0].Contains(".exe"))
                        altModule = (IntPtr)Int64.Parse(moduleName[0], System.Globalization.NumberStyles.HexNumber);
                    else
                    {
                        try
                        {
                            altModule = modules[moduleName[0]];
                        }
                        catch
                        {
                            Debug.WriteLine("Module " + moduleName[0] + " was not found in module list!");
                            Debug.WriteLine("Modules: " + string.Join(",", modules));
                        }
                    }
                    ReadProcessMemory(pHandle, (UIntPtr)((Int64)altModule + offsets[0]), memoryAddress, (UIntPtr)size, IntPtr.Zero);
                }
                else // no offsets
                    ReadProcessMemory(pHandle, (UIntPtr)(offsets[0]), memoryAddress, (UIntPtr)size, IntPtr.Zero);

                Int64 num1 = BitConverter.ToInt64(memoryAddress, 0);

                UIntPtr base1 = (UIntPtr)0;
                try
                {
                    for (int i = 1; i < offsets.Length; i++)
                    {
                        base1 = new UIntPtr(Convert.ToUInt64(num1 + offsets[i]));
                        ReadProcessMemory(pHandle, base1, memoryAddress, (UIntPtr)size, IntPtr.Zero);
                        num1 = BitConverter.ToInt64(memoryAddress, 0);
                    }
                }
                catch
                {
                    return UIntPtr.Zero;
                }
                return base1;
            }
            else
            {
                Int64 trueCode = Convert.ToInt64(newOffsets, 16);
                IntPtr altModule = IntPtr.Zero;
                if (theCode.Contains("base") || theCode.Contains("main"))
                    altModule = mainModule.BaseAddress;
                else if (!theCode.Contains("base") && !theCode.Contains("main") && theCode.Contains("+"))
                {
                    string[] moduleName = theCode.Split('+');
                    if (!moduleName[0].Contains(".dll") && !moduleName[0].Contains(".exe"))
                    {
                        string theAddr = moduleName[0];
                        if (theAddr.Contains("0x")) theAddr = theAddr.Replace("0x", "");
                        altModule = (IntPtr)Int64.Parse(theAddr, NumberStyles.HexNumber);
                    }
                    else
                    {
                        try
                        {
                            altModule = modules[moduleName[0]];
                        }
                        catch
                        {
                            Debug.WriteLine("Module " + moduleName[0] + " was not found in module list!");
                            Debug.WriteLine("Modules: " + string.Join(",", modules));
                        }
                    }
                }
                else
                    altModule = modules[theCode.Split('+')[0]];
                return (UIntPtr)((Int64)altModule + trueCode);
            }
        }

        /// <summary>
        /// Close the process when finished.
        /// </summary>
        public void closeProcess()
        {
            if (pHandle == null)
                return;

            CloseHandle(pHandle);
            theProc = null;
        }

        /// <summary>
        /// Inject a DLL file.
        /// </summary>
        /// <param name="strDLLName">path and name of DLL file.</param>
        public void InjectDLL(String strDLLName)
        {
            IntPtr bytesout;

            foreach (ProcessModule pm in theProc.Modules)
            {
                if (pm.ModuleName.StartsWith("inject", StringComparison.InvariantCultureIgnoreCase))
                    return;
            }

            if (!theProc.Responding)
                return;

            int LenWrite = strDLLName.Length + 1;
            UIntPtr AllocMem = VirtualAllocEx(pHandle, (UIntPtr)null, (uint)LenWrite, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);

            WriteProcessMemory(pHandle, AllocMem, strDLLName, (UIntPtr)LenWrite, out bytesout);
            UIntPtr Injector = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

            if (Injector == null)
                return;

            IntPtr hThread = CreateRemoteThread(pHandle, (IntPtr)null, 0, Injector, AllocMem, 0, out bytesout);
            if (hThread == null)
                return;

            int Result = WaitForSingleObject(hThread, 10 * 1000);
            if (Result == 0x00000080L || Result == 0x00000102L)
            {
                if (hThread != null)
                    CloseHandle(hThread);
                return;
            }
            VirtualFreeEx(pHandle, AllocMem, (UIntPtr)0, 0x8000);

            if (hThread != null)
                CloseHandle(hThread);

            return;
        }

#if WINXP
#else
        /// <summary>
        /// Creates a code cave to write custom opcodes in target process
        /// </summary>
        /// <param name="code">Address to create the trampoline</param>
        /// <param name="newBytes">The opcodes to write in the code cave</param>
        /// <param name="replaceCount">The number of bytes being replaced</param>
        /// <param name="size">size of the allocated region</param>
        /// <param name="file">ini file to look in</param>
        /// <remarks>Please ensure that you use the proper replaceCount
        /// if you replace halfway in an instruction you may cause bad things</remarks>
        /// <returns>UIntPtr to created code cave for use for later deallocation</returns>
        public UIntPtr CreateCodeCave(string code, byte[] newBytes, int replaceCount, int size = 0x10000, string file = "")
        {
            UIntPtr theCode;
            theCode = get64bitCode(code, file);
            UIntPtr address = theCode;

            return CreateCodeCave(address, newBytes, replaceCount, size);
        }

        public UIntPtr CreateCodeCave(UIntPtr address, byte[] newBytes, int replaceCount, int size = 0x10000)
        {
            if (replaceCount < 5)
                return UIntPtr.Zero; // returning UIntPtr.Zero instead of throwing an exception
                                     // to better match existing code

            // if x64 we need to try to allocate near the address so we dont run into the +-2GB limit of the 0xE9 jmp

            UIntPtr caveAddress = UIntPtr.Zero;
            UIntPtr prefered = address;

            for (var i = 0; i < 10 && caveAddress == UIntPtr.Zero; i++)
            {
                caveAddress = VirtualAllocEx(pHandle, FindFreeBlockForRegion(prefered.ToUInt64(), (uint)newBytes.Length),
                                             (uint)size, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);

                if (caveAddress == UIntPtr.Zero)
                    prefered = UIntPtr.Add(prefered, 0x10000);
            }

            // Failed to allocate memory around the address we wanted let windows handle it and hope for the best?
            if (caveAddress == UIntPtr.Zero)
                caveAddress = VirtualAllocEx(pHandle, UIntPtr.Zero, (uint)size, MEM_COMMIT | MEM_RESERVE,
                                             PAGE_EXECUTE_READWRITE);

            int nopsNeeded = replaceCount > 5 ? replaceCount - 5 : 0;

            // (to - from - 5)
            int offset = (int)((long)caveAddress - (long)address - 5);

            byte[] jmpBytes = new byte[5 + nopsNeeded];
            jmpBytes[0] = 0xE9;
            BitConverter.GetBytes(offset).CopyTo(jmpBytes, 1);

            for (var i = 5; i < jmpBytes.Length; i++)
            {
                jmpBytes[i] = 0x90;
            }
            writeBytes(address, jmpBytes);

            byte[] caveBytes = new byte[5 + newBytes.Length];
            offset = (int)((long)address + jmpBytes.Length - ((long)caveAddress + newBytes.Length) - 5);

            newBytes.CopyTo(caveBytes, 0);
            caveBytes[newBytes.Length] = 0xE9;
            BitConverter.GetBytes(offset).CopyTo(caveBytes, newBytes.Length + 1);

            writeBytes(caveAddress, caveBytes);

            return caveAddress;
        }

        // array of memory allocations
        private List<MemoryAlloc> memoryAllocs = new List<MemoryAlloc>();

        /// <summary>
        /// Memory allocated
        /// </summary>
        struct MemoryAlloc
        {
            public ulong allocateNearThisAddress;
            public ulong address;
            public ulong pointer;
            public ulong size;
            public ulong SizeLeft => size - (pointer - address);
            public uint lastProtection;
        }

        public ulong Alloc(uint size, ulong allocateNearThisAddress)
        {
            GetSystemInfo(out SYSTEM_INFO systemInfo);

            try
            {
                // check for existing alloc near this address
                var i = memoryAllocs.Select((alloc, index) => new { alloc, index })
                                    .Where(pair => pair.alloc.allocateNearThisAddress == allocateNearThisAddress)
                                    .Select(pair => pair.index).First();

                // get the alloc from the array
                var found = memoryAllocs[i];
                // is there enough room
                if (found.SizeLeft >= size)
                {
                    var ret = found.pointer;
                    found.pointer += size;
                    memoryAllocs[i] = found;
                    return ret;
                }
            }
            catch { }

            var addr = FindFreeBlockForRegion(allocateNearThisAddress, size);

            VirtualQueryEx(theProc.Handle, new UIntPtr((ulong)addr), out MEMORY_BASIC_INFORMATION mbi);

            memoryAllocs.Add(new MemoryAlloc
            {
                address = addr.ToUInt64(),
                allocateNearThisAddress = allocateNearThisAddress,
                pointer = addr.ToUInt64() + size,
                size = systemInfo.pageSize,
                lastProtection = mbi.Protect
            });

            if (VirtualAllocEx(theProc.Handle, new UIntPtr((ulong)addr), size, MEM_RESERVE | MEM_COMMIT, PAGE_EXECUTE_READWRITE) == null)
                throw new Exception("Couldn't allocate memory at " + addr);

            return (ulong)addr;
        }

        /*
        private UIntPtr FindFreeBlockForRegion(UIntPtr baseAddress, uint size)
        {
            UIntPtr minAddress = UIntPtr.Subtract(baseAddress, 0x70000000);
            UIntPtr maxAddress = UIntPtr.Add(baseAddress, 0x70000000);

            UIntPtr ret = UIntPtr.Zero;
            UIntPtr tmpAddress;

            GetSystemInfo(out SYSTEM_INFO si);

            if (Is64Bit)
            {
                if ((long)minAddress > (long)si.maximumApplicationAddress ||
                    (long)minAddress < (long)si.minimumApplicationAddress)
                    minAddress = si.minimumApplicationAddress;

                if ((long)maxAddress < (long)si.minimumApplicationAddress ||
                    (long)maxAddress > (long)si.maximumApplicationAddress)
                    maxAddress = si.maximumApplicationAddress;
            }
            else
            {
                minAddress = si.minimumApplicationAddress;
                maxAddress = si.maximumApplicationAddress;
            }

			UIntPtr current = minAddress;

			while (VirtualQueryEx(pHandle, current, out MEMORY_BASIC_INFORMATION mbi).ToUInt64() != 0)
            {
                if ((long)mbi.BaseAddress > (long)maxAddress)
                    return UIntPtr.Zero;  // No memory found, let windows handle

                if (mbi.State == MEM_FREE && mbi.RegionSize > size)
                {
                    if ((long)mbi.BaseAddress % si.allocationGranularity > 0)
                    {
                        // The whole size can not be used
                        tmpAddress = mbi.BaseAddress;
                        int offset = (int)(si.allocationGranularity -
                                           ((long)tmpAddress % si.allocationGranularity));

                        // Check if there is enough left
                        if ((mbi.RegionSize - offset) >= size)
                        {
                            // yup there is enough
                            tmpAddress = UIntPtr.Add(tmpAddress, offset);

                            if ((long)tmpAddress < (long)baseAddress)
                            {
                                tmpAddress = UIntPtr.Add(tmpAddress, (int)(mbi.RegionSize - offset - size));

                                if ((long)tmpAddress > (long)baseAddress)
                                    tmpAddress = baseAddress;

                                // decrease tmpAddress until its alligned properly
                                tmpAddress = UIntPtr.Subtract(tmpAddress, (int)((long)tmpAddress % si.allocationGranularity));
                            }

                            // if the difference is closer then use that
                            if (Math.Abs((long)tmpAddress - (long)baseAddress) < Math.Abs((long)ret - (long)baseAddress))
                                ret = tmpAddress;
                        }
                    }
                    else
                    {
                        tmpAddress = mbi.BaseAddress;

                        if ((long)tmpAddress < (long)baseAddress) // try to get it the cloest possible 
                                                                  // (so to the end of the region - size and
                                                                  // aligned by system allocation granularity)
                        {
                            tmpAddress = UIntPtr.Add(tmpAddress, (int)(mbi.RegionSize - size));

                            if ((long)tmpAddress > (long)baseAddress)
                                tmpAddress = baseAddress;

                            // decrease until aligned properly
                            tmpAddress =
                                UIntPtr.Subtract(tmpAddress, (int)((long)tmpAddress % si.allocationGranularity));
                        }

                        if (Math.Abs((long)tmpAddress - (long)baseAddress) < Math.Abs((long)ret - (long)baseAddress))
                            ret = tmpAddress;
                    }
                }

                if (mbi.RegionSize % si.allocationGranularity > 0)
                    mbi.RegionSize += si.allocationGranularity - (mbi.RegionSize % si.allocationGranularity);

				UIntPtr previous = current;
				current = UIntPtr.Add(mbi.BaseAddress, (int)mbi.RegionSize);

                if ((long)current > (long)maxAddress)
                    return ret;

                if ((long)previous > (long)current)
                    return ret; // Overflow
            }

            return ret;
        }*/

        private UIntPtr FindFreeBlockForRegion(ulong @base, uint size)
        {
            // initialize minimum and maximum address space relative to the base address
            // maximum JMP instruction for 64-bit is a relative JMP using the RIP register
            // jump to offset of 32-bit value, max being 7FFFFFFF
            // cheat engine slices off the Fs to give just 70000000 for unknown reasons
            var minAddress = @base - 0x70000000; // 0x10000 (32-bit)
            var maxAddress = @base + 0x70000000; // 0xfffffffff (32-bit)

            // retrieve system info
            GetSystemInfo(out SYSTEM_INFO systemInfo);

            // keep min and max values within the system range for a given application
            if (minAddress < (ulong)systemInfo.minimumApplicationAddress)
                minAddress = (ulong)systemInfo.minimumApplicationAddress;
            if (maxAddress > (ulong)systemInfo.maximumApplicationAddress)
                maxAddress = (ulong)systemInfo.maximumApplicationAddress;

            // address for the current loop
            ulong addr = minAddress;
            // address from the last loop
            ulong oldAddr = 0;
            // current result to be passed back from function
            ulong result = 0;

            // query information about pages in virtual address space into mbi
            while (VirtualQueryEx(theProc.Handle, new UIntPtr(addr), out MEMORY_BASIC_INFORMATION mbi).ToUInt64() != 0)
            {
                // the base address is past the max address
                if (mbi.BaseAddress.ToUInt64() > maxAddress)
                    return UIntPtr.Zero; // throw new Exception("Base address is greater than max address.");

                // check if the state is free to allocate and the region size allocated is enough to fit our requested size
                if (mbi.State == MEM_FREE && mbi.RegionSize > size)
                {
                    // set address to the current base address
                    ulong nAddr = mbi.BaseAddress.ToUInt64();
                    // get potential offset from granuarltiy alignment
                    var offset = systemInfo.allocationGranularity - (nAddr % systemInfo.allocationGranularity);

                    // checks base address if it's on the edge of the allocation granularity (page)
                    if (mbi.BaseAddress.ToUInt64() % systemInfo.allocationGranularity > 0)
                    {
                        if ((ulong)mbi.RegionSize - offset >= size)
                        {
                            // increase by potential offset
                            nAddr += offset;

                            // address is under base address
                            if (nAddr < @base)
                            {
                                // move into the region
                                nAddr += (ulong)mbi.RegionSize - offset - size;
                                // prevent overflow past base address
                                if (nAddr > @base)
                                    nAddr = @base;
                                // align to page
                                nAddr -= nAddr % systemInfo.allocationGranularity;
                            }

                            // new address is less than the one found last loop
                            if (Math.Abs((long)(nAddr - @base)) < Math.Abs((long)(result - @base)))
                                result = nAddr;
                        }
                    }
                    else
                    {
                        // address is under base address
                        if (nAddr < @base)
                        {
                            // move into the region
                            nAddr += (ulong)mbi.RegionSize - size;
                            // prevent overflow past base address
                            if (nAddr > @base)
                                nAddr = @base;
                            // align to page
                            nAddr -= nAddr % systemInfo.allocationGranularity;
                        }

                        // new address is less than the one found last loop
                        if (Math.Abs((long)(nAddr - @base)) < Math.Abs((long)(result - @base)))
                            result = nAddr;
                    }
                }

                // region size isn't aligned with allocation granularity increase by difference 
                if (mbi.RegionSize % systemInfo.allocationGranularity > 0)
                    mbi.RegionSize += systemInfo.allocationGranularity - (mbi.RegionSize % systemInfo.allocationGranularity);

                // set old address
                oldAddr = addr;
                // increase address to the next region from our base address
                addr = (ulong)mbi.BaseAddress + (ulong)mbi.RegionSize;

                // address goes over max size or overflow
                if (addr > maxAddress || oldAddr > addr)
                    return (UIntPtr)result;
            }

            return (UIntPtr)result; // maybe not a good idea not sure
        }
#endif

        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        public static void SuspendProcess(int pid)
        {
            var process = Process.GetProcessById(pid);

            if (process.ProcessName == string.Empty)
                return;

            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);
                if (pOpenThread == IntPtr.Zero)
                    continue;

                SuspendThread(pOpenThread);
                CloseHandle(pOpenThread);
            }
        }

        public static void ResumeProcess(int pid)
        {
            var process = Process.GetProcessById(pid);
            if (process.ProcessName == string.Empty)
                return;

            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);
                if (pOpenThread == IntPtr.Zero)
                    continue;

                var suspendCount = 0;
                do
                {
                    suspendCount = ResumeThread(pOpenThread);
                } while (suspendCount > 0);
                CloseHandle(pOpenThread);
            }
        }

#if WINXP
#else
        async Task PutTaskDelay(int delay)
        {
            await Task.Delay(delay);
        }
#endif

        void AppendAllBytes(string path, byte[] bytes)
        {
            using (var stream = new FileStream(path, FileMode.Append))
            {
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        public byte[] fileToBytes(string path, bool dontDelete = false)
        {
            byte[] newArray = File.ReadAllBytes(path);
            if (!dontDelete)
                File.Delete(path);
            return newArray;
        }

        public string mSize()
        {
            if (is64bit())
                return ("x16");
            else
                return ("x8");
        }

        /// <summary>
        /// Convert a byte array to hex values in a string.
        /// </summary>
        /// <param name="ba">your byte array to convert</param>
        /// <returns></returns>
        public static string ByteArrayToHexString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            int i = 1;
            foreach (byte b in ba)
            {
                if (i == 16)
                {
                    hex.AppendFormat("{0:x2}{1}", b, Environment.NewLine);
                    i = 0;
                }
                else
                    hex.AppendFormat("{0:x2} ", b);
                i++;
            }
            return hex.ToString().ToUpper();
        }

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
            {
                hex.AppendFormat("{0:x2} ", b);
            }
            return hex.ToString();
        }

#if WINXP
#else

        public struct SYSTEM_INFO
        {
            public ushort processorArchitecture;
            ushort reserved;
            public uint pageSize;
            public UIntPtr minimumApplicationAddress;
            public UIntPtr maximumApplicationAddress;
            public IntPtr activeProcessorMask;
            public uint numberOfProcessors;
            public uint processorType;
            public uint allocationGranularity;
            public ushort processorLevel;
            public ushort processorRevision;
        }

        public struct MEMORY_BASIC_INFORMATION32
        {
            public UIntPtr BaseAddress;
            public UIntPtr AllocationBase;
            public uint AllocationProtect;
            public uint RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        public struct MEMORY_BASIC_INFORMATION64
        {
            public UIntPtr BaseAddress;
            public UIntPtr AllocationBase;
            public uint AllocationProtect;
            public uint __alignment1;
            public ulong RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
            public uint __alignment2;
        }

        public struct MEMORY_BASIC_INFORMATION
        {
            public UIntPtr BaseAddress;
            public UIntPtr AllocationBase;
            public uint AllocationProtect;
            public long RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        public ulong getMinAddress()
        {
            SYSTEM_INFO SI;
            GetSystemInfo(out SI);
            return (ulong)SI.minimumApplicationAddress;
        }

        /// <summary>
        /// Dump memory page by page to a dump.dmp file. Can be used with Cheat Engine.
        /// </summary>
        public bool DumpMemory(string file = "dump.dmp")
        {
            Debug.Write("[DEBUG] memory dump starting... (" + DateTime.Now.ToString("h:mm:ss tt") + ")" + Environment.NewLine);
            SYSTEM_INFO sys_info = new SYSTEM_INFO();
            GetSystemInfo(out sys_info);

            UIntPtr proc_min_address = sys_info.minimumApplicationAddress;
            UIntPtr proc_max_address = sys_info.maximumApplicationAddress;

            // saving the values as long ints so I won't have to do a lot of casts later
            Int64 proc_min_address_l = (Int64)proc_min_address; //(Int64)procs.MainModule.BaseAddress;
            Int64 proc_max_address_l = (Int64)theProc.VirtualMemorySize64 + proc_min_address_l;

            //int arrLength = 0;
            if (File.Exists(file))
                File.Delete(file);


            MEMORY_BASIC_INFORMATION memInfo = new MEMORY_BASIC_INFORMATION();
            while (proc_min_address_l < proc_max_address_l)
            {
                VirtualQueryEx(pHandle, proc_min_address, out memInfo);
                byte[] buffer = new byte[(Int64)memInfo.RegionSize];
                UIntPtr test = (UIntPtr)((Int64)memInfo.RegionSize);
                UIntPtr test2 = (UIntPtr)((Int64)memInfo.BaseAddress);

                ReadProcessMemory(pHandle, test2, buffer, test, IntPtr.Zero);

                AppendAllBytes(file, buffer); //due to memory limits, we have to dump it then store it in an array.
                //arrLength += buffer.Length;

                proc_min_address_l += (Int64)memInfo.RegionSize;
                proc_min_address = new UIntPtr((ulong)proc_min_address_l);
            }


            Debug.Write("[DEBUG] memory dump completed. Saving dump file to " + file + ". (" + DateTime.Now.ToString("h:mm:ss tt") + ")" + Environment.NewLine);
            return true;
        }

        /// <summary>
        /// Array of byte scan.
        /// </summary>
        /// <param name="search">array of bytes to search for, OR your ini code label.</param>
        /// <param name="writable">Include writable addresses in scan</param>
        /// <param name="executable">Include executable addresses in scan</param>
        /// <param name="file">ini file (OPTIONAL)</param>
        /// <returns>IEnumerable of all addresses found.</returns>
        public IEnumerable<long> AoBScan(string search, bool writable = false, bool executable = true, string file = "")
        {
            return AoBScan(0, long.MaxValue, search, writable, executable, file);
        }

        /// <summary>
        /// Array of Byte scan.
        /// </summary>
        /// <param name="start">Your starting address.</param>
        /// <param name="end">ending address</param>
        /// <param name="search">array of bytes to search for, OR your ini code label.</param>
        /// <param name="file">ini file (OPTIONAL)</param>
        /// <param name="writable">Include writable addresses in scan</param>
        /// <param name="executable">Include executable addresses in scan</param>
        /// <returns>IEnumerable of all addresses found.</returns>
        public IEnumerable<long> AoBScan(long start, long end, string search, bool writable = false, bool executable = true, string file = "")
        {
            var memRegionList = new List<MemoryRegionResult>();

            string memCode = LoadCode(search, file);

            string[] stringByteArray = memCode.Split(' ');
            byte[] mask = new byte[stringByteArray.Length];

            for (var i = 0; i < stringByteArray.Length; i++)
            {
                string ba = stringByteArray[i];

                if (ba == "??" || (ba.Length == 1 && ba == "?"))
                {
                    mask[i] = 0x00;
                    stringByteArray[i] = "0x00";
                }
                else if (Char.IsLetterOrDigit(ba[0]) && ba[1] == '?')
                {
                    mask[i] = 0xF0;
                    stringByteArray[i] = ba[0] + "0";
                }
                else if (Char.IsLetterOrDigit(ba[1]) && ba[0] == '?')
                {
                    mask[i] = 0x0F;
                    stringByteArray[i] = "0" + ba[1];
                }
                else
                    mask[i] = 0xFF;
            }

            SYSTEM_INFO sys_info = new SYSTEM_INFO();
            GetSystemInfo(out sys_info);

            UIntPtr proc_min_address = sys_info.minimumApplicationAddress;
            UIntPtr proc_max_address = sys_info.maximumApplicationAddress;

            if (start < (long)proc_min_address.ToUInt64())
                start = (long)proc_min_address.ToUInt64();

            if (end > (long)proc_max_address.ToUInt64())
                end = (long)proc_max_address.ToUInt64();

            Debug.WriteLine("[DEBUG] memory scan starting... (min:0x" + proc_min_address.ToUInt64().ToString(mSize()) + " max:0x" + proc_max_address.ToUInt64().ToString(mSize()) + " time:" + DateTime.Now.ToString("h:mm:ss tt") + ")");
            UIntPtr currentBaseAddress = new UIntPtr((ulong)start);

            MEMORY_BASIC_INFORMATION memInfo = new MEMORY_BASIC_INFORMATION();

            //Debug.WriteLine("[DEBUG] start:0x" + start.ToString("X8") + " curBase:0x" + currentBaseAddress.ToUInt64().ToString("X8") + " end:0x" + end.ToString("X8") + " size:0x" + memInfo.RegionSize.ToString("X8") + " vAloc:" + VirtualQueryEx(pHandle, currentBaseAddress, out memInfo).ToUInt64().ToString());

            while (VirtualQueryEx(pHandle, currentBaseAddress, out memInfo).ToUInt64() != 0 &&
                   currentBaseAddress.ToUInt64() < (ulong)end &&
                   currentBaseAddress.ToUInt64() + (ulong)memInfo.RegionSize >
                   currentBaseAddress.ToUInt64())
            {
                bool isValid = memInfo.State == MEM_COMMIT;
                isValid &= memInfo.BaseAddress.ToUInt64() < (ulong)proc_max_address.ToUInt64();
                isValid &= ((memInfo.Protect & PAGE_GUARD) == 0);
                isValid &= ((memInfo.Protect & PAGE_NOACCESS) == 0);
                isValid &= (memInfo.Type == MEM_PRIVATE) || (memInfo.Type == MEM_IMAGE);

                if (isValid)
                {
                    bool isWritable = ((memInfo.Protect & PAGE_READWRITE) > 0) ||
                                      ((memInfo.Protect & PAGE_WRITECOPY) > 0) ||
                                      ((memInfo.Protect & PAGE_EXECUTE_READWRITE) > 0) ||
                                      ((memInfo.Protect & PAGE_EXECUTE_WRITECOPY) > 0);

                    bool isExecutable = ((memInfo.Protect & PAGE_EXECUTE) > 0) ||
                                        ((memInfo.Protect & PAGE_EXECUTE_READ) > 0) ||
                                        ((memInfo.Protect & PAGE_EXECUTE_READWRITE) > 0) ||
                                        ((memInfo.Protect & PAGE_EXECUTE_WRITECOPY) > 0);

                    isWritable &= writable;
                    isExecutable &= executable;

                    isValid &= isWritable || isExecutable;
                }

                if (!isValid)
                {
                    currentBaseAddress = new UIntPtr(memInfo.BaseAddress.ToUInt64() + (ulong)memInfo.RegionSize);
                    continue;
                }

                MemoryRegionResult memRegion = new MemoryRegionResult
                {
                    CurrentBaseAddress = currentBaseAddress,
                    RegionSize = memInfo.RegionSize,
                    RegionBase = memInfo.BaseAddress
                };

                currentBaseAddress = new UIntPtr(memInfo.BaseAddress.ToUInt64() + (ulong)memInfo.RegionSize);

                //Console.WriteLine("SCAN start:" + memRegion.RegionBase.ToString() + " end:" + currentBaseAddress.ToString());

                if (memRegionList.Count > 0)
                {
                    var previousRegion = memRegionList[memRegionList.Count - 1];

                    if ((long)previousRegion.RegionBase + previousRegion.RegionSize == (long)memInfo.BaseAddress)
                    {
                        memRegionList[memRegionList.Count - 1] = new MemoryRegionResult
                        {
                            CurrentBaseAddress = previousRegion.CurrentBaseAddress,
                            RegionBase = previousRegion.RegionBase,
                            RegionSize = previousRegion.RegionSize + memInfo.RegionSize
                        };

                        continue;
                    }
                }

                memRegionList.Add(memRegion);
            }

            ConcurrentBag<long> bagResult = new ConcurrentBag<long>();

            Parallel.ForEach(memRegionList,
                             (item, parallelLoopState, index) =>
                             {
                                 long[] compareResults = CompareScan(item, stringByteArray, mask);

                                 foreach (long result in compareResults)
                                     bagResult.Add(result);
                             });

            Debug.WriteLine("[DEBUG] memory scan completed. (time:" + DateTime.Now.ToString("h:mm:ss tt") + ")");

            return bagResult.ToList().OrderBy(c => c);
        }

        /// <summary>
        /// Array of bytes scan
        /// </summary>
        /// <param name="code">Starting address or ini label</param>
        /// <param name="end">ending address</param>
        /// <param name="search">array of bytes to search for or your ini code label</param>
        /// <param name="file">ini file</param>
        /// <returns>First address found</returns>
        public long AoBScan(string code, long end, string search, string file = "")
        {
            long start = (long)get64bitCode(code, file).ToUInt64();

            return (AoBScan(start, end, search, true, true, file)).FirstOrDefault();
        }

        private long[] CompareScan(MemoryRegionResult item, string[] aobToFind, byte[] mask)
        {
            if (mask.Length != aobToFind.Length)
                throw new ArgumentException($"{nameof(aobToFind)}.Length != {nameof(mask)}.Length");

            byte[] buffer = new byte[item.RegionSize];
            ReadProcessMemory(pHandle, item.CurrentBaseAddress, buffer, (UIntPtr)item.RegionSize, out ulong bytesRead);


            byte[] aobPattern = new byte[aobToFind.Length];

            for (int i = 0; i < aobToFind.Length; i++)
                aobPattern[i] = (byte)(Convert.ToByte(aobToFind[i], 16) & mask[i]);

            int result = 0 - aobToFind.Length;
            List<long> ret = new List<long>();
            do
            {
                result = FindPattern(buffer, aobPattern, mask, result + aobToFind.Length);

                if (result >= 0)
                    ret.Add((long)item.CurrentBaseAddress + result);

            } while (result != -1);

            return ret.ToArray();
        }

        private int FindPattern(byte[] body, byte[] pattern, byte[] masks, int start = 0)
        {
            int foundIndex = -1;

            if (body.Length <= 0 || pattern.Length <= 0 || start > body.Length - pattern.Length ||
                pattern.Length > body.Length) return foundIndex;

            for (int index = start; index <= body.Length - pattern.Length; index++)
            {
                if (((body[index] & masks[0]) == (pattern[0] & masks[0])))
                {
                    var match = true;
                    for (int index2 = 1; index2 <= pattern.Length - 1; index2++)
                    {
                        if ((body[index + index2] & masks[index2]) == (pattern[index2] & masks[index2])) continue;
                        match = false;
                        break;

                    }

                    if (!match) continue;

                    foundIndex = index;
                    break;
                }
            }

            return foundIndex;
        }

#endif
    }
}