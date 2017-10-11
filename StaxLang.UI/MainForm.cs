using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace StaxLang.UI {
    public partial class MainForm : Form {
        private CodeViewForm AnnotationForm = new CodeViewForm();

        public MainForm() {
            InitializeComponent();
        }

        private void RunButton_Click(object sender, EventArgs e) {
            var output = new StringWriter();
            var input = InputTextbox.Lines.Reverse().SkipWhile(l => l == "").Reverse().ToArray();
            var program = ProgramTextbox.SelectedText == "" ? ProgramTextbox.Text : ProgramTextbox.SelectedText;
            var sw = Stopwatch.StartNew();
            try {
                bool annotate = AnnotateMenuItem.Checked;
                var runner = new Executor(output) { Annotate = annotate };
                int steps = runner.Run(program, input, TimeSpan.FromSeconds(10));
                StepCountLabel.Text = $"{steps} steps.";
                OutputTextbox.Text = output.ToString();
                if (annotate) {
                    string formatted = string.Join(Environment.NewLine, runner.Annotation);
                    AnnotationForm.CodeTextbox.Text = formatted;
                    if (!AnnotationForm.Visible) AnnotationForm.Show(this);
                }
            }
            catch (Exception ex) {
                OutputTextbox.Text = ex.Message;
                StepCountLabel.Text = $"Failed.";
            }
            ElapsedTimeLabel.Text = $"Elapsed: {sw.Elapsed}";
        }

        private void ProgramTextbox_TextChanged(object sender, EventArgs e) {
            UpdatePackStatus();
            UpdateMetrics();
        }

        private void ProgramTextbox_KeyUp(object sender, KeyEventArgs e) {
            UpdateMetrics();
        }

        private void ProgramTextbox_MouseMove(object sender, MouseEventArgs e) {
            UpdateMetrics();
        }

        private void UpdatePackStatus() {
            string code = ProgramTextbox.Text;
            PackButton.Text = "&Pack";
            if (code.Contains("\n")) {
                PackButton.Enabled = false;
                return;
            }

            PackButton.Enabled = true;
            if (StaxPacker.IsPacked(code)) {
                PackButton.Text = "Un&pack";
            }
        }

        private void UpdateMetrics() {
            ProgramSizeLabel.Text = $"{ProgramTextbox.Text.Length} characters";
            if (ProgramTextbox.SelectedText.Length == 1) {
                ProgramSizeLabel.Text += $" (codepoint {ProgramTextbox.SelectedText[0] - 0})";
            }
            else if (ProgramTextbox.SelectedText != "") {
                ProgramSizeLabel.Text += $" ({ProgramTextbox.SelectedText.Length} selected)";
            }
            else if (StaxPacker.IsPacked(ProgramTextbox.Text)) {
                ProgramSizeLabel.Text += " (packed)";
            }
        }

        private void MainForm_Load(object sender, EventArgs e) {
            ProgramTextbox.Text = Settings.Default.Program;
            InputTextbox.Text = Settings.Default.Input;
            AnnotateMenuItem.Checked = Settings.Default.Annotate;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
            Settings.Default.Program = ProgramTextbox.Text;
            Settings.Default.Input = InputTextbox.Text;
            Settings.Default.Annotate = AnnotateMenuItem.Checked;
            Settings.Default.Save();
        }

        private void CompressorButton_Click(object sender, EventArgs e) {
            new CompressorForm().Show(this);
        }

        private void PackButton_Click(object sender, EventArgs e) {
            string code = ProgramTextbox.Text;

            if (StaxPacker.IsPacked(code)) {
                ProgramTextbox.Text = StaxPacker.Unpack(code);
            }
            else {
                ProgramTextbox.Text = StaxPacker.Pack(code);
            }
        }
    }
}
