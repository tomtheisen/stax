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
            this.ProgramTextbox = new System.Windows.Forms.TextBox();
            this.RunButton = new System.Windows.Forms.Button();
            this.ProgramSizeLabel = new System.Windows.Forms.Label();
            InputLabel = new System.Windows.Forms.Label();
            OutputLabel = new System.Windows.Forms.Label();
            ProgramLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.MainSplit)).BeginInit();
            this.MainSplit.Panel1.SuspendLayout();
            this.MainSplit.Panel2.SuspendLayout();
            this.MainSplit.SuspendLayout();
            this.SuspendLayout();
            // 
            // InputLabel
            // 
            InputLabel.AutoSize = true;
            InputLabel.Location = new System.Drawing.Point(9, 0);
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
            this.InputTextbox.Location = new System.Drawing.Point(12, 19);
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
            this.OutputTextbox.Size = new System.Drawing.Size(502, 244);
            this.OutputTextbox.TabIndex = 4;
            this.OutputTextbox.WordWrap = false;
            // 
            // MainSplit
            // 
            this.MainSplit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainSplit.Location = new System.Drawing.Point(0, 0);
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
            this.MainSplit.Panel2.Controls.Add(this.RunButton);
            this.MainSplit.Panel2.Controls.Add(InputLabel);
            this.MainSplit.Panel2.Controls.Add(this.InputTextbox);
            this.MainSplit.Panel2.Controls.Add(this.OutputTextbox);
            this.MainSplit.Panel2.Controls.Add(OutputLabel);
            this.MainSplit.Size = new System.Drawing.Size(529, 565);
            this.MainSplit.SplitterDistance = 148;
            this.MainSplit.TabIndex = 7;
            // 
            // ProgramTextbox
            // 
            this.ProgramTextbox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ProgramTextbox.Font = new System.Drawing.Font("Courier New", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ProgramTextbox.Location = new System.Drawing.Point(12, 29);
            this.ProgramTextbox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.ProgramTextbox.Multiline = true;
            this.ProgramTextbox.Name = "ProgramTextbox";
            this.ProgramTextbox.Size = new System.Drawing.Size(505, 117);
            this.ProgramTextbox.TabIndex = 1;
            this.ProgramTextbox.TextChanged += new System.EventHandler(this.ProgramTextbox_TextChanged);
            this.ProgramTextbox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.ProgramTextbox_KeyUp);
            this.ProgramTextbox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.ProgramTextbox_MouseMove);
            // 
            // RunButton
            // 
            this.RunButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.RunButton.Location = new System.Drawing.Point(418, 112);
            this.RunButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.RunButton.Name = "RunButton";
            this.RunButton.Size = new System.Drawing.Size(99, 42);
            this.RunButton.TabIndex = 3;
            this.RunButton.Text = "&Run";
            this.RunButton.UseVisualStyleBackColor = true;
            this.RunButton.Click += new System.EventHandler(this.RunButton_Click);
            // 
            // ProgramSizeLabel
            // 
            this.ProgramSizeLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ProgramSizeLabel.Location = new System.Drawing.Point(214, 8);
            this.ProgramSizeLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.ProgramSizeLabel.Name = "ProgramSizeLabel";
            this.ProgramSizeLabel.Size = new System.Drawing.Size(303, 18);
            this.ProgramSizeLabel.TabIndex = 7;
            this.ProgramSizeLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(529, 565);
            this.Controls.Add(this.MainSplit);
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
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.TextBox InputTextbox;
        private System.Windows.Forms.TextBox OutputTextbox;
        private System.Windows.Forms.SplitContainer MainSplit;
        private System.Windows.Forms.TextBox ProgramTextbox;
        private System.Windows.Forms.Button RunButton;
        private System.Windows.Forms.Label ProgramSizeLabel;
    }
}

