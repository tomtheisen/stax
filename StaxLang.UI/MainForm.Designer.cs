namespace StaxLang.UI {
    partial class MainForm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.Windows.Forms.Label InputLabel;
            System.Windows.Forms.Label OutputLabel;
            System.Windows.Forms.Label ProgramLabel;
            this.InputTextbox = new System.Windows.Forms.TextBox();
            this.OutputTextbox = new System.Windows.Forms.TextBox();
            this.MainSplit = new System.Windows.Forms.SplitContainer();
            this.ProgramSizeLabel = new System.Windows.Forms.Label();
            this.ProgramTextbox = new System.Windows.Forms.TextBox();
            this.PackButton = new System.Windows.Forms.Button();
            this.MainStatus = new System.Windows.Forms.StatusStrip();
            this.StepCountLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.ElapsedTimeLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.RunButton = new System.Windows.Forms.Button();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.AnnotateMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.TimeoutMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stringCompressorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.integerArrayCrammerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            InputLabel = new System.Windows.Forms.Label();
            OutputLabel = new System.Windows.Forms.Label();
            ProgramLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.MainSplit)).BeginInit();
            this.MainSplit.Panel1.SuspendLayout();
            this.MainSplit.Panel2.SuspendLayout();
            this.MainSplit.SuspendLayout();
            this.MainStatus.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // InputLabel
            // 
            InputLabel.AutoSize = true;
            InputLabel.Location = new System.Drawing.Point(12, 0);
            InputLabel.Name = "InputLabel";
            InputLabel.Size = new System.Drawing.Size(39, 17);
            InputLabel.TabIndex = 4;
            InputLabel.Text = "Input";
            // 
            // OutputLabel
            // 
            OutputLabel.AutoSize = true;
            OutputLabel.Location = new System.Drawing.Point(9, 137);
            OutputLabel.Name = "OutputLabel";
            OutputLabel.Size = new System.Drawing.Size(51, 17);
            OutputLabel.TabIndex = 5;
            OutputLabel.Text = "Output";
            // 
            // ProgramLabel
            // 
            ProgramLabel.AutoSize = true;
            ProgramLabel.Location = new System.Drawing.Point(12, 9);
            ProgramLabel.Name = "ProgramLabel";
            ProgramLabel.Size = new System.Drawing.Size(62, 17);
            ProgramLabel.TabIndex = 5;
            ProgramLabel.Text = "Program";
            // 
            // InputTextbox
            // 
            this.InputTextbox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.InputTextbox.Font = new System.Drawing.Font("Courier New", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.InputTextbox.Location = new System.Drawing.Point(12, 18);
            this.InputTextbox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.InputTextbox.Multiline = true;
            this.InputTextbox.Name = "InputTextbox";
            this.InputTextbox.Size = new System.Drawing.Size(505, 89);
            this.InputTextbox.TabIndex = 2;
            // 
            // OutputTextbox
            // 
            this.OutputTextbox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.OutputTextbox.Font = new System.Drawing.Font("Courier New", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.OutputTextbox.Location = new System.Drawing.Point(12, 158);
            this.OutputTextbox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.OutputTextbox.Multiline = true;
            this.OutputTextbox.Name = "OutputTextbox";
            this.OutputTextbox.ReadOnly = true;
            this.OutputTextbox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.OutputTextbox.Size = new System.Drawing.Size(503, 204);
            this.OutputTextbox.TabIndex = 4;
            this.OutputTextbox.WordWrap = false;
            // 
            // MainSplit
            // 
            this.MainSplit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainSplit.Location = new System.Drawing.Point(0, 28);
            this.MainSplit.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.MainSplit.Name = "MainSplit";
            this.MainSplit.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // MainSplit.Panel1
            // 
            this.MainSplit.Panel1.Controls.Add(this.ProgramSizeLabel);
            this.MainSplit.Panel1.Controls.Add(ProgramLabel);
            this.MainSplit.Panel1.Controls.Add(this.ProgramTextbox);
            // 
            // MainSplit.Panel2
            // 
            this.MainSplit.Panel2.Controls.Add(this.PackButton);
            this.MainSplit.Panel2.Controls.Add(this.MainStatus);
            this.MainSplit.Panel2.Controls.Add(this.RunButton);
            this.MainSplit.Panel2.Controls.Add(InputLabel);
            this.MainSplit.Panel2.Controls.Add(this.InputTextbox);
            this.MainSplit.Panel2.Controls.Add(this.OutputTextbox);
            this.MainSplit.Panel2.Controls.Add(OutputLabel);
            this.MainSplit.Size = new System.Drawing.Size(529, 537);
            this.MainSplit.SplitterDistance = 148;
            this.MainSplit.TabIndex = 7;
            // 
            // ProgramSizeLabel
            // 
            this.ProgramSizeLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ProgramSizeLabel.Location = new System.Drawing.Point(213, 7);
            this.ProgramSizeLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.ProgramSizeLabel.Name = "ProgramSizeLabel";
            this.ProgramSizeLabel.Size = new System.Drawing.Size(303, 18);
            this.ProgramSizeLabel.TabIndex = 7;
            this.ProgramSizeLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // ProgramTextbox
            // 
            this.ProgramTextbox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ProgramTextbox.Font = new System.Drawing.Font("Courier New", 14F);
            this.ProgramTextbox.Location = new System.Drawing.Point(12, 30);
            this.ProgramTextbox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.ProgramTextbox.Multiline = true;
            this.ProgramTextbox.Name = "ProgramTextbox";
            this.ProgramTextbox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.ProgramTextbox.Size = new System.Drawing.Size(505, 117);
            this.ProgramTextbox.TabIndex = 1;
            this.ProgramTextbox.TextChanged += new System.EventHandler(this.ProgramTextbox_TextChanged);
            this.ProgramTextbox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.ProgramTextbox_KeyUp);
            this.ProgramTextbox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.ProgramTextbox_MouseMove);
            // 
            // PackButton
            // 
            this.PackButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.PackButton.Location = new System.Drawing.Point(314, 111);
            this.PackButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.PackButton.Name = "PackButton";
            this.PackButton.Size = new System.Drawing.Size(99, 42);
            this.PackButton.TabIndex = 8;
            this.PackButton.Text = "&Pack";
            this.PackButton.UseVisualStyleBackColor = true;
            this.PackButton.Click += new System.EventHandler(this.PackButton_Click);
            // 
            // MainStatus
            // 
            this.MainStatus.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MainStatus.GripStyle = System.Windows.Forms.ToolStripGripStyle.Visible;
            this.MainStatus.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.MainStatus.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.StepCountLabel,
            this.ElapsedTimeLabel});
            this.MainStatus.Location = new System.Drawing.Point(0, 363);
            this.MainStatus.Name = "MainStatus";
            this.MainStatus.Padding = new System.Windows.Forms.Padding(1, 0, 19, 0);
            this.MainStatus.Size = new System.Drawing.Size(529, 22);
            this.MainStatus.TabIndex = 7;
            this.MainStatus.Text = "statusStrip1";
            // 
            // StepCountLabel
            // 
            this.StepCountLabel.Name = "StepCountLabel";
            this.StepCountLabel.Size = new System.Drawing.Size(0, 17);
            // 
            // ElapsedTimeLabel
            // 
            this.ElapsedTimeLabel.Name = "ElapsedTimeLabel";
            this.ElapsedTimeLabel.Size = new System.Drawing.Size(0, 17);
            // 
            // RunButton
            // 
            this.RunButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.RunButton.Location = new System.Drawing.Point(419, 112);
            this.RunButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.RunButton.Name = "RunButton";
            this.RunButton.Size = new System.Drawing.Size(99, 42);
            this.RunButton.TabIndex = 3;
            this.RunButton.Text = "&Run";
            this.RunButton.UseVisualStyleBackColor = true;
            this.RunButton.Click += new System.EventHandler(this.RunButton_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.optionsToolStripMenuItem,
            this.toolsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(8, 2, 0, 2);
            this.menuStrip1.Size = new System.Drawing.Size(529, 28);
            this.menuStrip1.TabIndex = 8;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.AnnotateMenuItem,
            this.TimeoutMenuItem});
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(73, 24);
            this.optionsToolStripMenuItem.Text = "&Options";
            // 
            // AnnotateMenuItem
            // 
            this.AnnotateMenuItem.CheckOnClick = true;
            this.AnnotateMenuItem.Name = "AnnotateMenuItem";
            this.AnnotateMenuItem.Size = new System.Drawing.Size(145, 26);
            this.AnnotateMenuItem.Text = "Annotate";
            // 
            // TimeoutMenuItem
            // 
            this.TimeoutMenuItem.Checked = true;
            this.TimeoutMenuItem.CheckOnClick = true;
            this.TimeoutMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.TimeoutMenuItem.Name = "TimeoutMenuItem";
            this.TimeoutMenuItem.Size = new System.Drawing.Size(145, 26);
            this.TimeoutMenuItem.Text = "Timeout";
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.stringCompressorToolStripMenuItem,
            this.integerArrayCrammerToolStripMenuItem});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(56, 24);
            this.toolsToolStripMenuItem.Text = "&Tools";
            // 
            // stringCompressorToolStripMenuItem
            // 
            this.stringCompressorToolStripMenuItem.Name = "stringCompressorToolStripMenuItem";
            this.stringCompressorToolStripMenuItem.Size = new System.Drawing.Size(235, 26);
            this.stringCompressorToolStripMenuItem.Text = "String Compressor";
            this.stringCompressorToolStripMenuItem.Click += new System.EventHandler(this.stringCompressorToolStripMenuItem_Click);
            // 
            // integerArrayCrammerToolStripMenuItem
            // 
            this.integerArrayCrammerToolStripMenuItem.Name = "integerArrayCrammerToolStripMenuItem";
            this.integerArrayCrammerToolStripMenuItem.Size = new System.Drawing.Size(235, 26);
            this.integerArrayCrammerToolStripMenuItem.Text = "Integer Array Crammer";
            this.integerArrayCrammerToolStripMenuItem.Click += new System.EventHandler(this.integerArrayCrammerToolStripMenuItem_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(529, 565);
            this.Controls.Add(this.MainSplit);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "MainForm";
            this.Text = "Stax Language";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.MainSplit.Panel1.ResumeLayout(false);
            this.MainSplit.Panel1.PerformLayout();
            this.MainSplit.Panel2.ResumeLayout(false);
            this.MainSplit.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.MainSplit)).EndInit();
            this.MainSplit.ResumeLayout(false);
            this.MainStatus.ResumeLayout(false);
            this.MainStatus.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TextBox InputTextbox;
        private System.Windows.Forms.TextBox OutputTextbox;
        private System.Windows.Forms.SplitContainer MainSplit;
        private System.Windows.Forms.TextBox ProgramTextbox;
        private System.Windows.Forms.Button RunButton;
        private System.Windows.Forms.Label ProgramSizeLabel;
        private System.Windows.Forms.StatusStrip MainStatus;
        private System.Windows.Forms.ToolStripStatusLabel StepCountLabel;
        private System.Windows.Forms.ToolStripStatusLabel ElapsedTimeLabel;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem AnnotateMenuItem;
        private System.Windows.Forms.Button PackButton;
        private System.Windows.Forms.ToolStripMenuItem TimeoutMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stringCompressorToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem integerArrayCrammerToolStripMenuItem;
    }
}

