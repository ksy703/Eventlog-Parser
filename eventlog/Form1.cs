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
using System.Collections;

namespace eventlog
{
    public partial class Form1 : Form
    {
        string fileName;
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
            dataGridView1.Columns.Clear();
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
                fileName = ofd.SafeFileName;
                string fileFullName = ofd.FileName;
                string filePath = fileFullName.Replace(fileName, "");
                textBox1.Text = fileFullName;

                StreamReader sr = ReadFile(filePath, fileName);
                MirReadToEnd(sr);

                sr.Close();

                BinaryReader rdr = new BinaryReader(File.Open(filePath + @"\" + fileName, FileMode.Open));
                //byte[] bytes = rdr.ReadBytes(130);
                byte[] bytes=new byte[200];
                rdr.BaseStream.Position = 0; int i=0;
                while(i<200)
                {
                    if (bytes[i] == 255)
                    {
                        break;
                    }
                    bytes[i] = rdr.ReadByte();
                    i++;
                }


                BitArray myBytes = new BitArray(bytes);


                for (int k = 0; k <200; k++)
                {
                    dataGridView1.Columns.Add(k.ToString(), k.ToString());
                }
                for (int r = 0; r < 200; r++)
                {
                    dataGridView1.Rows.Add();
                    
                    dataGridView1[r%16, r/16].Value = bytes[r];
                }
                int cnt = 0;
                foreach(int bytesvalue in bytes)
                {
                    string stringValue = Char.ConvertFromUtf32(bytesvalue);
                    dataGridView1[cnt, 16].Value = stringValue;
                    cnt++;
                }
                
                for (int k = 0; k < 1600; k++)
                {
                    Boolean b = myBytes[k];
                    if (b == true)
                    {
                        dataGridView1[k / 8,15].Value += "1";
                    }
                    else
                    {
                        dataGridView1[k / 8,15].Value += "0";
                    }
                }
                
            }
        }
        public void writeCSV(DataGridView gridIn, string outputFile)
        {
            //test to see if the DataGridView has any rows
            if (gridIn.RowCount > 0)
            {
                string value = "";
                DataGridViewRow dr = new DataGridViewRow();
                StreamWriter swOut = new StreamWriter(outputFile);

                //write header rows to csv
                for (int i = 0; i <= gridIn.Columns.Count - 1; i++)
                {
                    if (i > 0)
                    {
                        swOut.Write(",");
                    }
                    swOut.Write(gridIn.Columns[i].HeaderText);
                }

                swOut.WriteLine();

                //write DataGridView rows to csv
                for (int j = 0; j <= gridIn.Rows.Count - 1; j++)
                {
                    if (j > 0)
                    {
                        swOut.WriteLine();
                    }

                    dr = gridIn.Rows[j];

                    for (int i = 0; i <= gridIn.Columns.Count - 1; i++)
                    {
                        if (i > 0)
                        {
                            swOut.Write(",");
                        }
                        if (dr.Cells[i].Value==null)
                        {
                            dr.Cells[i].Value = "";
                        }
                        value = dr.Cells[i].Value.ToString();
                        //replace comma's with spaces
                        value = value.Replace(',', ' ');
                        //replace embedded newlines with spaces
                        value = value.Replace(Environment.NewLine, " ");

                        swOut.Write(value);
                    }
                }
                swOut.Close();
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            writeCSV(dataGridView1, fileName+".csv");
            MessageBox.Show("Converted successfully to *.csv format");
        }
    }
}
