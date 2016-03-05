using System;                      
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ConsoleApplication4
{
    

    class Program
    {
        static MemoryManager _MemManager;
                                            
        static void Main(string[] args)
        {
            _MemManager = new MemoryManager();

            string processName = Console.ReadLine();  

            Process process = Process.GetProcessesByName(processName)[0];

            _MemManager.SuspendProcess(process);

            string filter = Console.ReadLine();

            int[] result = _MemManager.GetPointers(process, filter);

            for(int i = 0; i < result.Length; i++)
            {
                char[] value = _MemManager.GetValueFromPointer(process, result[i], filter.Length);
                Console.Write("0x" + result[i].ToString("X") + " : ");
                foreach(char x in value)
                    Console.Write(((int)x).ToString("X") + " ");
                Console.Write(" : ");
                foreach(char x in value)
                    Console.Write(x.ToString());
                Console.Write('\n');
            }    

            Console.ReadKey(true);
            _MemManager.ResumeProcess(process);
        }  
    }
}
