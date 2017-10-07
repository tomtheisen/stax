namespace StaxLang.UI {
    partial class CodeViewForm {
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
            this.CodeTextbox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // CodeTextbox
            // 
            this.CodeTextbox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CodeTextbox.Font = new System.Drawing.Font("Courier New", 14F);
            this.CodeTextbox.Location = new System.Drawing.Point(0, 0);
            this.CodeTextbox.Multiline = true;
            this.CodeTextbox.Name = "CodeTextbox";
            this.CodeTextbox.ReadOnly = true;
            this.CodeTextbox.Size = new System.Drawing.Size(499, 329);
            this.CodeTextbox.TabIndex = 0;
            // 
            // CodeViewForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(499, 329);
            this.Controls.Add(this.CodeTextbox);
            this.Name = "CodeViewForm";
            this.Text = "CodeTransformForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.TextBox CodeTextbox;
    }
}