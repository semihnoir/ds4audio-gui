using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Diagnostics;
using DS4Windows;

namespace DualPodShockNet
{
    class PlayerWorker
    {
        private const byte PROTOCOL = 0x15;
        private const byte MODE_TYPE = 0xC0 | 0x04;
        private const byte TRANSACTION_TYPE = 0xA2;
        // Headphone volume L (0x10), Headphone volume R (0x20), Mic volume (0x40), Speaker volume (0x80)
        // enable rumble (0x01), lightbar (0x02), flash (0x04).
        private const byte FEATURES_SWITCH = 0xF3;
        private const byte POWER_RUMBLE_RIGHT = 0x00;
        private const byte POWER_RUMBLE_LEFT = 0x00;
        private const byte FLASH_ON = 0x00;
        private const byte FLASH_OFF = 0x00;
        private const byte VOL_LEFT = 0x38;
        private const byte VOL_RIGHT = 0x38;
        private const byte VOL_MIC = 0x00;
        //private const byte VOL_SPEAKER = 0x90; // Volume Built-in Speaker / 0x4D == Uppercase M (Mute?)
        private byte VOL_SPEAKER = 0x0D; // Volume Built-in Speaker / 0x4D == Uppercase M (Mute?)
        //private const byte VOL_SPEAKER = 0x00; // Volume Built-in Speaker / 0x4D == Uppercase M (Mute?)
        private const byte LIGHTBAR_RED = 0;
        private const byte LIGHTBAR_GREEN = 0x3D;
        private const byte LIGHTBAR_BLUE = 0;

        //protected const int BT_OUTPUT_REPORT_LENGTH = 334;
        protected const int BT_OUTPUT_REPORT_LENGTH = 334;
        protected const int BT_INPUT_REPORT_LENGTH = 547;

        private string filePath;
        private HidDevice hidDevice;
        private bool exitWorker = false;
        private byte[] outputBTCrc32Head = new byte[] { 0xA2 };
        private ManualResetEventSlim manualReset = new ManualResetEventSlim();

        public PlayerWorker(string filePath, HidDevice hidDevice, byte VOL_SPK = 0x0D)
        {
            this.filePath = filePath;
            this.hidDevice = hidDevice;
            this.VOL_SPEAKER = VOL_SPK;

            byte[] calibration = new byte[41];
            calibration[0] = 0x05;
            hidDevice.readFeatureData(calibration);

            if (!hidDevice.IsFileStreamOpen())
            {
                hidDevice.OpenFileStream(BT_OUTPUT_REPORT_LENGTH);
            }
        }

