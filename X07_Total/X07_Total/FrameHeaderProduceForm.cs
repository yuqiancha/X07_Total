using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace X07_Total
{
    public partial class FrameHeaderProduceForm : Form
    {
        public X07_Total.MainForm mform;
        public FrameHeaderProduceForm(X07_Total.MainForm parent)
        {
            InitializeComponent();
            mform = parent;

            dataGridView2.Rows.Add(1);
            dataGridView2.AllowUserToAddRows = false;

        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            String HeaderStr = dataGridView2.Rows[0].Cells[0].FormattedValue.ToString() +
                   dataGridView2.Rows[0].Cells[1].FormattedValue.ToString() +
                   dataGridView2.Rows[0].Cells[2].FormattedValue.ToString()[0];


           int temp1 = Convert.ToInt32(dataGridView2.Rows[0].Cells[3].FormattedValue.ToString(), 16);
            String GuoChengTag = Convert.ToString(temp1, 2).PadLeft(11, '0');
            GuoChengTag = GuoChengTag.Substring(GuoChengTag.Length - 11);//防止超出11bit，取最低11bit


       //     String temp3 = string.Format("{0:X}", Convert.ToInt32(HeaderStr + GuoChengTag, 2));//.PadLeft(4, '0');
            String temp3 = string.Format("{0:X}", Convert.ToInt32(HeaderStr + GuoChengTag, 2)).PadLeft(4, '0');


            switch (mform.Rs422_HeaderChn_Name)
            {
                case "textBox_422_YK1":
                    mform.textBox_422_YK1.Text = temp3;
                    break;
                case "textBox_422_YK2":
                    mform.textBox_422_YK2.Text = temp3;
                    break;
                case "textBox_422_YK3":
                    mform.textBox_422_YK3.Text = temp3;
                    break;
                case "textBox_422_YK4":
                    mform.textBox_422_YK4.Text = temp3;
                    break;
                case "textBox_422_YK5":
                    mform.textBox_422_YK5.Text = temp3;
                    break;
                case "textBox_422_YK6":
                    mform.textBox_422_YK6.Text = temp3;
                    break;
                case "textBox_422_YK7":
                    mform.textBox_422_YK7.Text = temp3;
                    break;
                case "textBox_422_YK8":
                    mform.textBox_422_YK8.Text = temp3;
                    break;
                case "textBox_422_YK9":
                    mform.textBox_422_YK9.Text = temp3;
                    break;
                case "textBox_422_YK10":
                    mform.textBox_422_YK10.Text = temp3;
                    break;
                case "textBox_422_YK11":
                    mform.textBox_422_YK11.Text = temp3;
                    break;
                case "textBox_422_YK12":
                    mform.textBox_422_YK12.Text = temp3;
                    break;
                default:
                    break;

            }


            this.Close();
        }
    }
}
