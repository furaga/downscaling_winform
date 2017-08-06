namespace downscaling_winform
{
    partial class Form1
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileFToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openImageOToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.bicubic_a_TBox = new System.Windows.Forms.TextBox();
            this.boxRButton = new System.Windows.Forms.RadioButton();
            this.perceptualRButton = new System.Windows.Forms.RadioButton();
            this.bicubicRButton = new System.Windows.Forms.RadioButton();
            this.subsamplingRButton = new System.Windows.Forms.RadioButton();
            this.inputRButton = new System.Windows.Forms.RadioButton();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.canvas = new System.Windows.Forms.PictureBox();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.label1 = new System.Windows.Forms.Label();
            this.gaussianRButton = new System.Windows.Forms.RadioButton();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.canvas)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileFToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(12, 5, 0, 5);
            this.menuStrip1.Size = new System.Drawing.Size(940, 31);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileFToolStripMenuItem
            // 
            this.fileFToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openImageOToolStripMenuItem});
            this.fileFToolStripMenuItem.Font = new System.Drawing.Font("Yu Gothic UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.fileFToolStripMenuItem.Name = "fileFToolStripMenuItem";
            this.fileFToolStripMenuItem.Size = new System.Drawing.Size(57, 21);
            this.fileFToolStripMenuItem.Text = "File (&F)";
            // 
            // openImageOToolStripMenuItem
            // 
            this.openImageOToolStripMenuItem.Name = "openImageOToolStripMenuItem";
            this.openImageOToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openImageOToolStripMenuItem.Size = new System.Drawing.Size(217, 22);
            this.openImageOToolStripMenuItem.Text = "Open Image (&O)";
            this.openImageOToolStripMenuItem.Click += new System.EventHandler(this.openImageOToolStripMenuItem_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.Location = new System.Drawing.Point(0, 31);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.gaussianRButton);
            this.splitContainer1.Panel1.Controls.Add(this.label1);
            this.splitContainer1.Panel1.Controls.Add(this.bicubic_a_TBox);
            this.splitContainer1.Panel1.Controls.Add(this.boxRButton);
            this.splitContainer1.Panel1.Controls.Add(this.perceptualRButton);
            this.splitContainer1.Panel1.Controls.Add(this.bicubicRButton);
            this.splitContainer1.Panel1.Controls.Add(this.subsamplingRButton);
            this.splitContainer1.Panel1.Controls.Add(this.inputRButton);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(940, 541);
            this.splitContainer1.SplitterDistance = 167;
            this.splitContainer1.SplitterWidth = 8;
            this.splitContainer1.TabIndex = 1;
            // 
            // bicubic_a_TBox
            // 
            this.bicubic_a_TBox.Font = new System.Drawing.Font("メイリオ", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.bicubic_a_TBox.Location = new System.Drawing.Point(72, 122);
            this.bicubic_a_TBox.Name = "bicubic_a_TBox";
            this.bicubic_a_TBox.Size = new System.Drawing.Size(76, 24);
            this.bicubic_a_TBox.TabIndex = 5;
            this.bicubic_a_TBox.Text = "-1.0";
            this.bicubic_a_TBox.TextChanged += new System.EventHandler(this.bicubic_a_TBox_TextChanged);
            // 
            // boxRButton
            // 
            this.boxRButton.AutoSize = true;
            this.boxRButton.Font = new System.Drawing.Font("メイリオ", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.boxRButton.Location = new System.Drawing.Point(15, 58);
            this.boxRButton.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.boxRButton.Name = "boxRButton";
            this.boxRButton.Size = new System.Drawing.Size(50, 24);
            this.boxRButton.TabIndex = 4;
            this.boxRButton.TabStop = true;
            this.boxRButton.Text = "box";
            this.boxRButton.UseVisualStyleBackColor = true;
            this.boxRButton.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
            // 
            // perceptualRButton
            // 
            this.perceptualRButton.AutoSize = true;
            this.perceptualRButton.Font = new System.Drawing.Font("メイリオ", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.perceptualRButton.Location = new System.Drawing.Point(15, 156);
            this.perceptualRButton.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.perceptualRButton.Name = "perceptualRButton";
            this.perceptualRButton.Size = new System.Drawing.Size(95, 24);
            this.perceptualRButton.TabIndex = 3;
            this.perceptualRButton.TabStop = true;
            this.perceptualRButton.Text = "perceptual";
            this.perceptualRButton.UseVisualStyleBackColor = true;
            this.perceptualRButton.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
            // 
            // bicubicRButton
            // 
            this.bicubicRButton.AutoSize = true;
            this.bicubicRButton.Font = new System.Drawing.Font("メイリオ", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.bicubicRButton.Location = new System.Drawing.Point(15, 99);
            this.bicubicRButton.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.bicubicRButton.Name = "bicubicRButton";
            this.bicubicRButton.Size = new System.Drawing.Size(71, 24);
            this.bicubicRButton.TabIndex = 2;
            this.bicubicRButton.TabStop = true;
            this.bicubicRButton.Text = "bicubic";
            this.bicubicRButton.UseVisualStyleBackColor = true;
            this.bicubicRButton.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
            // 
            // subsamplingRButton
            // 
            this.subsamplingRButton.AutoSize = true;
            this.subsamplingRButton.Font = new System.Drawing.Font("メイリオ", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.subsamplingRButton.Location = new System.Drawing.Point(15, 40);
            this.subsamplingRButton.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.subsamplingRButton.Name = "subsamplingRButton";
            this.subsamplingRButton.Size = new System.Drawing.Size(105, 24);
            this.subsamplingRButton.TabIndex = 1;
            this.subsamplingRButton.TabStop = true;
            this.subsamplingRButton.Text = "subsampling";
            this.subsamplingRButton.UseVisualStyleBackColor = true;
            this.subsamplingRButton.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
            // 
            // inputRButton
            // 
            this.inputRButton.AutoSize = true;
            this.inputRButton.Font = new System.Drawing.Font("メイリオ", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.inputRButton.Location = new System.Drawing.Point(15, 20);
            this.inputRButton.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.inputRButton.Name = "inputRButton";
            this.inputRButton.Size = new System.Drawing.Size(59, 24);
            this.inputRButton.TabIndex = 0;
            this.inputRButton.TabStop = true;
            this.inputRButton.Text = "input";
            this.inputRButton.UseVisualStyleBackColor = true;
            this.inputRButton.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.canvas);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.richTextBox1);
            this.splitContainer2.Size = new System.Drawing.Size(765, 541);
            this.splitContainer2.SplitterDistance = 391;
            this.splitContainer2.SplitterWidth = 9;
            this.splitContainer2.TabIndex = 0;
            // 
            // canvas
            // 
            this.canvas.BackColor = System.Drawing.Color.White;
            this.canvas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.canvas.Location = new System.Drawing.Point(0, 0);
            this.canvas.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.canvas.Name = "canvas";
            this.canvas.Size = new System.Drawing.Size(765, 391);
            this.canvas.TabIndex = 0;
            this.canvas.TabStop = false;
            this.canvas.Paint += new System.Windows.Forms.PaintEventHandler(this.canvas_Paint);
            // 
            // richTextBox1
            // 
            this.richTextBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBox1.Location = new System.Drawing.Point(0, 0);
            this.richTextBox1.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(765, 141);
            this.richTextBox1.TabIndex = 0;
            this.richTextBox1.Text = "";
            // 
            // openFileDialog
            // 
            this.openFileDialog.FileName = "openFileDialog";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("メイリオ", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label1.Location = new System.Drawing.Point(40, 123);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(31, 20);
            this.label1.TabIndex = 6;
            this.label1.Text = "a =";
            // 
            // gaussianRButton
            // 
            this.gaussianRButton.AutoSize = true;
            this.gaussianRButton.Font = new System.Drawing.Font("メイリオ", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.gaussianRButton.Location = new System.Drawing.Point(15, 78);
            this.gaussianRButton.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.gaussianRButton.Name = "gaussianRButton";
            this.gaussianRButton.Size = new System.Drawing.Size(82, 24);
            this.gaussianRButton.TabIndex = 7;
            this.gaussianRButton.TabStop = true;
            this.gaussianRButton.Text = "gaussian";
            this.gaussianRButton.UseVisualStyleBackColor = true;
            this.gaussianRButton.CheckedChanged += new System.EventHandler(this.radioButton_CheckedChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 28F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(940, 572);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.menuStrip1);
            this.Font = new System.Drawing.Font("メイリオ", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            this.Name = "Form1";
            this.Text = "downscaling_winform";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.canvas)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileFToolStripMenuItem;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.RadioButton perceptualRButton;
        private System.Windows.Forms.RadioButton bicubicRButton;
        private System.Windows.Forms.RadioButton subsamplingRButton;
        private System.Windows.Forms.RadioButton inputRButton;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.PictureBox canvas;
        private System.Windows.Forms.ToolStripMenuItem openImageOToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.RadioButton boxRButton;
        private System.Windows.Forms.TextBox bicubic_a_TBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton gaussianRButton;
    }
}

