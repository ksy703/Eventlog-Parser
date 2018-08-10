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
        private DataTable dt;

        public Form1()
        {
            InitializeComponent(); 
        }

        //progressBar initializing
        public void progressBar1_Init()
        {
            progressBar1.Style = ProgressBarStyle.Continuous;
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 1;
            progressBar1.Step = 1;
            progressBar1.Value = 0;
        }

        //check file, get info
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

        //import button click
        private void button1_Click(object sender, EventArgs e)
        {
            dataGridView1.Columns.Clear();
            eventlog_parsing();

            dt = dt.DefaultView.ToTable(true);
            dataGridView1.DataSource = dt;
            dataGridView1.EditMode = DataGridViewEditMode.EditProgrammatically;
        }

        //setting datatable columns
        public void SetUpData()
        {
            dt.Columns.Add("Serial Number");
            dt.Columns.Add("Start Time");
            dt.Columns.Add("Event Type");
            dt.Columns.Add("Duration");
            dt.Columns.Add("H2S Status");
            dt.Columns.Add("H2S Peak (ppm)");
            dt.Columns.Add("CO Status");
            dt.Columns.Add("CO Peak (ppm)");
            dt.Columns.Add("O2 Status");
            dt.Columns.Add("O2 Peak (%Vol)");
            dt.Columns.Add("LEL Status");
            dt.Columns.Add("LEL Peak (%LEL)");
        }

        //boolean to bit
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

        //check status
        public string status(string a,string b)
        {
            if (a == "1"&&b =="1")
            {
               
                return "High";
            }
            else if(a=="0"&&b=="1")
            {
                return "Low";
            }
            else
            {
                return "";
            }
        }


        public void eventlog_parsing()
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
                textBox1.Text = fileFullName + "\n";
                StreamReader sr = ReadFile(filePath, fileName);

                progressBar1_Init();

                string sn = "";
               
                sr.Close();

                BinaryReader rdr = new BinaryReader(File.Open(filePath + @"\" + fileName, FileMode.Open));
                
                byte[] Header_bytes = new byte[2];
                byte[] info_bytes = new byte[18];

                rdr.BaseStream.Position = 0; int i = 0; int row = 0;
                dt = new DataTable();
                
                SetUpData();

                while (rdr.BaseStream.Position < l)
                {
                    do
                    {
                        Header_bytes[i] = rdr.ReadByte();
                    } while (Header_bytes[i] == 255) ;
                        if (i > 0 && Char.ConvertFromUtf32(Header_bytes[i - 1]) == "S" && Char.ConvertFromUtf32(Header_bytes[i]) == "N")
                    {
                        progressBar1.PerformStep();
                        rdr.ReadByte();
                        byte[] bytes = new byte[15];
                        sn = ""; int n = 0;
                        for (int x = 0; x < 12; x++)
                        {
                            sn += Char.ConvertFromUtf32(rdr.ReadByte());
                        }
                        rdr.ReadBytes(65);
                        info_bytes[0] = 255;
                        while (n < 15)
                        {
                            bytes[n] = rdr.ReadByte();
                            if (n > 0 && Char.ConvertFromUtf32(bytes[n - 1]) == "z" && Char.ConvertFromUtf32(bytes[n]) == "e")
                            {
                                rdr.ReadBytes(6);
                                info_bytes = rdr.ReadBytes(18);
                                break;
                            }
                            n++;
                        }
                        
                        while (info_bytes[0] != 255)
                        {

                            BitArray myBytes = new BitArray(info_bytes);
                            String[] info_bits = new String[7];
                            info_bits[0] = make_bit(myBytes[7]);
                            for (int k = 1; k <= 6; k++)
                            {
                                info_bits[1] = make_bit(myBytes[k]) + info_bits[1];
                            }

                            for (int k = 13; k <= 15; k++)
                            {
                                info_bits[2] = make_bit(myBytes[k]) + info_bits[2];
                            }
                            info_bits[2] = make_bit(myBytes[0]) + info_bits[2];
                            for (int k = 8; k <= 12; k++)
                            {
                                info_bits[3] = make_bit(myBytes[k]) + info_bits[3];
                            }
                            for (int k = 20; k <= 23; k++)
                            {
                                info_bits[4] = make_bit(myBytes[k]) + info_bits[4];
                            }
                            for (int k = 30; k < 32; k++)
                            {
                                info_bits[5] = make_bit(myBytes[k]) + info_bits[5];
                            }
                            for (int k = 16; k <= 19; k++)
                            {
                                info_bits[5] = make_bit(myBytes[k]) + info_bits[5];
                            }
                            for (int k = 24; k <= 29; k++)
                            {
                                info_bits[6] = make_bit(myBytes[k]) + info_bits[6];
                            }
                            int hour = Convert.ToInt32(info_bits[4], 2);
                            if (info_bits[0].Equals("1"))
                            {
                                hour += 12;
                            }
                            string date_time = (2000 + Convert.ToInt32(info_bits[1], 2)) + "-" + (Convert.ToInt32(info_bits[2], 2)).ToString("D2") + "-" + (Convert.ToInt32(info_bits[3], 2)).ToString("D2")
                            + " " + (hour).ToString("D2") + ":" + (Convert.ToInt32(info_bits[5], 2)).ToString("D2") + ":" + (Convert.ToInt32(info_bits[6], 2)).ToString("D2");

                            dt.Rows.Add(sn, date_time, "Peak Exposure", info_bytes[5], status(make_bit(myBytes[52]), make_bit(myBytes[53])), info_bytes[8], status(make_bit(myBytes[76]), make_bit(myBytes[77])), info_bytes[11], status(make_bit(myBytes[100]), make_bit(myBytes[101])), (float)info_bytes[14] / 10, status(make_bit(myBytes[124]), make_bit(myBytes[125])), info_bytes[17]);


                            row++;
                            info_bytes = rdr.ReadBytes(18);
                        }
                        

                    }
                    
                    i += 1;
                    i = i % 2;

                }rdr.Close();
                if (progressBar1.Value >= progressBar1.Maximum)
                {
                    MessageBox.Show("success!");
                    
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
