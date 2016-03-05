using System;                      
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ConsoleApplication4
{
    public struct SYSTEM_INFO
    {
        public ushort processorArchitecture;
        ushort reserved;
        public uint pageSize;
        public IntPtr minimumApplicationAddress;  
        public IntPtr maximumApplicationAddress;  
        public IntPtr activeProcessorMask;
        public uint numberOfProcessors;
        public uint processorType;
        public uint allocationGranularity;
        public ushort processorLevel;
        public ushort processorRevision;
    }

    public struct MEMORY_BASIC_INFORMATION
    {
        public int BaseAddress;
        public int AllocationBase;
        public int AllocationProtect;
        public int RegionSize; 
        public int State;
        public int Protect;
        public int lType;
    }

    public class MEMORY_ITEM
    {
        public int Location;
        public char Content;
        public MEMORY_ITEM Next;

        public MEMORY_ITEM(int location, char content)
        {
            Location = location;
            Content = content;
        }
    }

    class Program
    {           
        const int PROCESS_QUERY_INFORMATION = 0x0400;
        const int MEM_COMMIT = 0x00001000;
        const int PAGE_READWRITE = 0x04;
        const int PROCESS_WM_READ = 0x0010;

        static string _ProcessName = "";    

        [DllImport("kernel32.dll")]
        static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        static void Main(string[] args)
        {   
            SYSTEM_INFO _SysInfo = new SYSTEM_INFO();
            GetSystemInfo(out _SysInfo);

            IntPtr _ProcMax = _SysInfo.maximumApplicationAddress;
            IntPtr _ProcMin = _SysInfo.minimumApplicationAddress;

            long _ProcMaxLong = (long)_ProcMax;
            long _ProcMinLong = (long)_ProcMin;

            Console.WriteLine("Enter process!");
            _ProcessName = Console.ReadLine();
            if(Process.GetProcessesByName(_ProcessName).Length == 0)
                return;

            Process _Proc = Process.GetProcessesByName(_ProcessName)[0];
            IntPtr _ProcHandle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_WM_READ, false, _Proc.Id);

            MEMORY_BASIC_INFORMATION _MemBasicInfo = new MEMORY_BASIC_INFORMATION();

            int _BytesRead = 0;
            MEMORY_ITEM _OutputStart = new MEMORY_ITEM(0, '\0');
            MEMORY_ITEM _OutputEnd = _OutputStart;
            ;    

            while(_ProcMinLong < _ProcMaxLong)
            {
                VirtualQueryEx(_ProcHandle, _ProcMin, out _MemBasicInfo, 28);

                if(_MemBasicInfo.Protect == PAGE_READWRITE && _MemBasicInfo.State == MEM_COMMIT)
                {
                    byte[] _Buffer = new byte[_MemBasicInfo.RegionSize];
                    ReadProcessMemory((int)_ProcHandle, _MemBasicInfo.BaseAddress, _Buffer, _MemBasicInfo.RegionSize, ref _BytesRead);

                    for(int i = 0; i < _MemBasicInfo.RegionSize; i++)
                    {
                        if((char)_Buffer[i] != '\0')
                        {      
                            _OutputEnd.Next = new MEMORY_ITEM((_MemBasicInfo.BaseAddress + i), (char)_Buffer[i]);
                            _OutputEnd = _OutputEnd.Next;
                        }
                    } 
                }
                _ProcMinLong += _MemBasicInfo.RegionSize;
                _ProcMin = new IntPtr(_ProcMinLong);
            } 

            Console.WriteLine("Scan done!");
            Console.WriteLine("Search?");
            char[] _SearchTerm = Console.ReadLine().ToCharArray();

            MEMORY_ITEM _OutputCurrent = _OutputStart.Next;
            while(_OutputCurrent != null)
            {
                if(_SearchTerm[0] == _OutputCurrent.Content)
                {
                    MEMORY_ITEM _OutputTemp = _OutputCurrent;
                    bool _Trigger = true;
                    for(int z = 0; z < _SearchTerm.Length; z++)
                    {
                        _Trigger = _SearchTerm[z] == _OutputCurrent.Content;
                        _OutputCurrent = _OutputCurrent.Next;
                        if(!_Trigger)
                            break;
                    }
                    _OutputCurrent = _OutputTemp;
                    if(_Trigger)
                        Console.WriteLine("Occurance found at 0x" + _OutputCurrent.Location.ToString("X"));  
                }
                _OutputCurrent = _OutputCurrent.Next;
            }
            Console.WriteLine("Search complete!");
            Console.ReadKey(true);
        }
    }
}
