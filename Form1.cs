using YoloDotNet;
using YoloDotNet.Enums;
using YoloDotNet.Models;
using YoloDotNet.Extensions;
using SkiaSharp;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Diagnostics;
namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        public Yolo yolo;
        private VideoCapture videoCapture;
        private System.Windows.Forms.Timer timer;
        private bool isPlaying = false;

        private float confidenceThreshold = 0.25f;
        private float iouThreshold = 0.7f;
        private Bitmap currentDisplayBitmap;


        private Process? ffplayProcess;
        private string? currentVideoPath;
        private bool isAudioPlaying = false;
        private int audioPosition = 0;


        private static DetectionDrawingOptions _drawingOptions = default!;
        public Form1()
        {
            InitializeComponent();
            InitializeYolo();
            InitializeTimer();


        }

        private void InitializeYolo()
        {
            try
            {
                yolo = new Yolo(new YoloOptions
                {

                    OnnxModel = @"",
                    Cuda = true,
                    GpuId = 0,
                    PrimeGpu = true,
                    ImageResize = ImageResize.Proportional
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"YOLOģ�ͳ�ʼ��ʧ��: {ex.Message}", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeTimer()
        {
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 23; 
            timer.Tick += Timer_Tick;
        }




        private void button1_Click(object sender, EventArgs e)
        {
            using var openFileDialog = new OpenFileDialog
            {
                Title = "ѡ����Ƶ�ļ�",
                Filter = "��Ƶ�ļ�|*.mp4;*.avi;*.mov;*.mkv|�����ļ�|*.*",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                OpenVideo(openFileDialog.FileName);
            }
        }
        private double videoFps; // ��Ƶʵ��֡��
        private DateTime? videoStartTime; // ��Ƶ��ʼ���ŵ�ʱ��
        private int totalFrames; // ��Ƶ��֡��
        private void OpenVideo(string filePath)
        {
            try
            {
                CleanupVideoResources();
                currentVideoPath = filePath;

                videoCapture = new VideoCapture(filePath);

                if (!videoCapture.IsOpened())
                {
                    MessageBox.Show("�޷�����Ƶ�ļ���", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // ���»�ȡ����Ƶ��֡�ʺ���֡�����ؼ�����
                videoFps = videoCapture.Get(VideoCaptureProperties.Fps);
                totalFrames = (int)videoCapture.Get(VideoCaptureProperties.FrameCount);

                // ��̬����Timer�����������Ƶ֡�ʼ���ÿ֡Ӧ����ĺ�������
                timer.Interval = (int)(1000 / videoFps);

                // ����UI
                labelStatus.Text = "��Ƶ�Ѽ���";
                buttonPlayPause.Enabled = true;
                buttonStop.Enabled = true;
                trackBar1.Maximum = totalFrames;
                trackBar1.Value = 0;
                isPlaying = false;

                UpdatePlayPauseButtonText();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"����Ƶʧ��: {ex.Message}", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CleanupVideoResources()
        {
            timer?.Stop();
            StopAudio();

            if (videoCapture != null)
            {
                videoCapture.Release();
                videoCapture.Dispose();
                videoCapture = null;
            }

            if (currentDisplayBitmap != null)
            {
                currentDisplayBitmap.Dispose();
                currentDisplayBitmap = null;
            }
            // ��ջ�����ʾ
            pictureBox1.Image?.Dispose();
            pictureBox2.Image?.Dispose();
            pictureBox1.Image = null;
            pictureBox2.Image = null;
            // ��������״̬�������ؼ�����
            videoStartTime = null;       // ��Ƶ��ʼʱ������
            videoFps = 0;                // ��Ƶ֡������
            totalFrames = 0;             // ��֡������
            audioPosition = 0;           // ��Ƶλ������
            isPlaying = false;           // ����״̬����
            currentVideoPath = null;     // ��ǰ��Ƶ·������
        }

        private void buttonPlayPause_Click(object sender, EventArgs e)
        {

            isPlaying = !isPlaying;
            UpdatePlayPauseButtonText();
            if (isPlaying)
            {
                if (currentVideoPath != null)
                {
                    // ��¼��ʼʱ�䣨������ͣ�ָ��������ƫ������
                    if (videoStartTime == null)
                    {
                        videoStartTime = DateTime.Now;
                    }
                    else
                    {
                        // ����ͣ�ָ�ʱ��������ʼʱ�䣨��ȥ�Ѳ��ŵ�ʱ�䣩
                        double elapsed = audioPosition / 1000.0; // �Ѳ��ŵ�����
                        videoStartTime = DateTime.Now - TimeSpan.FromSeconds(elapsed);
                    }
                    StartAudio();
                }
                timer.Start();
                labelStatus.Text = "���ڲ��Ų����...";
            }
            else
            {
                timer.Stop();
                PauseAudio();
                labelStatus.Text = "����ͣ";
            }
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            if (videoCapture != null && videoCapture.IsOpened())
            {
                CleanupVideoResources();
                labelStatus.Text = "��ֹͣ";
                buttonPlayPause.Enabled = false;
                buttonStop.Enabled = false;
                trackBar1.Value = 0;
                isPlaying = false;
                UpdatePlayPauseButtonText();
            }
        }

        private void UpdatePlayPauseButtonText()
        {
            buttonPlayPause.Text = isPlaying ? "��ͣ" : "����";
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (videoCapture == null || !videoCapture.IsOpened() || !videoStartTime.HasValue)
            {
                timer.Stop();
                return;
            }
            // ����Ӳ��ſ�ʼ�����ڵ�ʱ�䣨�룩
            TimeSpan elapsed = DateTime.Now - videoStartTime.Value;
            double elapsedSeconds = elapsed.TotalSeconds;

            // ���㵱ǰӦ���ŵ�֡������ʱ���֡�ʣ�
            int expectedFrame = (int)(elapsedSeconds * videoFps);
            // ����֡��Χ����������֡������С��0��
            expectedFrame = Math.Clamp(expectedFrame, 0, totalFrames - 1);

            // ��ת������֡
            videoCapture.Set(VideoCaptureProperties.PosFrames, expectedFrame);

            using var frame = new Mat();
            if (!videoCapture.Read(frame))
            {
                // ��Ƶ����
                CleanupVideoResources();
                labelStatus.Text = "��Ƶ�������";
                buttonPlayPause.Enabled = false;
                isPlaying = false;
                UpdatePlayPauseButtonText();
                return;
            }

            // ���½�����
            if (trackBar1.Value != expectedFrame)
                trackBar1.Value = expectedFrame;
            // ͬ����Ƶλ��
            audioPosition = (int)(elapsedSeconds * 1000); // ת��Ϊ����
            SyncAudioWithVideo();

            try
            {
                ProcessFrame(frame);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"����֡ʱ����: {ex.Message}", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
                CleanupVideoResources();
                labelStatus.Text = "���������ֹͣ";
                buttonPlayPause.Enabled = false;
                isPlaying = false;
                UpdatePlayPauseButtonText();
            }
        }
        // ������Ƶͬ������
        private void SyncAudioWithVideo()
        {
            if (isAudioPlaying && ffplayProcess != null && !ffplayProcess.HasExited)
            {
                // ������ƵӦ����λ�ã��룩
                double expectedAudioPos = audioPosition / 1000.0;
                // ����seek����ͬ����Ƶ
                SendCommandToFfplay($"seek {expectedAudioPos:F1}\n");
            }
        }
        private void ProcessFrame(Mat frame)
        {
            // ��ʾԭʼ֡
            using var sourceBitmap = BitmapConverter.ToBitmap(frame);
            var displayBitmap = new Bitmap(sourceBitmap);

            // �ͷ�֮ǰ����ʾλͼ
            if (currentDisplayBitmap != null)
            {
                currentDisplayBitmap.Dispose();
            }

            currentDisplayBitmap = displayBitmap;
            pictureBox1.Image = displayBitmap;

            // ת��ΪSKImage����YOLO���
            using (var systemBitmap = BitmapConverter.ToBitmap(frame))
            using (var memoryStream = new MemoryStream())
            {
                systemBitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                memoryStream.Position = 0;
                using (var skBitmap = SKBitmap.Decode(memoryStream))
                using (var skImage = SKImage.FromBitmap(skBitmap))
                {
                    // ���м��
                    var results = yolo.RunObjectDetection(skImage, confidence: confidenceThreshold, iou: iouThreshold);

                    // ���Ƽ����
                    using (var resultImage = skImage.Draw(results, _drawingOptions))
                    {

                        // ��ʾ�����
                        using (var ms = new MemoryStream())
                        {
                            SetDrawingOptions();
                            resultImage.Encode(SKEncodedImageFormat.Jpeg, 80).SaveTo(ms);
                            pictureBox2.Image?.Dispose();
                            pictureBox2.Image = Image.FromStream(ms);
                        }
                    }
                }
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            if (videoCapture != null && videoCapture.IsOpened())
            {
                videoCapture.Set(VideoCaptureProperties.PosFrames, trackBar1.Value);

                // ͬ����Ƶλ��
                double fps = videoCapture.Get(VideoCaptureProperties.Fps);
                audioPosition = (int)(trackBar1.Value / fps * 1000); // ת��Ϊ����

                if (isAudioPlaying && ffplayProcess != null)
                {
                    // ��ͣ��Ƶ
                    SendCommandToFfplay("p");

                    // ��ת����λ��
                    SendCommandToFfplay($"seek {audioPosition / 1000.0:F1}\n");

                    // ���֮ǰ�ǲ���״̬����ָ�����
                    if (isPlaying)
                    {
                        SendCommandToFfplay("p");
                    }
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            CleanupVideoResources();
            yolo?.Dispose();
        }

        //ͬ����Ƶ
        private void StartAudio()
        {
            if (string.IsNullOrEmpty(currentVideoPath))
                return;

            if (ffplayProcess == null || ffplayProcess.HasExited)
            {
                try
                {
                    // ����ʱ��
                    double startTime = 0;
                    if (videoCapture != null && videoCapture.IsOpened())
                    {
                        double fps = videoCapture.Get(VideoCaptureProperties.Fps);
                        int currentFrame = (int)videoCapture.Get(VideoCaptureProperties.PosFrames);
                        startTime = currentFrame / fps;
                    }

                    // ffplay������Ƶ
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = "ffplay",
                        Arguments = $"-nodisp -autoexit -ss {startTime:F3} \"{currentVideoPath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    ffplayProcess = new Process { StartInfo = startInfo };
                    ffplayProcess.Start();

                    isAudioPlaying = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"������Ƶ����ʧ��: {ex.Message}", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (!isAudioPlaying)
            {
                //�ָ�����
                SendCommandToFfplay("p");
                isAudioPlaying = true;
            }
        }

        private void PauseAudio()
        {
            if (isAudioPlaying && ffplayProcess != null && !ffplayProcess.HasExited)
            {
                SendCommandToFfplay("p");
                isAudioPlaying = false;
            }
        }

        private void StopAudio()
        {
            if (ffplayProcess != null)
            {
                try
                {
                    if (!ffplayProcess.HasExited)
                    {
                        SendCommandToFfplay("q");
                        ffplayProcess.WaitForExit(1000);

                        if (!ffplayProcess.HasExited)
                        {
                            ffplayProcess.Kill();
                        }
                    }
                }
                catch { }

                ffplayProcess.Dispose();
                ffplayProcess = null;
                isAudioPlaying = false;
            }
        }

        private void SendCommandToFfplay(string command)
        {
            if (ffplayProcess != null && !ffplayProcess.HasExited && !ffplayProcess.StandardInput.BaseStream.CanWrite)
            {
                try
                {
                    ffplayProcess.StandardInput.WriteLine(command);
                }
                catch { }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Img_Form img_Form = new Img_Form(this);
            img_Form.ShowDialog();
        }

        //����
        private static void SetDrawingOptions()
        {
            // ���û���ѡ��
            _drawingOptions = new DetectionDrawingOptions
            {
                DrawBoundingBoxes = true,
                DrawConfidenceScore = false,
                DrawLabels = true,
                EnableFontShadow = true,

                // SKTypeface�����������ı���Ⱦ������
                // SKTypeface.Defaultʹ��ϵͳĬ������
                // ��Ҫ�����Զ������壺
                //   - ʹ��SKTypeface.FromFamilyName("����������", SKFontStyle)�����������Ƽ��أ����Ѱ�װ��
                //   - ʹ��SKTypeface.FromFile("�����ļ�·��.ttf")ֱ�Ӵ��ļ���������
                // ʾ����
                //   Font = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal)
                //   Font = SKTypeface.FromFile("C:\\Fonts\\�Զ�������.ttf")
                Font = SKTypeface.Default,

                FontSize = 24,
                FontColor = SKColors.White,
                DrawLabelBackground = true,
                EnableDynamicScaling = true,
                BorderThickness = 8,

                // Ĭ������£�YoloDotNet���Զ�Ϊ�߽�������ɫ
                // ��Ҫ������ЩĬ����ɫ���ɶ����Լ���ʮ��������ɫ��������
                // �����е�ÿ��Ԫ�ض�Ӧģ���е��������
                // ʾ����
                //   BoundingBoxHexColors = ["#00ff00", "#547457", ...] // �����IDָ����ɫ

                BoundingBoxOpacity = 128,

                // ����ѡ���������ø���Ŀ���β�������ڿ��ӻ�
                // Ŀ����һϵ��֡��ͼ���е��˶�·��
                // ֻ�����ø���ʱ������ʹ��SortTracker����β�����Ʋ���Ч
                // ����VideoStreamʾ���н�������ʾ

                // DrawTrackedTail = false,
                // TailPaintColorEnd = new(),
                // TailPaintColorStart = new(),
                // TailThickness = 0,
            };
        }
    }
}

