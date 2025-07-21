namespace WinFormsApp1
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            button1 = new Button();
            pictureBox1 = new PictureBox();
            pictureBox2 = new PictureBox();
            buttonPlayPause = new Button();
            buttonStop = new Button();
            trackBar1 = new TrackBar();
            labelStatus = new Label();
            button2 = new Button();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trackBar1).BeginInit();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(14, 16);
            button1.Margin = new Padding(3, 4, 3, 4);
            button1.Name = "button1";
            button1.Size = new Size(135, 47);
            button1.TabIndex = 0;
            button1.Text = "选择视频";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // pictureBox1
            // 
            pictureBox1.BorderStyle = BorderStyle.FixedSingle;
            pictureBox1.Location = new Point(12, 190);
            pictureBox1.Margin = new Padding(3, 4, 3, 4);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(354, 199);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 1;
            pictureBox1.TabStop = false;
            pictureBox1.Text = "原始视频";
            // 
            // pictureBox2
            // 
            pictureBox2.BorderStyle = BorderStyle.FixedSingle;
            pictureBox2.Location = new Point(372, 88);
            pictureBox2.Margin = new Padding(3, 4, 3, 4);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new Size(595, 399);
            pictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox2.TabIndex = 2;
            pictureBox2.TabStop = false;
            pictureBox2.Text = "检测结果";
            // 
            // buttonPlayPause
            // 
            buttonPlayPause.Enabled = false;
            buttonPlayPause.Location = new Point(166, 16);
            buttonPlayPause.Margin = new Padding(3, 4, 3, 4);
            buttonPlayPause.Name = "buttonPlayPause";
            buttonPlayPause.Size = new Size(135, 47);
            buttonPlayPause.TabIndex = 3;
            buttonPlayPause.Text = "播放";
            buttonPlayPause.UseVisualStyleBackColor = true;
            buttonPlayPause.Click += buttonPlayPause_Click;
            // 
            // buttonStop
            // 
            buttonStop.Enabled = false;
            buttonStop.Location = new Point(320, 16);
            buttonStop.Margin = new Padding(3, 4, 3, 4);
            buttonStop.Name = "buttonStop";
            buttonStop.Size = new Size(135, 47);
            buttonStop.TabIndex = 4;
            buttonStop.Text = "停止";
            buttonStop.UseVisualStyleBackColor = true;
            buttonStop.Click += buttonStop_Click;
            // 
            // trackBar1
            // 
            trackBar1.Location = new Point(14, 422);
            trackBar1.Margin = new Padding(3, 4, 3, 4);
            trackBar1.Maximum = 100;
            trackBar1.Name = "trackBar1";
            trackBar1.Size = new Size(352, 56);
            trackBar1.TabIndex = 5;
            trackBar1.Scroll += trackBar1_Scroll;
            // 
            // labelStatus
            // 
            labelStatus.AutoSize = true;
            labelStatus.Location = new Point(461, 32);
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new Size(41, 20);
            labelStatus.TabIndex = 6;
            labelStatus.Text = "就绪";
            // 
            // button2
            // 
            button2.Location = new Point(604, 15);
            button2.Name = "button2";
            button2.Size = new Size(139, 48);
            button2.TabIndex = 7;
            button2.Text = "进行图片检测";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(991, 544);
            Controls.Add(button2);
            Controls.Add(labelStatus);
            Controls.Add(trackBar1);
            Controls.Add(buttonStop);
            Controls.Add(buttonPlayPause);
            Controls.Add(pictureBox2);
            Controls.Add(pictureBox1);
            Controls.Add(button1);
            Font = new Font("MV Boli", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Margin = new Padding(3, 4, 3, 4);
            Name = "Form1";
            Text = "YOLO视频对象检测";
            FormClosing += Form1_FormClosing;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            ((System.ComponentModel.ISupportInitialize)trackBar1).EndInit();
            ResumeLayout(false);
            PerformLayout();



        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.Button buttonPlayPause;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.TrackBar trackBar1;
        private System.Windows.Forms.Label labelStatus;
        private Button button2;
    }
}