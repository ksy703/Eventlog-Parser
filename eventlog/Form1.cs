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
        string fileName; long l;
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

        //import button click
        private void button1_Click(object sender, EventArgs e)
        {
            dataGridView1.Columns.Clear();
            eventlog_parsing_max_xt();

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
        private string status(string bits)
        {
            string status="";
            if (bits.Substring(1).Equals("1"))
            {
                status += "Latched; ";
            }

            int alarm_Status = Convert.ToInt32(bits.Substring(2,4),2);
            switch (alarm_Status)
            {
                case 1:
                    status += "zeroing"; break;
                case 2:
                    status += "spanning"; break;
                case 3:
                    status += "Error alarm"; break;
                case 4:
                    status += "Error Acknowledged"; break;
                case 8:
                    status += "Low alarm"; break;
                case 9:
                    status += "Low alarm Acknowledged"; break;
                case 10:
                    status += "TWA alarm"; break;
                case 11:
                    status += "STEL alarm"; break;
                case 12:
                    status += "High alarm"; break;
                case 13:
                    status += "Multi alarm"; break;
                default:
                    status += ""; break;
            }
            return status;
        }
        //gas reading 값 계산
        private float gas_reading(string pre,string upper_byte,string lower_byte)
        {
            Console.WriteLine(upper_byte+lower_byte);
            int num = Convert.ToInt32((upper_byte + lower_byte), 2);
            int precision = Convert.ToInt32(pre.Substring(6, 2),2);
            float gas;
            switch (precision)
            {
                case 1:
                    gas = num / 10; break;
                case 2:
                    gas = num / 100; break;
                case 3:
                    gas = num / 1000; break;
                default:
                    gas = num; break;
            }
            if (pre.Substring(0).Equals("1"))
            {
                gas *= (-1);
            }return gas;
        }


        //file 선택 후 binary stream 읽고 parsing
        public void eventlog_parsing_max_xt()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "file open";
            ofd.FileName = "test";
            ofd.Filter = "event log 파일|*.evl";

            DialogResult dr = ofd.ShowDialog();
            if (dr == DialogResult.OK)
            {
                fileName = ofd.SafeFileName;
                string filePath = ofd.FileName;
                textBox1.Text = filePath;
                FileInfo fi = new FileInfo(filePath);
                l = fi.Length;

                progressBar1_Init();
                
                BinaryReader rdr = new BinaryReader(File.Open(filePath, FileMode.Open));

                byte[] Header_bytes = new byte[2];
                byte[] info_bytes = new byte[18];

                rdr.BaseStream.Position = 0; int i = 0; string sn;

                dt = new DataTable();
                SetUpData();

                while (rdr.BaseStream.Position < l)
                {
                    do
                    {
                        Header_bytes[i] = rdr.ReadByte();
                    } while (Header_bytes[i] == 255);

                    if (i > 0 && Char.ConvertFromUtf32(Header_bytes[i - 1]) == "S" && Char.ConvertFromUtf32(Header_bytes[i]) == "N")
                    {
                        progressBar1.PerformStep();
                        rdr.ReadByte();
                        byte[] bytes = new byte[80];
                        sn = ""; int n = 1; int h_cnt = 0;
                        byte s;
                        //serial number
                        while(rdr.BaseStream.Position<l)
                        {
                            s = rdr.ReadByte();
                            if (s != 13)
                            {
                                sn += Char.ConvertFromUtf32(s);
                            }
                            else if(rdr.ReadByte()==10)
                            {
                                break;
                            }
                        }
                        //header
                        while (rdr.BaseStream.Position < l)
                        {
                            s = rdr.ReadByte();
                            if (s == 13&&rdr.ReadByte()==10)
                            {
                                h_cnt++;
                            }
                            if (h_cnt == 4)
                            {
                                break;
                            }
                        }
                        //eventlog parsing
                        info_bytes = rdr.ReadBytes(18);
                        
                        while (info_bytes[0] != 255)
                        {
                            string[] reverse = new string[18];
                            string date_bytes;
                            BitArray bits = new BitArray(info_bytes);
                            for (n = 0; n < 144; n++)
                            {
                                reverse[n / 8] = make_bit(bits[n]) + reverse[n / 8];
                            }

                            date_bytes = reverse[0] + reverse[1] + reverse[2] + reverse[3];

                            int hour = Convert.ToInt32(date_bytes.Substring(16, 4), 2);

                            if (date_bytes.Substring(0, 1).Equals("1"))
                            {
                                hour += 12;
                            }

                            string date_time = (2000 + Convert.ToInt32(date_bytes.Substring(1, 6), 2)) + "-" + (Convert.ToInt32(date_bytes.Substring(7, 4), 2)).ToString("D2") + "-" + (Convert.ToInt32(date_bytes.Substring(11, 5), 2)).ToString("D2")
                            + " " + (hour).ToString("D2") + ":" + (Convert.ToInt32(date_bytes.Substring(20, 6), 2)).ToString("D2") + ":" + (Convert.ToInt32(date_bytes.Substring(26, 6), 2)).ToString("D2");

                            dt.Rows.Add(sn, date_time, "Peak Exposure", Convert.ToInt32((reverse[4] + reverse[5]).Substring(1, 15), 2), status(reverse[6]), gas_reading(reverse[6],reverse[7],reverse[8]), status(reverse[9]), gas_reading(reverse[9],reverse[10],reverse[11]), status(reverse[12]), gas_reading(reverse[12], reverse[13], reverse[14]), status(reverse[15]), gas_reading(reverse[15],reverse[16],reverse[17]));

                            
                            info_bytes = rdr.ReadBytes(18);
                        }


                    }

                    i += 1;
                    i = i % 2;

                }
                rdr.Close();
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
                        if (dr.Cells[i].Value == null)
                        {
                            dr.Cells[i].Value = "";
                        }
                        value = dr.Cells[i].Value.ToString();
                        //replace comma with spaces
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
            writeCSV(dataGridView1, fileName + ".csv");
            MessageBox.Show("Converted successfully to *.csv format");
        }
    }
}
