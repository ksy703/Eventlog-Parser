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
        string fileName;long l;
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
            l = fi.Length;
            StreamReader sr = new StreamReader(mir_directory + @"\" + mir_fileName,Encoding.ASCII,false);
            return sr;
        }
        public void MirReadToEnd(StreamReader sr)
        {
            if (sr != null)
            {
                textBox1.Text += sr.ReadToEnd();
                progressBar1.PerformStep();
            }
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            dataGridView1.Columns.Clear();
            FileopenDialog();
        }
        public void SetUpDataGridView()
        {
            dataGridView1.ColumnCount = 12;
            dataGridView1.Columns[0].Name = "Serial Number";
            dataGridView1.Columns[1].Name = "Start Time";
            dataGridView1.Columns[1].Width = 130;
            dataGridView1.Columns[2].Name = "Event Type";
            dataGridView1.Columns[3].Name = "Duration";
            dataGridView1.Columns[4].Name = "H2S Status";
            dataGridView1.Columns[5].Name = "H2S Peak (ppm)";
            dataGridView1.Columns[6].Name = "CO Status";
            dataGridView1.Columns[7].Name = "CO Peak (ppm)";
            dataGridView1.Columns[8].Name = "O2 Status";
            dataGridView1.Columns[9].Name = "O2 Peak (%Vol)";
            dataGridView1.Columns[10].Name = "LEL Status";
            dataGridView1.Columns[11].Name = "LEL Peak (%LEL)";
        }
        public string make_bit(Boolean b)
        {
            if (b == true)
            {
                return "1";
            }
            else
            {
                return "0";
            }
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
                string sn="";
                StreamReader sr = ReadFile(filePath, fileName);
                MirReadToEnd(sr);

                sr.Close();
                BinaryReader rdr = new BinaryReader(File.Open(filePath + @"\" + fileName, FileMode.Open));
                //byte[] bytes = rdr.ReadBytes(130);
                byte[] Header_bytes=new byte[15];
                byte[] info_bytes = new byte[18];
                rdr.BaseStream.Position = 0; int i=0; int row = 0;
                SetUpDataGridView();
                
                while (rdr.BaseStream.Position!=l)
                {
                    Header_bytes[i] = rdr.ReadByte();
                    if (i>0&&Char.ConvertFromUtf32(Header_bytes[i-1])=="S"&& Char.ConvertFromUtf32(Header_bytes[i]) == "N")
                    {
                        rdr.ReadByte();
                        byte[] bytes = rdr.ReadBytes(88);
                        sn = "";
                        for(int x = 0; x < 12; x++)
                        {
                            sn += Char.ConvertFromUtf32(bytes[x]);
                        }
                        
                        info_bytes = rdr.ReadBytes(18);
                        while (info_bytes[0] != 255)
                        {
                           
                            BitArray myBytes = new BitArray(info_bytes);
                            String[] info_bits = new String[18];
                            info_bits[0] = make_bit(myBytes[7]);
                            for(int k=1;k<=6;k++)
                            {
                                info_bits[1] = make_bit(myBytes[k]) + info_bits[1];
                            }
                            
                            for(int k = 13; k <= 15; k++)
                            {
                                info_bits[2] = make_bit(myBytes[k]) + info_bits[2];
                            }
                            info_bits[2] = make_bit(myBytes[0])+info_bits[2];
                            for (int k = 8; k <= 12;k++)
                            {
                                info_bits[3] = make_bit(myBytes[k]) + info_bits[3];
                            }for(int k = 20; k <= 23; k++)
                            {
                                info_bits[4] = make_bit(myBytes[k]) + info_bits[4];
                            }for(int k = 30; k < 32; k++)
                            {
                                info_bits[5] = make_bit(myBytes[k]) + info_bits[5];
                            }
                            for (int k = 16; k <= 19; k++)
                            {
                                info_bits[5] = make_bit(myBytes[k]) + info_bits[5];
                            }for(int k = 24; k <= 29; k++)
                            {
                                info_bits[6] = make_bit(myBytes[k]) + info_bits[6];
                            }
                            int hour = Convert.ToInt32(info_bits[4], 2);
                            if (info_bits[0].Equals("1"))
                            {
                                hour += 12;
                            }
                            string date_time = (2000+Convert.ToInt32(info_bits[1], 2))+"-"+(Convert.ToInt32(info_bits[2],2)).ToString("D2")+"-"+(Convert.ToInt32(info_bits[3],2)).ToString("D2")
                            + " " + (hour).ToString("D2") + ":" + (Convert.ToInt32(info_bits[5],2)).ToString("D2") + ":" + (Convert.ToInt32(info_bits[6],2)).ToString("D2");

                            dataGridView1.Rows.Add();
                            dataGridView1[0, row].Value = sn;
                            dataGridView1[2, row].Value = "Peak Exposure";
                            dataGridView1[3, row].Value = info_bytes[5];
                            dataGridView1[5, row].Value = info_bytes[8];
                            dataGridView1[7, row].Value = info_bytes[11];
                            dataGridView1[9, row].Value = (float)info_bytes[14] / 10;
                            dataGridView1[11, row].Value = info_bytes[17];
                            dataGridView1[1, row].Value = date_time;
                            
                            for (int j = 0; j < dataGridView1.Rows.Count-1; j++)
                            {
                                if (dataGridView1[1, j].Value.ToString()==(date_time)&&dataGridView1[0,j].Value.ToString()==(sn))
                                {
                                    dataGridView1.Rows.Remove(dataGridView1.Rows[row]);
                                    row--;
                                    break;
                                }
                                if (Convert.ToInt32(info_bits[1], 2) > 18)
                                {
                                    dataGridView1.Rows.Remove(dataGridView1.Rows[row]);
                                    row--;
                                    break;
                                }
                            }
                            
                            row++;
                            info_bytes = rdr.ReadBytes(18);
                        }
                        
                    }
                    i +=1;
                    i = i % 2;
                    
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
                for (int j = 0; j < gridIn.Rows.Count; j++)
                {
                    if (j > 0)
                    {
                        swOut.WriteLine();
                    }

                    dr = gridIn.Rows[j];

                    for (int i = 0; i < gridIn.Columns.Count; i++)
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
