using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace X07_Total
{
    public partial class RModifyForm : Form
    {
        public MainForm mform;
        DataTable dtMD1;
        DataTable dtMD2;
        DataTable dtMD3;
        DataTable dtMD4;

        public RModifyForm(X07_Total.MainForm parent)
        {
            InitializeComponent();
            mform = parent;
        }

        private void RModifyForm_Load(object sender, EventArgs e)
        {
            for (int i = 0; i < 24; i++)
            {
                dataGridView_M1.Rows.Add();
                dataGridView_M1.Rows[i].Cells[0].Value = (i + 1).ToString();
                dataGridView_M1.Rows[i].Cells[1].Value = Data.GetConfigStr(Data.RconfigPath, "R_Channel_" + i.ToString(), "modify1");
                dataGridView_M1.Rows[i].Cells[2].Value = Data.GetConfigStr(Data.RconfigPath, "R_Channel_" + i.ToString(), "modify2");

                dataGridView_M2.Rows.Add();
                dataGridView_M2.Rows[i].Cells[0].Value = (i + 1 + 24).ToString();

                dataGridView_M2.Rows[i].Cells[1].Value = Data.GetConfigStr(Data.RconfigPath, "R_Channel_" + (i + 24).ToString(), "modify1");
                dataGridView_M2.Rows[i].Cells[2].Value = Data.GetConfigStr(Data.RconfigPath, "R_Channel_" + (i + 24).ToString(), "modify2");

                dataGridView_M3.Rows.Add();
                dataGridView_M3.Rows[i].Cells[0].Value = (i + 1 + 48).ToString();

                dataGridView_M3.Rows[i].Cells[1].Value = Data.GetConfigStr(Data.RconfigPath, "R_Channel_" + (i + 48).ToString(), "modify1");
                dataGridView_M3.Rows[i].Cells[2].Value = Data.GetConfigStr(Data.RconfigPath, "R_Channel_" + (i + 48).ToString(), "modify2");

                dataGridView_M4.Rows.Add();
                dataGridView_M4.Rows[i].Cells[0].Value = (i + 1 + 72).ToString();

                dataGridView_M4.Rows[i].Cells[1].Value = Data.GetConfigStr(Data.RconfigPath, "R_Channel_" + (i + 72).ToString(), "modify1");
                dataGridView_M4.Rows[i].Cells[2].Value = Data.GetConfigStr(Data.RconfigPath, "R_Channel_" + (i + 72).ToString(), "modify2");
            }

            dataGridView_M1.AllowUserToAddRows = false;
            dataGridView_M2.AllowUserToAddRows = false;
            dataGridView_M3.AllowUserToAddRows = false;
            dataGridView_M4.AllowUserToAddRows = false;
        }

        private void btn_MLoad_Click(object sender, EventArgs e)
        {
            String Path = Program.GetStartupPath() + @"参数修正码本\";
            openFileDialog1.InitialDirectory = Path;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                MyLog.Info("载入R修正码本成功！");

                string[] content = File.ReadAllLines(openFileDialog1.FileName);
                string[] temp = new string[3];

                if (content.Length >= 96)
                {
                    for (int i = 0; i < 96; i++)
                    {
                        temp = content[i].Split(',');
                        //    Data.value_a[i] = double.Parse(temp[1].Trim());
                        //    Data.value_b[i] = double.Parse(temp[2].Trim());
                        if (i >= 0 && i < 24)
                        {//改写savaconfig
                            dataGridView_M1.Rows[i].Cells[1].Value = double.Parse(temp[1].Trim());
                            dataGridView_M1.Rows[i].Cells[2].Value = double.Parse(temp[2].Trim());
                            Data.SaveConfigStr(Data.RconfigPath, "R_Channel_" + (i).ToString(), "modify1", dataGridView_M1.Rows[i].Cells[1].FormattedValue.ToString());
                            Data.SaveConfigStr(Data.RconfigPath, "R_Channel_" + (i).ToString(), "modify2", dataGridView_M1.Rows[i].Cells[2].FormattedValue.ToString());
                        }
                        else if (i >= 24 && i < 48)
                        {
                            dataGridView_M2.Rows[i - 24].Cells[1].Value = double.Parse(temp[1].Trim());
                            dataGridView_M2.Rows[i - 24].Cells[2].Value = double.Parse(temp[2].Trim());

                            Data.SaveConfigStr(Data.RconfigPath, "R_Channel_" + (i).ToString(), "modify1", dataGridView_M2.Rows[i - 24].Cells[1].FormattedValue.ToString());
                            Data.SaveConfigStr(Data.RconfigPath, "R_Channel_" + (i).ToString(), "modify2", dataGridView_M2.Rows[i - 24].Cells[2].FormattedValue.ToString());
                        }
                        else if (i >= 48 && i < 72)
                        {
                            dataGridView_M3.Rows[i - 48].Cells[1].Value = double.Parse(temp[1].Trim());
                            dataGridView_M3.Rows[i - 48].Cells[2].Value = double.Parse(temp[2].Trim());
                            Data.SaveConfigStr(Data.RconfigPath, "R_Channel_" + (i).ToString(), "modify1", dataGridView_M2.Rows[i - 48].Cells[1].FormattedValue.ToString());
                            Data.SaveConfigStr(Data.RconfigPath, "R_Channel_" + (i).ToString(), "modify2", dataGridView_M2.Rows[i - 48].Cells[2].FormattedValue.ToString());
                        }
                        else if (i >= 72 && i < 96)
                        {
                            dataGridView_M4.Rows[i - 72].Cells[1].Value = double.Parse(temp[1].Trim());
                            dataGridView_M4.Rows[i - 72].Cells[2].Value = double.Parse(temp[2].Trim());
                            Data.SaveConfigStr(Data.RconfigPath, "R_Channel_" + (i).ToString(), "modify1", dataGridView_M2.Rows[i - 72].Cells[1].FormattedValue.ToString());
                            Data.SaveConfigStr(Data.RconfigPath, "R_Channel_" + (i).ToString(), "modify2", dataGridView_M2.Rows[i - 72].Cells[2].FormattedValue.ToString());
                        }
                        else
                        {
                            //Nothing happens
                        }
                    }

                }
            }
        }

        private void btn_MOK_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 24; i++)
            {
                Data.value_R1[i] = int.Parse(dataGridView_M1.Rows[i].Cells[1].FormattedValue.ToString());
                Data.value_R2[i] = int.Parse(dataGridView_M1.Rows[i].Cells[2].FormattedValue.ToString());

                Data.value_R1[i + 24] = int.Parse(dataGridView_M2.Rows[i].Cells[1].FormattedValue.ToString());
                Data.value_R2[i + 24] = int.Parse(dataGridView_M2.Rows[i].Cells[2].FormattedValue.ToString());

                Data.value_R1[i + 48] = int.Parse(dataGridView_M3.Rows[i].Cells[1].FormattedValue.ToString());
                Data.value_R2[i + 48] = int.Parse(dataGridView_M3.Rows[i].Cells[2].FormattedValue.ToString());

                Data.value_R1[i + 72] = int.Parse(dataGridView_M4.Rows[i].Cells[1].FormattedValue.ToString());
                Data.value_R2[i + 72] = int.Parse(dataGridView_M4.Rows[i].Cells[2].FormattedValue.ToString());
            }
            for (int i = 0; i < 96; i++) {
                Data.SaveConfigStr(Data.RconfigPath, "R_Channel_" + i.ToString(), "modify1", Data.value_R1[i].ToString());
                Data.SaveConfigStr(Data.RconfigPath, "R_Channel_" + i.ToString(), "modify2", Data.value_R2[i].ToString());
            }
            MyLog.Info("成功加载DA修正参数");

            this.Close();
        }

        private void dataGridView_M1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                int i = e.RowIndex;
                if (e.ColumnIndex == 1)
                {
                    Data.SaveConfigStr(Data.RconfigPath, "R_Channel_" + i.ToString(), "modify1", dataGridView_M1.Rows[i].Cells[1].FormattedValue.ToString());
                }
                if (e.ColumnIndex == 2)
                {
                    Data.SaveConfigStr(Data.RconfigPath, "R_Channel_" + i.ToString(), "modify2", dataGridView_M1.Rows[i].Cells[1].FormattedValue.ToString());
                }
            }
        }

        private void btn_MSave_Click(object sender, EventArgs e)
        {
            String Path = Program.GetStartupPath() + @"参数修正码本\";
            if (!Directory.Exists(Path))
                Directory.CreateDirectory(Path);

            saveFileDialog1.InitialDirectory = Path;

            saveFileDialog1.Filter = "文本文件(*.txt)|*.txt|All files(*.*)|*.*";
            saveFileDialog1.FilterIndex = 1;
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                String ModifyStr1 = "";
                String ModifyStr2 = "";
                String ModifyStr3 = "";
                String ModifyStr4 = "";
                for (int i = 0; i < 24; i++)
                {
                    ModifyStr1 += dataGridView_M1.Rows[i].Cells[0].FormattedValue.ToString()
                        + ","
                        + dataGridView_M1.Rows[i].Cells[1].FormattedValue.ToString()
                        + ","
                        + dataGridView_M1.Rows[i].Cells[2].FormattedValue.ToString()
                        + "\r\n";

                    ModifyStr2 += dataGridView_M2.Rows[i].Cells[0].FormattedValue.ToString()
                        + ","
                        + dataGridView_M2.Rows[i].Cells[1].FormattedValue.ToString()
                        + ","
                        + dataGridView_M2.Rows[i].Cells[2].FormattedValue.ToString()
                        + "\r\n";
                    ModifyStr3 += dataGridView_M3.Rows[i].Cells[0].FormattedValue.ToString()
                        + ","
                        + dataGridView_M3.Rows[i].Cells[1].FormattedValue.ToString()
                        + ","
                        + dataGridView_M3.Rows[i].Cells[2].FormattedValue.ToString()
                        + "\r\n";
                    ModifyStr4 += dataGridView_M4.Rows[i].Cells[0].FormattedValue.ToString()
                        + ","
                        + dataGridView_M4.Rows[i].Cells[1].FormattedValue.ToString()
                        + ","
                        + dataGridView_M4.Rows[i].Cells[2].FormattedValue.ToString()
                        + "\r\n";
                }
                string localFilePath = saveFileDialog1.FileName.ToString(); //获得文件路径 

                FileStream file0 = new FileStream(localFilePath, FileMode.Create);
                StreamWriter sw = new StreamWriter(file0);
                sw.WriteLine(ModifyStr1 + ModifyStr2 + ModifyStr3 + ModifyStr4);
                sw.Flush();
                sw.Close();
                file0.Close();
                MessageBox.Show("存储文件成功！", "保存文件");
            }
        }
    }
}
