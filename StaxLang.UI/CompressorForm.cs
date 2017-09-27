using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StaxLang {
    public partial class CompressorForm : Form {
        public CompressorForm() {
            InitializeComponent();
        }

        private void OriginalTextbox_TextChanged(object sender, EventArgs e) {
            EditTimer.Enabled = true;
            EditTimer.Stop();
            EditTimer.Start();
        }

        private void EditTimer_Tick(object sender, EventArgs e) {
            EditTimer.Enabled = false;

            string input = OriginalTextbox.Text;
            if (input.Length == 0) {
                CompressedTextbox.Text = "z";
                return;
            }
            if (input.Length == 1) {
                CompressedTextbox.Text = "'" + input;
                return;
            }

            string compressed = HuffmanCompressor.Compress(input);
            if (compressed == null) {
                CompressedTextbox.Text = '"' + input.Replace("`", "``").Replace("\"", "`\"") + '"';
                return;
            }

            CompressedTextbox.Text = '.' + compressed + '.';
        }
    }
}
