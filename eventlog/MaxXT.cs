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
    public class MaxXT
    {
        public DataTable dt;
        public string fileName; long fileLength; public string filePath;

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
        public string status(string bits)
        {
            string status = "";
            if (bits.Substring(1).Equals("1"))
            {
                status += "Latched ";
            }

            int alarm_Status = Convert.ToInt32(bits.Substring(2, 4), 2);
            switch (alarm_Status)
            {
                case 1: status += "zeroing "; break;
                case 2: status += "spanning "; break;
                case 3: status += "Error alarm "; break;
                case 4: status += "Error Acknowledged "; break;
                case 8: status += "Low alarm "; break;
                case 9: status += "Low alarm Acknowledged "; break;
                case 10: status += "TWA alarm "; break;
                case 11: status += "STEL alarm "; break;
                case 12: status += "High alarm "; break;
                case 13: status += "Multi alarm "; break;
                default: status += ""; break;
            }
            return status;
        }
        //gas reading 값 계산
        public float gas_reading(string pre, string upper_byte, string lower_byte)
        {
            int num = Convert.ToInt32((upper_byte + lower_byte), 2);
            int precision = Convert.ToInt32(pre.Substring(6, 2), 2);
            float gas;
            switch (precision)
            {
                case 1: gas = num / 10; break;
                case 2: gas = num / 100; break;
                case 3: gas = num / 1000; break;
                default: gas = num; break;
            }
            if (pre.Substring(0).Equals("1"))
            {
                gas *= (-1);
            }
            return gas;
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
                dt = new DataTable();
                fileName = ofd.SafeFileName;
                filePath = ofd.FileName;
                FileInfo fi = new FileInfo(filePath);
                fileLength = fi.Length;
                if (!fileName.Contains("MX"))
                {
                    MessageBox.Show("Wrong file!");
                }

                else
                {
                    BinaryReader rdr = new BinaryReader(File.Open(filePath, FileMode.Open));

                    byte[] Header_bytes = new byte[2];
                    byte[] info_bytes = new byte[18];

                    rdr.BaseStream.Position = 0; int i = 0;


                    SetUpData();

                    while (rdr.BaseStream.Position < fileLength)
                    {
                        do
                        {
                            Header_bytes[i] = rdr.ReadByte();
                        } while (Header_bytes[i] == 0xff);

                        if (i > 0 && Char.ConvertFromUtf32(Header_bytes[i - 1]) == "S" && Char.ConvertFromUtf32(Header_bytes[i]) == "N")
                        {
                            string sn = ""; int h_cnt = 0; string size = "";
                            byte s;

                            //header
                            while (rdr.BaseStream.Position < fileLength)
                            {
                                s = rdr.ReadByte();
                                //serial number
                                if (h_cnt == 0 && s == 0x3a)
                                {
                                    s = rdr.ReadByte();
                                    while (s != 13)
                                    {
                                        sn += Char.ConvertFromUtf32(s);
                                        s = rdr.ReadByte();
                                    }
                                }
                                //size
                                if (h_cnt == 4 && s == 0x3a)
                                {
                                    s = rdr.ReadByte();
                                    while (s != 13)
                                    {
                                        size += Char.ConvertFromUtf32(s);
                                        s = rdr.ReadByte();
                                    }

                                }
                                if (s == 0x0d && rdr.ReadByte() == 0x0a)
                                {
                                    h_cnt++;
                                }
                                if (h_cnt == 5)
                                {
                                    break;
                                }
                            }
                            //eventlog parsing
                            int sz = Convert.ToInt32(size);
                            info_bytes = rdr.ReadBytes(18);
                            sz -= 18;
                            while (info_bytes[0] != 0xff)
                            {
                                string[] reverse = new string[18];
                                string date_bytes;
                                BitArray bits = new BitArray(info_bytes);
                                for (int n = 0; n < 144; n++)
                                {
                                    reverse[n / 8] = make_bit(bits[n]) + reverse[n / 8];
                                }

                                date_bytes = reverse[0] + reverse[1] + reverse[2] + reverse[3];

                                int hour = Convert.ToInt32(date_bytes.Substring(16, 4), 2);

                                if (date_bytes.Substring(0, 1).Equals("1"))
                                {
                                    hour += 12;
                                }

                                DateTime date_time = new DateTime((2000 + Convert.ToInt32(date_bytes.Substring(1, 6), 2)), Convert.ToInt32(date_bytes.Substring(7, 4), 2), Convert.ToInt32(date_bytes.Substring(11, 5), 2), hour, Convert.ToInt32(date_bytes.Substring(20, 6), 2), Convert.ToInt32(date_bytes.Substring(26, 6), 2));

                                dt.Rows.Add(sn, date_time.ToString("yyyy/MM/dd HH:mm:ss"), "Peak Exposure", Convert.ToInt32((reverse[4] + reverse[5]).Substring(1, 15), 2), status(reverse[6]), gas_reading(reverse[6], reverse[7], reverse[8]), status(reverse[9]), gas_reading(reverse[9], reverse[10], reverse[11]), status(reverse[12]), gas_reading(reverse[12], reverse[13], reverse[14]), status(reverse[15]), gas_reading(reverse[15], reverse[16], reverse[17]));

                                if (sz < 18)
                                {
                                    break;
                                }
                                sz -= 18;
                                info_bytes = rdr.ReadBytes(18);
                            }
                        }
                        i += 1;
                        i = i % 2;
                    }
                    rdr.Close();
                }
                
            }
        }
    }
}
