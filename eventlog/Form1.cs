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
        public MaxXT mxt;
        public MicroClip mcp;
        public Form1()
        {
            InitializeComponent();
            mxt = new MaxXT();
            mcp = new MicroClip();
        }
        //import button click
        public void MaxXT_import_button_Click(object sender, EventArgs e)
        {
            dataGridView1.Columns.Clear();
            mxt.eventlog_parsing_max_xt();
            textBox1.Text = mxt.filePath;
            if (mxt.dt != null)
            {
                mxt.dt = mxt.dt.DefaultView.ToTable(true);
                dataGridView1.DataSource = mxt.dt;
                dataGridView1.EditMode = DataGridViewEditMode.EditProgrammatically;
            }
            
        }


        public void MicroClip_import_button_Click(object sender, EventArgs e)
        {
            dataGridView2.Columns.Clear();
            mcp.eventlog_parsing_max_xt();
            textBox2.Text = mcp.filePath;
            if (mcp.dt != null)
            {
                mcp.dt = mcp.dt.DefaultView.ToTable(true);
                dataGridView2.DataSource = mcp.dt;
                dataGridView2.EditMode = DataGridViewEditMode.EditProgrammatically;
            }
        }
        
        public void writeCSV(DataGridView gridIn, string outputFile)
        {
            //test to see if the DataGridView has any rows
            if (gridIn.RowCount > 0)
            {
                string value = "";
                DataGridViewRow dr = new DataGridViewRow();
                StreamWriter swOut = new StreamWriter(outputFile,false);

                //write header rows to csv
                for (int i = 0; i < gridIn.Columns.Count; i++)
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
                        value = value.Replace(',', ' ');
                        value = value.Replace(Environment.NewLine, " ");

                        swOut.Write(value);
                    }
                }
                swOut.Close();
                MessageBox.Show("Converted successfully to *.csv format");
            }
            else
            {
                MessageBox.Show("No data to convert");
            }
        }
        public void SaveToCSV_button_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab==tabPage1)
            {
                writeCSV(dataGridView1,mxt.fileName+".csv");
            }else if (tabControl1.SelectedTab == tabPage2)
            {
                writeCSV(dataGridView2,mcp.fileName + ".csv");
            }
        }
    }
}
