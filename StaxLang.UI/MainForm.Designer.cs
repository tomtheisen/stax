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
            System.Windows.Forms.Label ProgramLabel;
            System.Windows.Forms.Label InputLabel;
            System.Windows.Forms.Label OutputLabel;
            this.ProgramTextbox = new System.Windows.Forms.TextBox();
            this.RunButton = new System.Windows.Forms.Button();
            this.InputTextbox = new System.Windows.Forms.TextBox();
            this.OutputTextbox = new System.Windows.Forms.TextBox();
            this.ProgramSizeLabel = new System.Windows.Forms.Label();
            ProgramLabel = new System.Windows.Forms.Label();
            InputLabel = new System.Windows.Forms.Label();
            OutputLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // ProgramLabel
            // 
            ProgramLabel.AutoSize = true;
            ProgramLabel.Location = new System.Drawing.Point(7, 7);
            ProgramLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            ProgramLabel.Name = "ProgramLabel";
            ProgramLabel.Size = new System.Drawing.Size(46, 13);
            ProgramLabel.TabIndex = 3;
            ProgramLabel.Text = "Program";
            // 
            // InputLabel
            // 
            InputLabel.AutoSize = true;
            InputLabel.Location = new System.Drawing.Point(9, 102);
            InputLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            InputLabel.Name = "InputLabel";
            InputLabel.Size = new System.Drawing.Size(31, 13);
            InputLabel.TabIndex = 4;
            InputLabel.Text = "Input";
            // 
            // OutputLabel
            // 
            OutputLabel.AutoSize = true;
            OutputLabel.Location = new System.Drawing.Point(9, 193);
            OutputLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            OutputLabel.Name = "OutputLabel";
            OutputLabel.Size = new System.Drawing.Size(39, 13);
            OutputLabel.TabIndex = 5;
            OutputLabel.Text = "Output";
            // 
            // ProgramTextbox
            // 
            this.ProgramTextbox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ProgramTextbox.Font = new System.Drawing.Font("Courier New", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ProgramTextbox.Location = new System.Drawing.Point(9, 24);
            this.ProgramTextbox.Margin = new System.Windows.Forms.Padding(2);
            this.ProgramTextbox.Multiline = true;
            this.ProgramTextbox.Name = "ProgramTextbox";
            this.ProgramTextbox.Size = new System.Drawing.Size(380, 76);
            this.ProgramTextbox.TabIndex = 0;
            this.ProgramTextbox.TextChanged += new System.EventHandler(this.ProgramTextbox_TextChanged);
            this.ProgramTextbox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.ProgramTextbox_KeyUp);
            this.ProgramTextbox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.ProgramTextbox_MouseMove);
            // 
            // RunButton
            // 
            this.RunButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.RunButton.Location = new System.Drawing.Point(314, 327);
            this.RunButton.Margin = new System.Windows.Forms.Padding(2);
            this.RunButton.Name = "RunButton";
            this.RunButton.Size = new System.Drawing.Size(74, 34);
            this.RunButton.TabIndex = 3;
            this.RunButton.Text = "&Run";
            this.RunButton.UseVisualStyleBackColor = true;
            this.RunButton.Click += new System.EventHandler(this.RunButton_Click);
            // 
            // InputTextbox
            // 
            this.InputTextbox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.InputTextbox.Font = new System.Drawing.Font("Courier New", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.InputTextbox.Location = new System.Drawing.Point(9, 118);
            this.InputTextbox.Margin = new System.Windows.Forms.Padding(2);
            this.InputTextbox.Multiline = true;
            this.InputTextbox.Name = "InputTextbox";
            this.InputTextbox.Size = new System.Drawing.Size(377, 73);
            this.InputTextbox.TabIndex = 1;
            // 
            // OutputTextbox
            // 
            this.OutputTextbox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.OutputTextbox.Font = new System.Drawing.Font("Courier New", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.OutputTextbox.Location = new System.Drawing.Point(9, 209);
            this.OutputTextbox.Margin = new System.Windows.Forms.Padding(2);
            this.OutputTextbox.Multiline = true;
            this.OutputTextbox.Name = "OutputTextbox";
            this.OutputTextbox.ReadOnly = true;
            this.OutputTextbox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.OutputTextbox.Size = new System.Drawing.Size(377, 114);
            this.OutputTextbox.TabIndex = 2;
            this.OutputTextbox.WordWrap = false;
            // 
            // ProgramSizeLabel
            // 
            this.ProgramSizeLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ProgramSizeLabel.Location = new System.Drawing.Point(162, 7);
            this.ProgramSizeLabel.Name = "ProgramSizeLabel";
            this.ProgramSizeLabel.Size = new System.Drawing.Size(227, 15);
            this.ProgramSizeLabel.TabIndex = 6;
            this.ProgramSizeLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(397, 370);
            this.Controls.Add(this.ProgramSizeLabel);
            this.Controls.Add(this.OutputTextbox);
            this.Controls.Add(OutputLabel);
            this.Controls.Add(InputLabel);
            this.Controls.Add(ProgramLabel);
            this.Controls.Add(this.InputTextbox);
            this.Controls.Add(this.RunButton);
            this.Controls.Add(this.ProgramTextbox);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "MainForm";
            this.Text = "Stax Language";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox ProgramTextbox;
        private System.Windows.Forms.Button RunButton;
        private System.Windows.Forms.TextBox InputTextbox;
        private System.Windows.Forms.TextBox OutputTextbox;
        private System.Windows.Forms.Label ProgramSizeLabel;
    }
}

