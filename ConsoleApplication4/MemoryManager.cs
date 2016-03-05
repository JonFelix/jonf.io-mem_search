using System.Runtime.InteropServices;
using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace ConsoleApplication4
{
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

    class MemoryManager
    {      
        const int PROCESS_QUERY_INFORMATION = 0x0400;
        const int MEM_COMMIT = 0x00001000;
        const int PAGE_READWRITE = 0x04;
        const int PROCESS_WM_READ = 0x0010;  

        [DllImport("kernel32.dll")]
        static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern System.IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);



        public void SuspendProcess(Process process)
        {        
            if(process.ProcessName == string.Empty)
                return;

            foreach(ProcessThread _Thread in process.Threads)
            {
                IntPtr _OpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)_Thread.Id);

                if(_OpenThread == IntPtr.Zero)
                    continue;
                SuspendThread(_OpenThread);
            }
        }

        public void ResumeProcess(Process process)
        {       
            if(process.ProcessName == string.Empty)
                return;

            foreach(ProcessThread _Thread in process.Threads)
            {
                IntPtr _OpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)_Thread.Id);

                if(_OpenThread == IntPtr.Zero)
                    continue;
                int _SuspendCount = 0;
                do
                {
                    _SuspendCount = ResumeThread(_OpenThread);
                } while(_SuspendCount > 0);    
            }
        }  

        public int[] GetPointers(Process process, string filter)
        {
            if(process.ProcessName == string.Empty)
                return new int[0];

            List<int> _Result = new List<int>();
            SYSTEM_INFO _SysInfo = new SYSTEM_INFO();
            GetSystemInfo(out _SysInfo);

            IntPtr _ProcMax = _SysInfo.maximumApplicationAddress;
            IntPtr _ProcMin = _SysInfo.minimumApplicationAddress;

            long _ProcMaxLong = (long)_ProcMax;
            long _ProcMinLong = (long)_ProcMin;             

            char[] _SearchTerm = filter.ToCharArray();

            Process _Proc = process;
            IntPtr _ProcHandle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_WM_READ, false, _Proc.Id);

            MEMORY_BASIC_INFORMATION _MemBasicInfo = new MEMORY_BASIC_INFORMATION();

            int _BytesRead = 0;

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
                            if(_SearchTerm[0] == (char)_Buffer[i])
                            {
                                bool _Trigger = true;
                                int _NullOffset = 0;
                                for(int z = 0; z < _SearchTerm.Length; z++)
                                {
                                    if(i + z + _NullOffset >= _Buffer.Length)
                                        break;
                                    if((char)_Buffer[i + z + _NullOffset] == '\0')
                                    {
                                        _NullOffset++;
                                    }
                                    if(i + z + _NullOffset >= _Buffer.Length)
                                        break;
                                    _Trigger = _SearchTerm[z] == (char)_Buffer[i + z + _NullOffset];
                                    if(!_Trigger)
                                        break;
                                }
                                if(_Trigger)
                                    _Result.Add(_MemBasicInfo.BaseAddress + i);
                            }
                        }
                    }
                }
                _ProcMinLong += _MemBasicInfo.RegionSize;
                _ProcMin = new IntPtr(_ProcMinLong);  
            }
            return _Result.ToArray();
        }
             
        public char[] GetValueFromPointer(Process process, int pointer, int offset = 0)
        {
            if(process.ProcessName == string.Empty)
                return new char[0];
            IntPtr _ProcHandle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_WM_READ, false, process.Id);
            List<Char> _Result = new List<char>();
            int _BytesRead = 0;
            byte[] _Buffer = new byte[offset];
            ReadProcessMemory((int)_ProcHandle, pointer, _Buffer, offset, ref _BytesRead);              
            for(int i = 0; i < _Buffer.Length; i++)  
                _Result.Add((char)_Buffer[i]);        
            return _Result.ToArray();
        }

    }
}
