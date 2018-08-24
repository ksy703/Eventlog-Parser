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
    public class MicroClip
    {
        public DataTable dt;
        public string fileName; public string filePath; long fileLength;
        string bump = "No";

        //setting datatable columns
        public void SetUpData()
        {
            dt.Columns.Add("Serial Number");
            dt.Columns.Add("Start Time");
            dt.Columns.Add("Event Type");
            dt.Columns.Add("Bump?");
            dt.Columns.Add("H2S Peak (ppm)");
            dt.Columns.Add("CO Peak (ppm)");
            dt.Columns.Add("O2 Peak (%Vol)");
            dt.Columns.Add("LEL Peak (%LEL)");
            dt.Columns.Add("Duration");
            dt.Columns.Add("Device Status");
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

        //check status(byte[14],byte[15])
        public string status(string bits)
        {
            string status = "";
            for (int b = 0; b < 16; b++)
            {
                if (bits.Substring(b, 1) == "1")
                {
                    switch (b)
                    {
                        case 15: status += "Down-scale Alarm "; break;
                        case 14: status += "TWA Alarm "; break;
                        case 13: status += "STEL Alarm "; break;
                        case 12: status += "Low Alarm "; break;
                        case 11: status += "High Alarm "; break;
                        case 10: status += "Multi Alarm "; break;
                        case 9: status += "Battery Low "; break;
                        case 8: status += "Reserved "; break;
                        case 7: status += "Auto Zeroing "; break;
                        case 6: status += "Calibrating "; break;
                        case 5: bump = "Yes"; break;
                        default: status += ""; break;
                    }
                }
            }
            return status;
        }
        //file 선택
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

                if (!fileName.Contains("MC"))
                {
                    MessageBox.Show("Wrong file!");
                }
                else
                {

                    string[] log_distinct_bits = new string[fileLength / 8];

                    BinaryReader rdr = new BinaryReader(File.Open(filePath, FileMode.Open));
                    byte[] Header_bytes = new byte[2];
                    byte[] info_bytes = new byte[16];

                    int i = 0;

                    SetUpData();

                    while (rdr.BaseStream.Position < fileLength)
                    {
                        do
                        {
                            Header_bytes[i] = rdr.ReadByte();
                        } while (Header_bytes[i] == 0xff);

                        if (i > 0 && Char.ConvertFromUtf32(Header_bytes[i - 1]) == "S" && Char.ConvertFromUtf32(Header_bytes[i]) == "N")
                        {
                            string sn = ""; int header_cnt = 0; string size = ""; string dock_time = "";
                            DateTime DockTime = new DateTime();
                            byte s;

                            //header
                            while (rdr.BaseStream.Position < fileLength)
                            {
                                s = rdr.ReadByte();
                                //serial number
                                if (header_cnt == 0 && s == 0x3a)
                                {
                                    s = rdr.ReadByte();
                                    while (s != 0x0d)
                                    {
                                        sn += Char.ConvertFromUtf32(s);
                                        s = rdr.ReadByte();
                                    }
                                }
                                //DockTime
                                if (header_cnt == 1 && s == 0x3a)
                                {
                                    s = rdr.ReadByte();
                                    while (s != 0x0d)
                                    {
                                        dock_time += Char.ConvertFromUtf32(s);
                                        s = rdr.ReadByte();
                                    }
                                    string[] logtime = dock_time.Split(':', ' ', '-');
                                    DockTime = new DateTime(int.Parse(logtime[0]), int.Parse(logtime[1]), int.Parse(logtime[2]), int.Parse(logtime[3]), int.Parse(logtime[4]), int.Parse(logtime[5]));

                                }
                                //Size
                                if (header_cnt == 4 && s == 0x3a)
                                {
                                    s = rdr.ReadByte();
                                    while (s != 0x0d)
                                    {
                                        size += Char.ConvertFromUtf32(s);
                                        s = rdr.ReadByte();
                                    }
                                }
                                if (s == 0x0d && rdr.ReadByte() == 0x0a)
                                {
                                    header_cnt++;
                                }
                                if (header_cnt == 5)
                                {
                                    break;
                                }
                            }

                            int sz = Convert.ToInt32(size);

                            //timeStartsSeconds
                            string[] reverse = new string[4];
                            info_bytes = rdr.ReadBytes(4);
                            BitArray bits = new BitArray(info_bytes);

                            for (int n = 0; n < 32; n++)
                            {
                                reverse[n / 8] = make_bit(bits[n]) + reverse[n / 8];
                            }
                            uint timeStartSeconds = Convert.ToUInt32(reverse[3] + reverse[2] + reverse[1] + reverse[0], 2) * 1000;

                            //eventlog parsing
                            info_bytes = rdr.ReadBytes(16);
                            sz -= 16;
                            while (info_bytes[0] != 0xff)
                            {
                                reverse = new string[16];
                                string distinct = "";
                                bits = new BitArray(info_bytes);
                                for (int n = 0; n < 128; n++)
                                {
                                    reverse[n / 8] = make_bit(bits[n]) + reverse[n / 8];
                                    distinct += make_bit(bits[n]);
                                }
                                //중복 제외
                                if (!log_distinct_bits.Contains(distinct))
                                {
                                    log_distinct_bits[dt.Rows.Count] = distinct;
                                    uint log_milliseconds = Convert.ToUInt32(reverse[3] + reverse[2] + reverse[1] + reverse[0], 2) * 1000;
                                    int devicetime = (int)log_milliseconds - (int)timeStartSeconds;
                                    DateTime date_time = DockTime.AddMilliseconds(devicetime);

                                    ///////////////////////////////////////////////////////////////////////////
                                    // device date_time = DockTime  -  timeStartSeconds  +  log_milliseconds //
                                    ///////////////////////////////////////////////////////////////////////////

                                    bump = "No";

                                    string event_type = status(reverse[15] + reverse[14]);

                                    String H2S = (Convert.ToInt16(reverse[5] + reverse[4], 2) / 10.0).ToString("0.0");
                                    String CO = (Convert.ToInt16(reverse[7] + reverse[6], 2) / 10.0).ToString("0.0");
                                    String O2 = (Convert.ToInt16(reverse[9] + reverse[8], 2) / 10.0).ToString("0.0");
                                    String LEL = (Convert.ToInt16(reverse[11] + reverse[10], 2) / 10.0).ToString("0.0");

                                    int Duration = Convert.ToUInt16(reverse[12], 2);

                                    dt.Rows.Add(sn, date_time.ToString("yyyy/MM/dd HH:mm:ss"), event_type, bump, H2S, CO, O2, LEL, Duration, "Active");
                                }
                                if (sz < 16)
                                {
                                    break;
                                }
                                sz -= 16;
                                info_bytes = rdr.ReadBytes(16);
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