        public void Playback()
        {
            // Reduce input buffer size
            NativeMethods.HidD_SetNumInputBuffers(hidDevice.safeReadHandle.DangerousGetHandle(),
                3);
            // Allow active sleep times less than 16 ms
            Util.timeBeginPeriod(1);

            Stopwatch testerWatch = new Stopwatch();
            int delayTime = 0;
            double baseFrameTime = 8.0;
            double nextWaitTime = 8.0;

            FileStream openedFs = File.OpenRead(filePath);
            FileStream openFile = File.Open(filePath, FileMode.Open, FileAccess.Read,FileShare.Read);
            //BinaryReader binReader = new BinaryReader(openFile);
            //binReader.ReadBytes(224);
            int lilEndianCounter = 0;
            int bytesRead;
            int bufferSize = BT_OUTPUT_REPORT_LENGTH;
            byte[] outputBuffer = new byte[bufferSize];
            byte[] audioData = new byte[224];
            //while (!exitWorker && (audioData = binReader.ReadBytes(224)).Length > 0)

            // Read first segment before loop
            bytesRead = openFile.Read(audioData, 0, 224);

            long curtime = 0;
            long oldtime = Stopwatch.GetTimestamp();
            testerWatch.Start();
            while (!exitWorker && bytesRead > 0)
            {
                //Array.Clear(outputBuffer, 0, BT_OUTPUT_REPORT_LENGTH);
                //Console.WriteLine(bytesRead);

                int indexBuffer = 81;
                int indexAudioData;
                if (lilEndianCounter > 0xffff)
                {
                    lilEndianCounter = 0;
                }

                outputBuffer[0] = PROTOCOL;
                outputBuffer[1] = MODE_TYPE;
                outputBuffer[2] = TRANSACTION_TYPE;
                outputBuffer[3] = FEATURES_SWITCH;
                outputBuffer[4] = 0x04; // Unknown
                outputBuffer[5] = 0x00;
                outputBuffer[6] = POWER_RUMBLE_RIGHT;
                outputBuffer[7] = POWER_RUMBLE_LEFT;
                outputBuffer[8] = LIGHTBAR_RED;
                outputBuffer[9] = LIGHTBAR_GREEN;
                outputBuffer[10] = LIGHTBAR_BLUE;
                outputBuffer[11] = FLASH_ON;
                outputBuffer[12] = FLASH_OFF;
                outputBuffer[13] = 0x00; outputBuffer[14] = 0x00; outputBuffer[15] = 0x00; outputBuffer[16] = 0x00; /* Start Empty Frames */
                outputBuffer[17] = 0x00; outputBuffer[18] = 0x00; outputBuffer[19] = 0x00; outputBuffer[20] = 0x00; /* Start Empty Frames */
                outputBuffer[21] = VOL_LEFT;
                outputBuffer[22] = VOL_RIGHT;
                outputBuffer[23] = VOL_MIC;
                outputBuffer[24] = VOL_SPEAKER;
                outputBuffer[25] = 0x85;
                //outputBuffer[26] = 0x00; outputBuffer[27] = 0x00; outputBuffer[28] = 0x00; outputBuffer[29] = 0x00; outputBuffer[30] = 0x00; outputBuffer[31] = 0x00; /* Start Empty Frames */
                //outputBuffer[32] = 0x00; outputBuffer[33] = 0x00; outputBuffer[34] = 0x00; outputBuffer[35] = 0x00; outputBuffer[36] = 0x00; outputBuffer[37] = 0x00;
                //outputBuffer[38] = 0x00; outputBuffer[39] = 0x00; outputBuffer[40] = 0x00; outputBuffer[41] = 0x00; outputBuffer[42] = 0x00; outputBuffer[43] = 0x00;
                //outputBuffer[44] = 0x00; outputBuffer[45] = 0x00; outputBuffer[46] = 0x00; outputBuffer[47] = 0x00; outputBuffer[48] = 0x00; outputBuffer[49] = 0x00;
                //outputBuffer[50] = 0x00; outputBuffer[51] = 0x00; outputBuffer[52] = 0x00; outputBuffer[53] = 0x00; outputBuffer[54] = 0x00; outputBuffer[55] = 0x00;
                //outputBuffer[56] = 0x00; outputBuffer[57] = 0x00; outputBuffer[58] = 0x00; outputBuffer[59] = 0x00; outputBuffer[60] = 0x00; outputBuffer[61] = 0x00;
                //outputBuffer[62] = 0x00; outputBuffer[63] = 0x00; outputBuffer[64] = 0x00; outputBuffer[65] = 0x00; outputBuffer[66] = 0x00; outputBuffer[67] = 0x00;
                //outputBuffer[68] = 0x00; outputBuffer[69] = 0x00; outputBuffer[70] = 0x00; outputBuffer[71] = 0x00; outputBuffer[72] = 0x00; outputBuffer[73] = 0x00;
                //outputBuffer[74] = 0x00; outputBuffer[75] = 0x00; outputBuffer[76] = 0x00; outputBuffer[77] = 0x00; /* End Empty Frames */
                outputBuffer[78] = (byte)(lilEndianCounter & 255);
                outputBuffer[79] = (byte)((lilEndianCounter / 256) & 255);
                outputBuffer[80] = 0x02; // 0x02 Speaker Mode On / 0x24 Headset Mode On
                //outputBuffer[80] = 0x24; // 0x02 Speaker Mode On / 0x24 Headset Mode On

                // AUDIO DATA
                for (indexAudioData = 0; indexAudioData < bytesRead; indexAudioData++)
                {
                    outputBuffer[indexBuffer++] = (byte)(audioData[indexAudioData] & 255);
                    //indexBuffer++;
                }

                //outputBuffer[306] = 0x00; outputBuffer[307] = 0x00; outputBuffer[308] = 0x00; outputBuffer[309] = 0x00; outputBuffer[310] = 0x00; outputBuffer[311] = 0x00; /* Start Empty Frames */
                //outputBuffer[312] = 0x00; outputBuffer[313] = 0x00; outputBuffer[314] = 0x00; outputBuffer[315] = 0x00; outputBuffer[316] = 0x00; outputBuffer[317] = 0x00;
                //outputBuffer[318] = 0x00; outputBuffer[319] = 0x00; outputBuffer[320] = 0x00; outputBuffer[321] = 0x00; outputBuffer[322] = 0x00; outputBuffer[323] = 0x00;
                //outputBuffer[324] = 0x00; outputBuffer[325] = 0x00; outputBuffer[326] = 0x00; outputBuffer[327] = 0x00; outputBuffer[328] = 0x00; outputBuffer[329] = 0x00; /* End Empty Frames */
                //outputBuffer[330] = 0x00; outputBuffer[331] = 0x00; outputBuffer[332] = 0x00; outputBuffer[333] = 0x00; /* CRC-32 */

                // Generate CRC-32 data for output buffer and add it to output report
                uint calcCrc32;
                calcCrc32 = ~Crc32Algorithm.Compute(outputBTCrc32Head);
                calcCrc32 = ~Crc32Algorithm.CalculateBasicHash(ref calcCrc32, ref outputBuffer, 0, BT_OUTPUT_REPORT_LENGTH-4);

                outputBuffer[330] = (byte)calcCrc32;
                outputBuffer[331] = (byte)(calcCrc32 >> 8);
                outputBuffer[332] = (byte)(calcCrc32 >> 16);
                outputBuffer[333] = (byte)(calcCrc32 >> 24);

                hidDevice.WriteOutputReportViaInterrupt(outputBuffer, 10);

                lilEndianCounter += 2;
                //hidDevice.WriteAsyncOutputReportViaInterrupt(outputBuffer);
                //hidDevice.WriteOutputReportViaControl(outputBuffer);

                /*while (testerWatch.Elapsed.TotalMilliseconds < 7)
                {
                    Thread.Sleep(0);`
                }
                */
                // Read next section of audio before sleeping
                bytesRead = openFile.Read(audioData, 0, 224);

                // Attempt to sleep for a max of 4 milliseconds. Take file read time into account
                //Thread.Sleep(4);
                double sleepTime = Math.Clamp(4 - testerWatch.Elapsed.TotalMilliseconds, 0.0, 4.0);
                if (sleepTime > nextWaitTime)
                {
                    //sleepTime = 0;
                    sleepTime = Math.Max(0.0, nextWaitTime - 1.0);
                }
                //int sleepTime = Math.Min(Math.Max(0, 5 - (int)testerWatch.Elapsed.TotalMilliseconds), 4);

                int sleepTimeInt = (int)sleepTime;
                if (sleepTimeInt > 0)
                {
                    Thread.Sleep(sleepTimeInt);
                }

                // SpinWait for remaining time before sending next audio packet
                //while (testerWatch.Elapsed.TotalMilliseconds < 7.995)//7.9985)
                while (testerWatch.Elapsed.TotalMilliseconds < nextWaitTime)//7.9985)
                {
                    Thread.SpinWait(500);
                }

                curtime = Stopwatch.GetTimestamp();
                var testelapsed = curtime - oldtime;
                var lastTimeElapsedDouble = testelapsed * (1.0 / Stopwatch.Frequency) * 1000.0;
                var lastTimeElapsed = (long)lastTimeElapsedDouble;
                oldtime = curtime;
                var difftime = (lastTimeElapsedDouble - nextWaitTime);
                nextWaitTime = Math.Max(0.0, baseFrameTime - difftime);
                //Console.WriteLine($"NEXT WAIT: {lastTimeElapsedDouble} {difftime} {nextWaitTime}");
                //Console.WriteLine("ELAPSED TIME");
                //Console.WriteLine(testerWatch.ElapsedMilliseconds);


                // Reset timer
                testerWatch.Restart();
                //Thread.SpinWait(100000000);
            }

            openFile.Close();
            //Console.WriteLine("ELAPSED TIME");
            //Console.WriteLine(testerWatch.ElapsedMilliseconds);
            //openFile.Close();

            Util.timeEndPeriod(1);
        }

        public void StopPlayback()
        {
            exitWorker = true;
        }
    }
}
