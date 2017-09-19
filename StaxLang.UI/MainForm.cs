using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            try {
                new Executor(output).Run(program, input);
                OutputTextbox.Text = output.ToString();
            }
            catch (Exception ex) {
                OutputTextbox.Text = ex.Message;
            }
        }
    }
}
