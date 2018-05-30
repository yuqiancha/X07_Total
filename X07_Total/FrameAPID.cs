using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace X07_Total
{
    public partial class FrameAPID : Form
    {
        public bool ThreadOn = false;

        public class EPDU_Struct
        {
            public string Version;
            public string Type;
            public string FuDT;
            public string APID;
            public string DivdTag;
            public string BagCount;
            public int Count;
            public string CountStr;
            public byte[] EPDU_Head = new byte[6];

            public byte[] EPDU_Body;
            public string HeJiaoYan;
            public void DealEPDUHead()
            {
                this.Version = Convert.ToString(this.EPDU_Head[0], 2).PadLeft(8, '0').Substring(0, 3);
                this.Type = Convert.ToString(this.EPDU_Head[0], 2).PadLeft(8, '0').Substring(3, 1);
                this.FuDT = Convert.ToString(this.EPDU_Head[0], 2).PadLeft(8, '0').Substring(4, 1);
                this.APID = Convert.ToString(this.EPDU_Head[0], 2).PadLeft(8, '0').Substring(5, 3) + Convert.ToString(this.EPDU_Head[1], 2).PadLeft(8, '0');
                this.DivdTag = Convert.ToString(this.EPDU_Head[2], 2).PadLeft(8, '0').Substring(0, 2);
                this.BagCount = Convert.ToString(this.EPDU_Head[2], 2).PadLeft(8, '0').Substring(2, 6) + Convert.ToString(this.EPDU_Head[3], 2).PadLeft(8, '0');
                this.Count = this.EPDU_Head[4] * 256 + this.EPDU_Head[5];
                this.CountStr = Count.ToString();
                this.EPDU_Body = new byte[Count + 1];
            }
        }

        public FrameEPDU myFrameEPDU;
        public FrameAPID(X07_Total.FrameEPDU parent)
        {
            myFrameEPDU = parent;
            InitializeComponent();
        }
        string apid_s;
        List<string> AlreadyOnApid;

        private void button_Start_Click(object sender, EventArgs e)
        {
            Trace.WriteLine("开始APID,myFrameEPDU.channel = "+myFrameEPDU.channel);
            switch (myFrameEPDU.channel)
            {
                case 1:
                    AlreadyOnApid = Data.AlreadyOnApid;
                    break;
                case 2:
                    AlreadyOnApid = Data.AlreadyOnApid2;
                    break;
                case 3:
                    AlreadyOnApid = Data.AlreadyOnApid3;
                    break;
                default:
                    AlreadyOnApid = Data.AlreadyOnApid;
                    break;
            }

            if (button_Start.Text == "启动")
            {
                try
                {
                    int apid_int = Convert.ToInt32(textBox2.Text, 16);
                    apid_s = Convert.ToString(apid_int, 2).PadLeft(11, '0');
                    textBox2.BackColor = SystemColors.WindowText;
                }
                catch(Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                    textBox2.BackColor = Color.Red;
                    return;
                }
                if (AlreadyOnApid.IndexOf(apid_s) < 0)
                {
                    AlreadyOnApid.Add(apid_s);
                    textBox2.BackColor = SystemColors.Window;

                    new Thread(() => { DealWithEpduQueue(apid_s,myFrameEPDU.channel); }).Start();
                    ThreadOn = true;

                    button_Start.Text = "停止";
                }
                else
                {
                    Trace.WriteLine("Already On!");
                    textBox2.BackColor = Color.Red;                   
                }

            }
            else
            {
                ThreadOn = false;
                button_Start.Text = "启动";

                if (AlreadyOnApid.IndexOf(apid_s) >= 0)
                {
                    AlreadyOnApid.Remove(apid_s);
                }
            }
        }

        private void FrameAPID_Load(object sender, EventArgs e)
        {
            Trace.WriteLine(myFrameEPDU.channel);


        }


        private void DealWithEpduQueue(string apid,int chan)
        {
            Trace.WriteLine("DealWithEpudQueue +" + apid + "-" + chan.ToString());
            List<string> APIDList;
            List<string> AlreadyOnApid;
            Dictionary<string, Queue<byte[]>> Apid_EPDU_Dictionary;
            switch (chan)
            {
                case 1:
                    APIDList = Data.APIDList;
                    AlreadyOnApid = Data.AlreadyOnApid;
                    Apid_EPDU_Dictionary = Data.Apid_EPDU_Dictionary;
                    break;
                case 2:
                    APIDList = Data.APIDList2;
                    AlreadyOnApid = Data.AlreadyOnApid2;
                    Apid_EPDU_Dictionary = Data.Apid_EPDU_Dictionary2;
                    break;
                case 0:
                    APIDList = Data.APIDList3;
                    AlreadyOnApid = Data.AlreadyOnApid3;
                    Apid_EPDU_Dictionary = Data.Apid_EPDU_Dictionary3;
                    break;
                default:
                    APIDList = Data.APIDList;
                    AlreadyOnApid = Data.AlreadyOnApid;
                    Apid_EPDU_Dictionary = Data.Apid_EPDU_Dictionary;
                    break;
            }

            while (ThreadOn)
            {
                if (APIDList.IndexOf(apid) >= 0)
                {
                    if (Apid_EPDU_Dictionary[apid].Count() > 0)
                    {
                        EPDU_Struct myEPDU = new EPDU_Struct();

                        byte[] tempEPDU = Apid_EPDU_Dictionary[apid].Dequeue();

                        Trace.WriteLine(tempEPDU.Count());
                        Array.Copy(tempEPDU, 0, myEPDU.EPDU_Head, 0, 6);
                        myEPDU.DealEPDUHead();
                        Array.Copy(tempEPDU, 6, myEPDU.EPDU_Body, 0, tempEPDU.Count() - 6);

                        dataGridView1.Rows[0].Cells[0].Value = myEPDU.Version;
                        dataGridView1.Rows[0].Cells[1].Value = myEPDU.Type;
                        dataGridView1.Rows[0].Cells[2].Value = myEPDU.FuDT;
                        dataGridView1.Rows[0].Cells[3].Value = myEPDU.APID;
                        dataGridView1.Rows[0].Cells[4].Value = myEPDU.DivdTag;
                        dataGridView1.Rows[0].Cells[5].Value = Convert.ToInt32(myEPDU.BagCount, 2);
                        dataGridView1.Rows[0].Cells[6].Value = myEPDU.CountStr;

                        string str = "";
                        for (int i = 0; i < myEPDU.Count; i++)
                        {
                            str += myEPDU.EPDU_Body[i].ToString("x2");
                        }
                        textBox1.AppendText(str + "\n");
                        if (textBox1.Lines.Count() > 20)
                            textBox1.Clear();
                    }
                }
                else
                {
                    Thread.Sleep(500);
                    Trace.WriteLine("111");
                }
            }
        }

        private void FrameAPID_FormClosing(object sender, FormClosingEventArgs e)
        {
            ThreadOn = false;
            Thread.Sleep(501);
        }
    }
}
