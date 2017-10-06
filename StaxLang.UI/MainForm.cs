using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace StaxLang.UI {
    public partial class MainForm : Form {
        public MainForm() {
            InitializeComponent();
        }

        private void RunButton_Click(object sender, EventArgs e) {
            var output = new StringWriter();
            var input = InputTextbox.Lines.Reverse().SkipWhile(l => l == "").Reverse().ToArray();
            var program = ProgramTextbox.SelectedText == "" ? ProgramTextbox.Text : ProgramTextbox.SelectedText;
            var sw = Stopwatch.StartNew();
            try {
                int steps = new Executor(output).Run(program, input);
                StepCountLabel.Text = $"{steps} steps.";
                OutputTextbox.Text = output.ToString();
            }
            catch (Exception ex) {
                OutputTextbox.Text = ex.Message;
                StepCountLabel.Text = $"Failed.";
            }
            ElapsedTimeLabel.Text = $"Elapsed: {sw.Elapsed}";
        }

        private void ProgramTextbox_TextChanged(object sender, EventArgs e) {
            UpdateMetrics();
        }

        private void ProgramTextbox_KeyUp(object sender, KeyEventArgs e) {
            UpdateMetrics();
        }

        private void ProgramTextbox_MouseMove(object sender, MouseEventArgs e) {
            UpdateMetrics();
        }

        private void UpdateMetrics() {
            ProgramSizeLabel.Text = $"{ProgramTextbox.Text.Length} characters";
            if (ProgramTextbox.SelectedText.Length == 1) {
                ProgramSizeLabel.Text += $" (codepoint {ProgramTextbox.SelectedText[0] - 0})";
            }
            else if (ProgramTextbox.SelectedText != "") {
                ProgramSizeLabel.Text += $" ({ProgramTextbox.SelectedText.Length} selected)";
            }
        }

        private void MainForm_Load(object sender, EventArgs e) {
            ProgramTextbox.Text = Settings.Default.Program;
            InputTextbox.Text = Settings.Default.Input;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
            Settings.Default.Program = ProgramTextbox.Text;
            Settings.Default.Input = InputTextbox.Text;
            Settings.Default.Save();
        }

        private void CompressorButton_Click(object sender, EventArgs e) {
            new CompressorForm().Show(this);
        }
    }
}
