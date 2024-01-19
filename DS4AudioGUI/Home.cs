using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace DS4AudioGUI
{
    public partial class Home : Form
    {
        private string filePath = "";
        private byte volume = 0;
        private bool fileSelected = false;
        private bool isProcessStarted = false;
        private string dualPodShockNetPath = "DualPodShockNet.exe";
        private Process ffmpeg;
        private Process ds4;
        private CancellationTokenSource cancellationTokenSource;
        private int outputIndex = 0;

        public Home()
        {
            InitializeComponent();
            cancellationTokenSource = new CancellationTokenSource();
        }

        private void buttonSelect_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "MP3 Audio |*.mp3";
            openFileDialog.Title = "Choose MP3 Audio";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                filePath = openFileDialog.FileName;
                textBoxPath.Text = filePath;
                fileSelected = true;
            }
        }

        private void buttonPlay_Click(object sender, EventArgs e)
        {
            volume = Convert.ToByte(trackBarVolume.Value * 10);

            if (fileSelected)
            {
                ClearTempFiles();
                StartFfmpegAudioProcess();
            }
            else
            {
                MessageBox.Show("You must select an audio file to play.", "DS4 Audio GUI", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RecordAudio(object state)
        {
            var cancellationToken = (CancellationToken)state;

            while (!cancellationToken.IsCancellationRequested)
            {
                // Başka bir thread içinde ffmpeg işlemini başlat
                StartFfmpegProcess();

                // Belirli aralıklarla beklemek için Thread.Sleep kullanabilirsiniz
                Thread.Sleep(TimeSpan.FromSeconds(0));
            }
        }

        private void StartFfmpegProcess()
        {
            string outputFileName = $"output_{outputIndex++}.sbc";

            ProcessStartInfo ffmpegStartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = $"-f dshow -i audio=\"virtual-audio-capturer\" -ac 2 -ar 16000 -c:a sbc -b:a 224k -t 0.5 \"{outputFileName}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            ffmpeg = new Process
            {
                StartInfo = ffmpegStartInfo
            };

            ffmpeg.Start();

            // clear temp files
            ffmpeg.WaitForExit();
            StartDs4SysProcess(cancellationTokenSource.Token);
        }

        private void StartFfmpegAudioProcess()
        {
            string outputFileName = "output.sbc";

            ProcessStartInfo ffmpegStartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = $"-i \"{filePath}\" -ac 2 -ar 32k -c:a sbc -b:a 224k \"{outputFileName}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            ffmpeg = new Process
            {
                StartInfo = ffmpegStartInfo
            };

            ffmpeg.Start();

            // clear temp files
            ffmpeg.WaitForExit();
            StartDs4Process(cancellationTokenSource.Token);
        }

        private void StartDs4SysProcess(object state)
        {
            var cancellationToken = (CancellationToken)state;
            isProcessStarted = true;

            ProcessStartInfo ds4StartInfo = new ProcessStartInfo
            {
                FileName = dualPodShockNetPath,
                Arguments = $"--file \"output_{outputIndex - 1}.sbc\" --sound {volume}",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            ds4 = new Process
            {
                StartInfo = ds4StartInfo
            };

            ds4.Start();
            ds4.WaitForExit();
        }

        private void StartDs4Process(object state)
        {
            var cancellationToken = (CancellationToken)state;
            isProcessStarted = true;

            ProcessStartInfo ds4StartInfo = new ProcessStartInfo
            {
                FileName = dualPodShockNetPath,
                Arguments = $"--file \"output.sbc\" --sound {volume}",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            ds4 = new Process
            {
                StartInfo = ds4StartInfo
            };

            ds4.Start();
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            if (isProcessStarted)
            {
                ds4.Kill();
                isProcessStarted= false;
            }
        }

        private void trackBarVolume_ValueChanged(object sender, EventArgs e)
        {
            if(trackBarVolume.Value == 0)
            {
                volume = 0x00;
            }

            else
            {
                volume = Convert.ToByte(trackBarVolume.Value * 10);
            }
            
            labelVolume.Text = "Volume: " + (trackBarVolume.Value * 10) + "%";
        }

        private void ClearTempFiles()
        {
            // clear temp files
            string[] files = Directory.GetFiles(".", "output_*.sbc");
            foreach (string file in files)
            {
                File.Delete(file);
            }

            if (File.Exists("output.sbc"))
            {
                File.Delete("output.sbc");
            }
        }

        private void toolStripAbout_Click(object sender, EventArgs e)
        {
            About about = new About();
            about.ShowDialog();
        }

        private void buttonSysAudio_Click(object sender, EventArgs e)
        {
            isProcessStarted = true;
            volume = Convert.ToByte(trackBarVolume.Value * 10);

            cancellationTokenSource = new CancellationTokenSource();

            // clear temp files
            ClearTempFiles();

            ThreadPool.QueueUserWorkItem(RecordAudio, cancellationTokenSource.Token);
        }

        private void Home_Load(object sender, EventArgs e)
        {
            labelVolume.Text = "Volume: " + (trackBarVolume.Value * 10) + "%";
        }

        private void Home_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isProcessStarted)
            {
                ds4.Kill();
            }

            if (File.Exists("output.sbc"))
            {
                File.Delete("output.sbc");
            }
        }
    }
}
