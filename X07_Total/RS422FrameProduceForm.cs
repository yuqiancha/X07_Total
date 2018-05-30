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
using System.Diagnostics;


namespace X07_Total
{
    public partial class RS422FrameProduceForm : Form
    {
        public MainForm mform;
        int DataCount = 0;
        public RS422FrameProduceForm(X07_Total.MainForm parent)
        {
            InitializeComponent();
            mform = parent;
            dataGridView1.Rows.Add(1);
            dataGridView1.AllowUserToAddRows = false;

            dataGridView2.Rows.Add(1);
            dataGridView2.AllowUserToAddRows = false;
        }

        String ZhenStr;
        private void button1_Click(object sender, EventArgs e)
        {
            String tempJYBstr = textBox1.Text;
            if (tempJYBstr.Length != 4 || tempJYBstr.Substring(0, 2) != tempJYBstr.Substring(2, 2))
            {
                this.textBox1.BackColor = Color.Red;
                DialogResult dr = MessageBox.Show("请输入正确的校验包格式", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else
            {
                mform.JYB_Str = tempJYBstr;
                this.textBox1.BackColor = Color.White;
            }

            String tempJYBtime = numericUpDown1.Text;
            int JYBtime = 500;

            bool ret = int.TryParse(tempJYBtime, out JYBtime);
            mform.JYB_Time = JYBtime;


            int temp = 0;
            if (this.textBox9.Text != null)
            {
                string str = this.textBox9.Text.Replace(" ", "");
                temp = str.Length;
            }

            if (temp < 4 || temp % 4 != 0 || temp > 243 * 2)
            {
                DialogResult dr = MessageBox.Show("请输入偶数个有效数据，数据区必须不超过242字节", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else
            {
                String DataStr = this.textBox9.Text.Replace(" ", "");

                //GuoChengTag = 11bits应用过程标识符
                int temp1 = Convert.ToInt32(dataGridView2.Rows[0].Cells[3].FormattedValue.ToString(), 16);
                String GuoChengTag = Convert.ToString(temp1, 2).PadLeft(11, '0');
                GuoChengTag = GuoChengTag.Substring(GuoChengTag.Length - 11);//防止超出11bit，取最低11bit

                //XuLieCount = 14bits包序列计数
                int temp2 = int.Parse(dataGridView2.Rows[0].Cells[5].FormattedValue.ToString());
                String XuLieCount = Convert.ToString(int.Parse(dataGridView2.Rows[0].Cells[5].FormattedValue.ToString()), 2).PadLeft(14, '0');

                //包长
                int tempbao = int.Parse(dataGridView2.Rows[0].Cells[6].FormattedValue.ToString());
                string baoLen = tempbao.ToString("x4");

                String StrBin = dataGridView2.Rows[0].Cells[0].FormattedValue.ToString() +
                    dataGridView2.Rows[0].Cells[1].FormattedValue.ToString() +
                    dataGridView2.Rows[0].Cells[2].FormattedValue.ToString()[0] +
                    GuoChengTag +
                    dataGridView2.Rows[0].Cells[4].FormattedValue.ToString().Substring(0, 2) +
                    XuLieCount;// +            
                               //dataGridView2.Rows[0].Cells[6].FormattedValue.ToString();

                String temp3 = string.Format("{0:X}", Convert.ToInt32(StrBin, 2)).PadLeft(8, '0');

                String BagStr = temp3                                                   //包识别+包序列控制
                                                                                        //+ dataGridView2.Rows[0].Cells[6].FormattedValue.ToString()          //包长
                    + baoLen
                    + DataStr                                                           //数据域
                    + dataGridView3.Rows[0].Cells[0].FormattedValue.ToString();         //和校验

                //起始字+遥测包
                ZhenStr = dataGridView1.Rows[0].Cells[0].FormattedValue.ToString() + BagStr;

                switch (mform.Rs422_Channel_Name)
                {
                    case "textBox_422_1":
                        mform.textBox_422_1.Text = ZhenStr;
                        mform.JYB_Time_1 = JYBtime;
                        break;
                    case "textBox_422_2":
                        mform.textBox_422_2.Text = ZhenStr;
                        mform.JYB_Time_2 = JYBtime;
                        break;
                    case "textBox_422_3":
                        mform.textBox_422_3.Text = ZhenStr;
                        mform.JYB_Time_3 = JYBtime;
                        break;
                    case "textBox_422_4":
                        mform.textBox_422_4.Text = ZhenStr;
                        mform.JYB_Time_4 = JYBtime;
                        break;
                    case "textBox_422_5":
                        mform.textBox_422_5.Text = ZhenStr;
                        mform.JYB_Time_5 = JYBtime;
                        break;
                    case "textBox_422_6":
                        mform.textBox_422_6.Text = ZhenStr;
                        mform.JYB_Time_6 = JYBtime;
                        break;
                    case "textBox_422_7":
                        mform.textBox_422_7.Text = ZhenStr;
                        mform.JYB_Time_7 = JYBtime;
                        break;
                    case "textBox_422_8":
                        mform.textBox_422_8.Text = ZhenStr;
                        mform.JYB_Time_8 = JYBtime;
                        break;
                    case "textBox_422_9":
                        mform.textBox_422_9.Text = ZhenStr;
                        mform.JYB_Time_9 = JYBtime;
                        break;
                    case "textBox_422_10":
                        mform.textBox_422_10.Text = ZhenStr;
                        mform.JYB_Time_10 = JYBtime;
                        break;
                    case "textBox_422_11":
                        mform.textBox_422_11.Text = ZhenStr;
                        mform.JYB_Time_11 = JYBtime;
                        break;
                    case "textBox_422_12":
                        mform.textBox_422_12.Text = ZhenStr;
                        mform.JYB_Time_12 = JYBtime;
                        break;

                    default:
                        break;

                }
                this.Close();
            }
        }

        private void RS422FrameProduceForm_Paint(object sender, PaintEventArgs e)
        {
            Console.WriteLine("here is MainForm_Paint!!!");
            Pen mypen = new Pen(Color.Black);
            mypen.Width = 1;
            mypen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;

            e.Graphics.DrawLine(mypen, label5.Location.X, label5.Location.Y, dataGridView2.Location.X, dataGridView2.Location.Y);
            e.Graphics.DrawLine(mypen, label5.Location.X + label5.Width, label5.Location.Y, dataGridView2.Location.X + dataGridView2.Width, dataGridView2.Location.Y);

            e.Graphics.DrawLine(mypen, label6.Location.X, label6.Location.Y, textBox9.Location.X, textBox9.Location.Y);
            e.Graphics.DrawLine(mypen, label6.Location.X + label6.Width, label6.Location.Y, dataGridView3.Location.X + dataGridView3.Width, dataGridView3.Location.Y);
        }

        private void RS422FrameProduceForm_Load(object sender, EventArgs e)
        {
            this.dataGridViewComboBoxColumn1.DefaultCellStyle.NullValue = "000";
            this.dataGridViewComboBoxColumn2.DefaultCellStyle.NullValue = "0";
            this.dataGridViewComboBoxColumn3.DefaultCellStyle.NullValue = "0-无副导头";
            this.dataGridViewComboBoxColumn4.DefaultCellStyle.NullValue = "11-独立包";
            this.dataGridViewTextBoxColumn1.DefaultCellStyle.NullValue = "0";
            this.dataGridViewTextBoxColumn2.DefaultCellStyle.NullValue = "0";
            this.dataGridViewTextBoxColumn3.DefaultCellStyle.NullValue = "0";
            this.dataGridViewTextBoxColumn4.DefaultCellStyle.NullValue = "";
            this.dataGridViewTextBoxColumn4.ReadOnly = true;

            dataGridView3.Rows.Add();
            dataGridView3.Rows[0].Cells[0].Value = "0000";
            dataGridView3.AllowUserToAddRows = false;
        }


        private void textBox9_TextChanged(object sender, EventArgs e)
        {
            int crc = 0;
            String DataStr = this.textBox9.Text.Replace(" ", "");
            int count = DataStr.Length / 4;
            if (DataStr.Length % 4 == 0)
            {
                DataCount = DataStr.Length / 2;
                //this.dataGridView2.Rows[0].Cells[6].Value = (DataCount - 1).ToString("x4");
                this.dataGridView2.Rows[0].Cells[6].Value = DataCount + 1;

                for (int m = 0; m < DataStr.Length / 4; m++)
                {
                    int temp = Convert.ToInt32(DataStr.Substring(m * 4, 4), 16);
                    crc ^= temp;
                }
                this.dataGridView3.Rows[0].Cells[0].Value = crc.ToString("x4");

            }
            else
            {

            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            int temp = 0;
            if (this.textBox9.Text != null)
            {
                string str = this.textBox9.Text.Replace(" ", "");
                temp = str.Length;
            }

            if (temp < 4 || temp % 4 != 0)
            {

                DialogResult dr = MessageBox.Show("请输入偶数个有效数据", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                String DataStr = this.textBox9.Text.Replace(" ", "");

                //GuoChengTag = 11bits应用过程标识符
                int temp1 = Convert.ToInt32(dataGridView2.Rows[0].Cells[3].FormattedValue.ToString(), 16);
                String GuoChengTag = Convert.ToString(temp1, 2).PadLeft(11, '0');
                GuoChengTag = GuoChengTag.Substring(GuoChengTag.Length - 11);//防止超出11bit，取最低11bit

                //XuLieCount = 14bits包序列计数
                int temp2 = int.Parse(dataGridView2.Rows[0].Cells[5].FormattedValue.ToString());
                String XuLieCount = Convert.ToString(int.Parse(dataGridView2.Rows[0].Cells[5].FormattedValue.ToString()), 2).PadLeft(14, '0');

                //包长
                int tempbao = int.Parse(dataGridView2.Rows[0].Cells[6].FormattedValue.ToString());
                string baoLen = tempbao.ToString("x4");

                String StrBin = dataGridView2.Rows[0].Cells[0].FormattedValue.ToString() +
                    dataGridView2.Rows[0].Cells[1].FormattedValue.ToString() +
                    dataGridView2.Rows[0].Cells[2].FormattedValue.ToString()[0] +
                    GuoChengTag +
                    dataGridView2.Rows[0].Cells[4].FormattedValue.ToString().Substring(0, 2) +
                    XuLieCount;// +            
                               //dataGridView2.Rows[0].Cells[6].FormattedValue.ToString();

                String temp3 = string.Format("{0:X}", Convert.ToInt32(StrBin, 2)).PadLeft(8, '0');

                String BagStr = temp3                                                   //包识别+包序列控制
                                                                                        //+ dataGridView2.Rows[0].Cells[6].FormattedValue.ToString()          //包长
                    + baoLen
                    + DataStr                                                           //数据域
                    + dataGridView3.Rows[0].Cells[0].FormattedValue.ToString();         //和校验

                //起始字+遥测包
                ZhenStr = dataGridView1.Rows[0].Cells[0].FormattedValue.ToString() + BagStr;

                String Path = mform.Path + @"异步遥控包\";
                if (!Directory.Exists(Path))
                    Directory.CreateDirectory(Path);

                saveFileDialog1.InitialDirectory = Path;

                saveFileDialog1.Filter = "文本文件(*.txt)|*.txt|All files(*.*)|*.*";
                saveFileDialog1.FilterIndex = 1;
                saveFileDialog1.RestoreDirectory = true;

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    string localFilePath = saveFileDialog1.FileName.ToString(); //获得文件路径 

                    FileStream file0 = new FileStream(localFilePath, FileMode.Create);
                    StreamWriter sw = new StreamWriter(file0);
                    sw.WriteLine(ZhenStr);
                    sw.Flush();
                    sw.Close();
                    file0.Close();
                    MessageBox.Show("存储文件成功！", "保存文件");

                }
            }
        }

        private void dataGridView2_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                if (e.ColumnIndex == 5)
                {
                    try
                    {

                        int t = int.Parse(dataGridView2.Rows[e.RowIndex].Cells[e.ColumnIndex].FormattedValue.ToString());
                        if (t < 0 || t > 16383)
                        {
                            dataGridView2.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = "0";
                        }
                    }
                    catch (Exception ex)
                    {
                        MyLog.Error(ex.Message + "请输入正确的包序列计数!");
                        dataGridView2.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = "0";
                    }
                }

                if (e.ColumnIndex == 6)
                {
                    try
                    {

                        int t = int.Parse(dataGridView2.Rows[e.RowIndex].Cells[e.ColumnIndex].FormattedValue.ToString());
                        if (t < 0 || t > 65535)
                        {
                            dataGridView2.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = "0";
                        }
                    }
                    catch (Exception ex)
                    {
                        MyLog.Error(ex.Message + "请输入正确的包长!");
                        dataGridView2.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = "0";
                    }
                }
            }
        }
    }
}
