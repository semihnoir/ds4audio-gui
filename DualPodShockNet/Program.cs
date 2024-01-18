using System;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;

namespace DualPodShockNet
{
    class Program
    {
        private static Thread playerThread;

        static void Main(string[] args)
        {
            string filePath = @"D:\Downloads\ds4Audio\DS4AudioGUI\bin\Debug\output.sbc";
            byte soundCommand = 0x00;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--file":
                        if (i + 1 < args.Length)
                        {
                            filePath = args[i + 1];
                        }
                        break;
                    case "--sound":
                        if (i + 1 < args.Length)
                        {
                            try
                            {
                                soundCommand = Convert.ToByte(args[i + 1], 16);
                                Console.WriteLine("Hex formatında dönüştürüldü: {0}", soundCommand);
                            }
                            catch (FormatException)
                            {
                                Console.WriteLine("Geçersiz hex sayısı: {0}", args[i + 1]);
                            }
                            catch (OverflowException)
                            {
                                Console.WriteLine("Geçersiz hex sayısı: {0}", args[i + 1]);
                            }
                        }
                        break;
                }
            }

            Console.WriteLine("File Path: {0}", filePath);
            Console.WriteLine("Sound Command: {0}", soundCommand);

            try
            {
                Process.GetCurrentProcess().PriorityClass =
                    ProcessPriorityClass.High;
            }
            catch { } // Ignore problems raising the priority.

            // Force Normal IO Priority
            IntPtr ioPrio = new IntPtr(2);
            Util.NtSetInformationProcess(Process.GetCurrentProcess().Handle,
                Util.PROCESS_INFORMATION_CLASS.ProcessIoPriority, ref ioPrio, 4);

            // Force Normal Page Priority
            IntPtr pagePrio = new IntPtr(5);
            Util.NtSetInformationProcess(Process.GetCurrentProcess().Handle,
                Util.PROCESS_INFORMATION_CLASS.ProcessPagePriority, ref pagePrio, 4);

            if (args.Length > 0 && File.Exists(args[0]))
            {
                filePath = args[0];
            }

            Crc32Algorithm.InitializeTable(0xedb88320u);

            List<HidDevice> hidDevices = DeviceEnumerator.FindDevices();
            HidDevice usedDevice = hidDevices.FirstOrDefault();
            if (usedDevice == null)
            {
                return;
            }

            Console.WriteLine("FOUND DEVICE");
            Console.WriteLine("USING DEVICE: {0}", usedDevice.DevicePath);

            playerThread = new Thread(() =>
            {
                usedDevice.OpenDevice(false);
                PlayerWorker worker = new PlayerWorker(filePath, usedDevice, soundCommand);
                worker.Playback();
            });
            playerThread.IsBackground = true;
            playerThread.Priority = ThreadPriority.AboveNormal;
            playerThread.Name = "PLAYER THREAD";

            playerThread.Start();
            playerThread.Join();
        }
    }
}
