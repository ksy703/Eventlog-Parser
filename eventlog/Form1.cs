using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace eventlog
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Init();
        }
        public void Init()
        {
            progressBar1.Style = ProgressBarStyle.Continuous;
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 1;
            progressBar1.Step = 1;
            progressBar1.Value = 0;
        }
        public StreamReader ReadFile(string mir_directory,string mir_fileName)
        {
            DirectoryInfo di = new DirectoryInfo(mir_directory);
            if (!di.Exists) { return null; }
            FileInfo fi = new FileInfo(mir_directory + @"\" + mir_fileName);
            if (!fi.Exists) { return null; }
            StreamReader sr = new StreamReader(mir_directory + @"\" + mir_fileName,Encoding.ASCII,false);
            return sr;
        }
        public void MirReadToEnd(StreamReader sr)
        {
            if (sr != null)
            {
                textBox1.Text = sr.ReadToEnd();
                progressBar1.PerformStep();
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            FileopenDialog();
        }
        public void FileopenDialog()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "file open";
            ofd.FileName = "test";
            ofd.Filter = "event log 파일|*.evl";

            DialogResult dr = ofd.ShowDialog();
            if (dr == DialogResult.OK)
            {
                string fileName = ofd.SafeFileName;
                string fileFullName = ofd.FileName;
                string filePath = fileFullName.Replace(fileName, "");
                textBox1.Text = fileFullName;

                StreamReader sr = ReadFile(filePath, fileName);
                MirReadToEnd(sr);
                sr.Close();
            }
        }
    }
}
