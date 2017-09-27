namespace StaxLang {
    partial class CompressorForm {
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.Label OriginalLabel;
            System.Windows.Forms.Label CompressedLabel;
            this.OriginalTextbox = new System.Windows.Forms.TextBox();
            this.CompressedTextbox = new System.Windows.Forms.TextBox();
            this.EditTimer = new System.Windows.Forms.Timer(this.components);
            OriginalLabel = new System.Windows.Forms.Label();
            CompressedLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // OriginalLabel
            // 
            OriginalLabel.AutoSize = true;
            OriginalLabel.Location = new System.Drawing.Point(12, 9);
            OriginalLabel.Name = "OriginalLabel";
            OriginalLabel.Size = new System.Drawing.Size(42, 13);
            OriginalLabel.TabIndex = 1;
            OriginalLabel.Text = "Original";
            // 
            // CompressedLabel
            // 
            CompressedLabel.AutoSize = true;
            CompressedLabel.Location = new System.Drawing.Point(12, 111);
            CompressedLabel.Name = "CompressedLabel";
            CompressedLabel.Size = new System.Drawing.Size(65, 13);
            CompressedLabel.TabIndex = 2;
            CompressedLabel.Text = "Compressed";
            // 
            // OriginalTextbox
            // 
            this.OriginalTextbox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.OriginalTextbox.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.OriginalTextbox.Location = new System.Drawing.Point(12, 25);
            this.OriginalTextbox.Multiline = true;
            this.OriginalTextbox.Name = "OriginalTextbox";
            this.OriginalTextbox.Size = new System.Drawing.Size(467, 65);
            this.OriginalTextbox.TabIndex = 0;
            this.OriginalTextbox.TextChanged += new System.EventHandler(this.OriginalTextbox_TextChanged);
            // 
            // CompressedTextbox
            // 
            this.CompressedTextbox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.CompressedTextbox.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CompressedTextbox.Location = new System.Drawing.Point(12, 127);
            this.CompressedTextbox.Multiline = true;
            this.CompressedTextbox.Name = "CompressedTextbox";
            this.CompressedTextbox.ReadOnly = true;
            this.CompressedTextbox.Size = new System.Drawing.Size(467, 65);
            this.CompressedTextbox.TabIndex = 3;
            // 
            // EditTimer
            // 
            this.EditTimer.Tick += new System.EventHandler(this.EditTimer_Tick);
            // 
            // CompressorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(492, 226);
            this.Controls.Add(this.CompressedTextbox);
            this.Controls.Add(CompressedLabel);
            this.Controls.Add(OriginalLabel);
            this.Controls.Add(this.OriginalTextbox);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CompressorForm";
            this.Text = "Stax Compressor";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox OriginalTextbox;
        private System.Windows.Forms.TextBox CompressedTextbox;
        private System.Windows.Forms.Timer EditTimer;
    }
}