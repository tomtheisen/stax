namespace StaxLang {
    partial class ArrayCrammerForm {
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
            OriginalLabel.Location = new System.Drawing.Point(16, 11);
            OriginalLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            OriginalLabel.Name = "OriginalLabel";
            OriginalLabel.Size = new System.Drawing.Size(57, 17);
            OriginalLabel.TabIndex = 1;
            OriginalLabel.Text = "Original";
            // 
            // CompressedLabel
            // 
            CompressedLabel.AutoSize = true;
            CompressedLabel.Location = new System.Drawing.Point(16, 137);
            CompressedLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            CompressedLabel.Name = "CompressedLabel";
            CompressedLabel.Size = new System.Drawing.Size(68, 17);
            CompressedLabel.TabIndex = 2;
            CompressedLabel.Text = "Crammed";
            // 
            // OriginalTextbox
            // 
            this.OriginalTextbox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.OriginalTextbox.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.OriginalTextbox.Location = new System.Drawing.Point(16, 31);
            this.OriginalTextbox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.OriginalTextbox.Multiline = true;
            this.OriginalTextbox.Name = "OriginalTextbox";
            this.OriginalTextbox.Size = new System.Drawing.Size(621, 79);
            this.OriginalTextbox.TabIndex = 0;
            this.OriginalTextbox.TextChanged += new System.EventHandler(this.OriginalTextbox_TextChanged);
            // 
            // CompressedTextbox
            // 
            this.CompressedTextbox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.CompressedTextbox.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CompressedTextbox.Location = new System.Drawing.Point(16, 156);
            this.CompressedTextbox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.CompressedTextbox.Multiline = true;
            this.CompressedTextbox.Name = "CompressedTextbox";
            this.CompressedTextbox.ReadOnly = true;
            this.CompressedTextbox.Size = new System.Drawing.Size(621, 79);
            this.CompressedTextbox.TabIndex = 3;
            // 
            // EditTimer
            // 
            this.EditTimer.Tick += new System.EventHandler(this.EditTimer_Tick);
            // 
            // ArrayCrammerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(656, 278);
            this.Controls.Add(this.CompressedTextbox);
            this.Controls.Add(CompressedLabel);
            this.Controls.Add(OriginalLabel);
            this.Controls.Add(this.OriginalTextbox);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ArrayCrammerForm";
            this.Text = "Stax Integer Array Crammer";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox OriginalTextbox;
        private System.Windows.Forms.TextBox CompressedTextbox;
        private System.Windows.Forms.Timer EditTimer;
    }
}