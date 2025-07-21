using YoloDotNet;
using YoloDotNet.Enums;
using YoloDotNet.Models;
using YoloDotNet.Extensions;
using SkiaSharp;
using System.Diagnostics;

namespace WinFormsApp1
{

    public partial class Img_Form : Form
    {

        
        private Form1 mainForm;

        public Img_Form(Form1 form)
        {
            InitializeComponent();
            mainForm = form;
        }
        private static string picpath;

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // 设置对话框属性
            openFileDialog.Title = "选择图片文件";
            openFileDialog.Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif|所有文件|*.*";
            openFileDialog.Multiselect = false; // 只能选择一个文件

            
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // 获取选择的文件路径
                string selectedFilePath = openFileDialog.FileName;
                pictureBox1.Image = Image.FromFile(selectedFilePath);
                picpath = selectedFilePath;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {

            label2.Text = "";
            if (mainForm.yolo == null)
            {
                MessageBox.Show("YOLO模型初始化失败！");
                return;
            }

            if (string.IsNullOrEmpty(picpath))
            {
                MessageBox.Show("请先选择图片！");
                return;
            }
            try
            {
                // 加载图片并运行 YOLO 检测
                using var image = SKImage.FromEncodedData(picpath);
                var results = mainForm.yolo.RunObjectDetection(image, confidence: 0.25, iou: 0.7);

                //var resultImage = image.Draw(results , _drawingOptions);
                var resultImage = image.Draw(results);
                // 保存检测结果（可选）
                //resultImage.Save(@"C:....jpg", SKEncodedImageFormat.Jpeg, 80);


                // 将检测后的图片显示到 PictureBox
                using (var ms = new MemoryStream())
                {
                    resultImage.Encode(SKEncodedImageFormat.Jpeg, 80).SaveTo(ms);
                    pictureBox2.Image = Image.FromStream(ms);
                }

                //MessageBox.Show("检测完成！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"检测失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (pictureBox2.Image == null)
            {
                MessageBox.Show("没有可保存的图像！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                string timestamp = DateTime.Now.ToString("HHmmss_fff");
                string savePath = $@"C:\Users\27443\Desktop\res\new_image_{timestamp}.jpg";
                // 确保目录存在
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                // 将图像转换为 Bitmap
                using (Bitmap bitmap = new Bitmap(pictureBox2.Image))
                {
                    bitmap.Save(savePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                }
                label2.Text = "保存成功！";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Img_Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            picpath = "";
            mainForm = null;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string directoryPath = @"C:\Users\27443\Desktop\res";

            // 检查目录是否存在
            if (Directory.Exists(directoryPath))
            {
                // 打开目录（使用系统默认的文件资源管理器）
                Process.Start(new ProcessStartInfo(directoryPath)
                {
                    UseShellExecute = true, // 必须设为 true 才能打开目录
                    Verb = "open" // 动作：打开目录
                });
            }
            else
            {
                // 目录不存在
                MessageBox.Show($"目录不存在：{directoryPath}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
