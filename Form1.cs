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
                MessageBox.Show($"YOLO模型初始化失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                Title = "选择视频文件",
                Filter = "视频文件|*.mp4;*.avi;*.mov;*.mkv|所有文件|*.*",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                OpenVideo(openFileDialog.FileName);
            }
        }
        private double videoFps; // 视频实际帧率
        private DateTime? videoStartTime; // 视频开始播放的时间
        private int totalFrames; // 视频总帧数
        private void OpenVideo(string filePath)
        {
            try
            {
                CleanupVideoResources();
                currentVideoPath = filePath;

                videoCapture = new VideoCapture(filePath);

                if (!videoCapture.IsOpened())
                {
                    MessageBox.Show("无法打开视频文件！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // 重新获取新视频的帧率和总帧数（关键！）
                videoFps = videoCapture.Get(VideoCaptureProperties.Fps);
                totalFrames = (int)videoCapture.Get(VideoCaptureProperties.FrameCount);

                // 动态设置Timer间隔（根据视频帧率计算每帧应间隔的毫秒数）
                timer.Interval = (int)(1000 / videoFps);

                // 更新UI
                labelStatus.Text = "视频已加载";
                buttonPlayPause.Enabled = true;
                buttonStop.Enabled = true;
                trackBar1.Maximum = totalFrames;
                trackBar1.Value = 0;
                isPlaying = false;

                UpdatePlayPauseButtonText();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开视频失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            // 清空画面显示
            pictureBox1.Image?.Dispose();
            pictureBox2.Image?.Dispose();
            pictureBox1.Image = null;
            pictureBox2.Image = null;
            // 重置所有状态变量（关键！）
            videoStartTime = null;       // 视频开始时间重置
            videoFps = 0;                // 视频帧率重置
            totalFrames = 0;             // 总帧数重置
            audioPosition = 0;           // 音频位置重置
            isPlaying = false;           // 播放状态重置
            currentVideoPath = null;     // 当前视频路径重置
        }

        private void buttonPlayPause_Click(object sender, EventArgs e)
        {

            isPlaying = !isPlaying;
            UpdatePlayPauseButtonText();
            if (isPlaying)
            {
                if (currentVideoPath != null)
                {
                    // 记录开始时间（若从暂停恢复，需计算偏移量）
                    if (videoStartTime == null)
                    {
                        videoStartTime = DateTime.Now;
                    }
                    else
                    {
                        // 从暂停恢复时，修正开始时间（减去已播放的时间）
                        double elapsed = audioPosition / 1000.0; // 已播放的秒数
                        videoStartTime = DateTime.Now - TimeSpan.FromSeconds(elapsed);
                    }
                    StartAudio();
                }
                timer.Start();
                labelStatus.Text = "正在播放并检测...";
            }
            else
            {
                timer.Stop();
                PauseAudio();
                labelStatus.Text = "已暂停";
            }
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            if (videoCapture != null && videoCapture.IsOpened())
            {
                CleanupVideoResources();
                labelStatus.Text = "已停止";
                buttonPlayPause.Enabled = false;
                buttonStop.Enabled = false;
                trackBar1.Value = 0;
                isPlaying = false;
                UpdatePlayPauseButtonText();
            }
        }

        private void UpdatePlayPauseButtonText()
        {
            buttonPlayPause.Text = isPlaying ? "暂停" : "播放";
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (videoCapture == null || !videoCapture.IsOpened() || !videoStartTime.HasValue)
            {
                timer.Stop();
                return;
            }
            // 计算从播放开始到现在的时间（秒）
            TimeSpan elapsed = DateTime.Now - videoStartTime.Value;
            double elapsedSeconds = elapsed.TotalSeconds;

            // 计算当前应播放的帧（根据时间和帧率）
            int expectedFrame = (int)(elapsedSeconds * videoFps);
            // 限制帧范围（不超过总帧数，不小于0）
            expectedFrame = Math.Clamp(expectedFrame, 0, totalFrames - 1);

            // 跳转到期望帧
            videoCapture.Set(VideoCaptureProperties.PosFrames, expectedFrame);

            using var frame = new Mat();
            if (!videoCapture.Read(frame))
            {
                // 视频结束
                CleanupVideoResources();
                labelStatus.Text = "视频播放完毕";
                buttonPlayPause.Enabled = false;
                isPlaying = false;
                UpdatePlayPauseButtonText();
                return;
            }

            // 更新进度条
            if (trackBar1.Value != expectedFrame)
                trackBar1.Value = expectedFrame;
            // 同步音频位置
            audioPosition = (int)(elapsedSeconds * 1000); // 转换为毫秒
            SyncAudioWithVideo();

            try
            {
                ProcessFrame(frame);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"处理帧时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                CleanupVideoResources();
                labelStatus.Text = "处理出错，已停止";
                buttonPlayPause.Enabled = false;
                isPlaying = false;
                UpdatePlayPauseButtonText();
            }
        }
        // 新增音频同步方法
        private void SyncAudioWithVideo()
        {
            if (isAudioPlaying && ffplayProcess != null && !ffplayProcess.HasExited)
            {
                // 计算音频应处的位置（秒）
                double expectedAudioPos = audioPosition / 1000.0;
                // 发送seek命令同步音频
                SendCommandToFfplay($"seek {expectedAudioPos:F1}\n");
            }
        }
        private void ProcessFrame(Mat frame)
        {
            // 显示原始帧
            using var sourceBitmap = BitmapConverter.ToBitmap(frame);
            var displayBitmap = new Bitmap(sourceBitmap);

            // 释放之前的显示位图
            if (currentDisplayBitmap != null)
            {
                currentDisplayBitmap.Dispose();
            }

            currentDisplayBitmap = displayBitmap;
            pictureBox1.Image = displayBitmap;

            // 转换为SKImage进行YOLO检测
            using (var systemBitmap = BitmapConverter.ToBitmap(frame))
            using (var memoryStream = new MemoryStream())
            {
                systemBitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                memoryStream.Position = 0;
                using (var skBitmap = SKBitmap.Decode(memoryStream))
                using (var skImage = SKImage.FromBitmap(skBitmap))
                {
                    // 运行检测
                    var results = yolo.RunObjectDetection(skImage, confidence: confidenceThreshold, iou: iouThreshold);

                    // 绘制检测结果
                    using (var resultImage = skImage.Draw(results, _drawingOptions))
                    {

                        // 显示检测结果
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

                // 同步音频位置
                double fps = videoCapture.Get(VideoCaptureProperties.Fps);
                audioPosition = (int)(trackBar1.Value / fps * 1000); // 转换为毫秒

                if (isAudioPlaying && ffplayProcess != null)
                {
                    // 暂停音频
                    SendCommandToFfplay("p");

                    // 跳转到新位置
                    SendCommandToFfplay($"seek {audioPosition / 1000.0:F1}\n");

                    // 如果之前是播放状态，则恢复播放
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

        //同步音频
        private void StartAudio()
        {
            if (string.IsNullOrEmpty(currentVideoPath))
                return;

            if (ffplayProcess == null || ffplayProcess.HasExited)
            {
                try
                {
                    // 计算时间
                    double startTime = 0;
                    if (videoCapture != null && videoCapture.IsOpened())
                    {
                        double fps = videoCapture.Get(VideoCaptureProperties.Fps);
                        int currentFrame = (int)videoCapture.Get(VideoCaptureProperties.PosFrames);
                        startTime = currentFrame / fps;
                    }

                    // ffplay播放音频
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
                    MessageBox.Show($"启动音频播放失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (!isAudioPlaying)
            {
                //恢复播放
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

        //设置
        private static void SetDrawingOptions()
        {
            // 设置绘制选项
            _drawingOptions = new DetectionDrawingOptions
            {
                DrawBoundingBoxes = true,
                DrawConfidenceScore = false,
                DrawLabels = true,
                EnableFontShadow = true,

                // SKTypeface定义了用于文本渲染的字体
                // SKTypeface.Default使用系统默认字体
                // 若要加载自定义字体：
                //   - 使用SKTypeface.FromFamilyName("字体族名称", SKFontStyle)按字体族名称加载（需已安装）
                //   - 使用SKTypeface.FromFile("字体文件路径.ttf")直接从文件加载字体
                // 示例：
                //   Font = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal)
                //   Font = SKTypeface.FromFile("C:\\Fonts\\自定义字体.ttf")
                Font = SKTypeface.Default,

                FontSize = 24,
                FontColor = SKColors.White,
                DrawLabelBackground = true,
                EnableDynamicScaling = true,
                BorderThickness = 8,

                // 默认情况下，YoloDotNet会自动为边界框分配颜色
                // 若要覆盖这些默认颜色，可定义自己的十六进制颜色代码数组
                // 数组中的每个元素对应模型中的类别索引
                // 示例：
                //   BoundingBoxHexColors = ["#00ff00", "#547457", ...] // 按类别ID指定颜色

                BoundingBoxOpacity = 128,

                // 以下选项用于配置跟踪目标的尾部，用于可视化
                // 目标在一系列帧或图像中的运动路径
                // 只有启用跟踪时（例如使用SortTracker），尾部绘制才有效
                // 这在VideoStream示例中进行了演示

                // DrawTrackedTail = false,
                // TailPaintColorEnd = new(),
                // TailPaintColorStart = new(),
                // TailThickness = 0,
            };
        }
    }
}

