using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace StaxLang {
    public partial class ArrayCrammerForm : Form {
        public ArrayCrammerForm() {
            InitializeComponent();
        }

        private void OriginalTextbox_TextChanged(object sender, EventArgs e) {
            EditTimer.Enabled = true;
            EditTimer.Stop();
            EditTimer.Start();
        }

        private void EditTimer_Tick(object sender, EventArgs e) {
            EditTimer.Enabled = false;

            var matches = Regex.Matches(OriginalTextbox.Text, "-?\\d+");
            if (matches.Count == 0) {
                CompressedTextbox.Text = "z";
                return;
            }

            var numbers = new List<BigInteger>();
            for (int i = 0; i < matches.Count; i++) {
                numbers.Add(BigInteger.Parse(matches[i].Value));
            }
            string crammed = ArrayCrammer.Cram(numbers);

            CompressedTextbox.Text = $"\"{ crammed }\"!";
        }
    }
}
