using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace X07_Total
{
    public partial class FrameEPDU : Form
    {
        public MainForm mform;
        public int channel = 0;
        public bool RealTimeTag = false;
        public bool ReviewTag = false;

        public bool FirstIn = true;
        public bool NeedAddTag = false;

        public int RightMPDU = 1;
        public int EmptyMPDU = 0;
        public int WrongMPDU = 0;
        public int WrongEPDU = 0;


        class MPDU_Two_Struct
        {
            public byte[] MPDU_Head1 = new byte[2];
            public byte[] MPDU_Body1 = new byte[1002];
            public bool MPDU1_Used = false;
            public string Pointer1;
            public int Pointer1Value;
            public void GetMUDU_Pointer1()
            {
                Pointer1 = Convert.ToString(this.MPDU_Head1[0], 2).PadLeft(8, '0').Substring(5, 3) + Convert.ToString(this.MPDU_Head1[1], 2).PadLeft(8, '0');
                Pointer1Value = Convert.ToInt32(Pointer1, 2);
            }

            public byte[] MPDU_Head2 = new byte[2];
            public byte[] MPDU_Body2 = new byte[1002];
            public bool MPDU2_Used = false;
            public string Pointer2;
            public int Pointer2Value;
            public void GetMUDU_Pointer2()
            {
                Pointer2 = Convert.ToString(this.MPDU_Head2[0], 2).PadLeft(8, '0').Substring(5, 3) + Convert.ToString(this.MPDU_Head2[1], 2).PadLeft(8, '0');
                Pointer2Value = Convert.ToInt32(Pointer2, 2);
            }

            public byte[] Temp_EPDUS = new byte[2048];
            public int Temp_EPDUS_Count = 0;
        }
        MPDU_Two_Struct TempMPDU = new MPDU_Two_Struct();

//        public List<string> APIDList;
//        public Dictionary<string, Queue<byte[]>> Apid_EPDU_Dictionary;

        public delegate void UpdateDataGrid(ref EPDU_Struct myEPDU,ref List<string> APIDList,
            ref Dictionary<string, Queue<byte[]>> Apid_EPDU_Dictionary,ref Dictionary<string, BinaryWriter> myDictionary);

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

        public FrameEPDU()
        {
            InitializeComponent();
        }

        //FileStream EPDU_File;
        //BinaryWriter bw;

        public string Save_path;
        private void FrameEPDU_Load(object sender, EventArgs e)
        {
            Data.init(Data.Channel);
            comboBox_channel.SelectedIndex = Data.Channel;
            channel = Data.Channel;
            Save_path = Program.GetStartupPath() + @"数传机箱数据\";
            if (!Directory.Exists(Save_path))
                Directory.CreateDirectory(Save_path);
            Save_path = Save_path + @"解析出的EPDU包\";
            if (!Directory.Exists(Save_path))
                Directory.CreateDirectory(Save_path);

            //string timestr = string.Format("{0}-{1:D2}-{2:D2} {3:D2}：{4:D2}：{5:D2}", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            //string filename0 = Save_path + timestr + ".dat";
            //EPDU_File = new FileStream(filename0, FileMode.Create);
            //bw = new BinaryWriter(EPDU_File);
        }

        private void FrameEPDU_FormClosing(object sender, FormClosingEventArgs e)
        {
            RealTimeTag = false;
            ReviewTag = false;
            Thread.Sleep(500);

            switch (channel)
            {
                case 1:
                    Data.TBChan1Used = false;

                    foreach (var item in Data.myDictionary1)
                    {
                        item.Value.Close();
                    }
                    Data.myDictionary1.Clear();


                    break;
                case 2:
                    Data.TBChan2Used = false;
                    foreach (var item in Data.myDictionary2)
                    {
                        item.Value.Close();
                    }
                    Data.myDictionary2.Clear();
                    break;
                case 0:
                    Data.MOXAChanUsed = false;
                    foreach (var item in Data.myDictionary3)
                    {
                        item.Value.Close();
                    }
                    Data.myDictionary3.Clear();
                    break;
                default:
                    break;
            }

            //bw.Close();
            //EPDU_File.Close();

            foreach (var item in apidformList)
            {
                item.Close();
            }

            this.Dispose();
            this.Close();
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                comboBox1.Enabled = false;
                textBox3.Enabled = false;
                button1.Enabled = false;
                //  textBox2.Enabled = true;
                comboBox_channel.Enabled = true;
            }
            else
            {
                comboBox1.Enabled = true;
                textBox3.Enabled = true;
                button1.Enabled = true;
                // textBox2.Enabled = false;
                comboBox_channel.Enabled = false;
            }
        }

        private void comboBox_channel_SelectedIndexChanged(object sender, EventArgs e)
        {
            string chan_str = comboBox_channel.Text.ToString();
            switch (chan_str)
            {
                case "同步下行1":
                    if (Data.TBChan1Used)
                    {
                        comboBox_channel.BackColor = Color.Red;
                        button_Start.Enabled = false;
                    }
                    else
                    {
                        channel = 1;
                        comboBox_channel.BackColor = SystemColors.Window;

                        button_Start.Enabled = true;
                    }
                    break;
                case "同步下行2":
                    if (Data.TBChan2Used)
                    {
                        comboBox_channel.BackColor = Color.Red;
                        button_Start.Enabled = false;
                    }
                    else
                    {
                        channel = 2;
                        comboBox_channel.BackColor = SystemColors.Window;

                        button_Start.Enabled = true;
                    }
                    break;
                case "串口下行":
                    if (Data.MOXAChanUsed)
                    {
                        comboBox_channel.BackColor = Color.Red;
                        button_Start.Enabled = false;
                    }
                    else
                    {
                        channel = 0;
                        comboBox_channel.BackColor = SystemColors.Window;
                        button_Start.Enabled = true;
                    }
                    break;
                default:
                    channel = 1;
                    break;
            }
            Trace.WriteLine("EPDU查看器选择实时通道" + channel.ToString());
        }

        private void button_Start_Click(object sender, EventArgs e)
        {
            if (button_Start.Text == "启动")
            {
                button_Start.Text = "停止";

                switch (channel)
                {
                    case 1:
                        Data.TBChan1Used = true;
                        break;
                    case 2:
                        Data.TBChan2Used = true;
                        break;
                    case 0:
                        Data.MOXAChanUsed = true;
                        break;
                    default:
                        break;

                }

                comboBox_channel.Enabled = false;

                RightMPDU = 0;
                WrongEPDU = 0;
                WrongMPDU = 0;
                EmptyMPDU = 0;

                if (radioButton1.Checked)
                {
                    new Thread(() => { FunRealTime(channel); }).Start();
                    RealTimeTag = true;
                    new Thread(() => { Fun_MPDUCounts(); }).Start();
                }
                else if(radioButton2.Checked)
                {                    
                    ReviewTag = true;
                    new Thread(() => { FunReview(); }).Start();

                    new Thread(() => { FunRealTime(99); }).Start();
                    RealTimeTag = true;
                    new Thread(() => { Fun_MPDUCounts(); }).Start();

                }
                else
                {

                }
            }
            else
            {
                DealWithLastMPDU();
                button_Start.Text = "启动";

                switch (channel)
                {
                    case 1:
                        Data.TBChan1Used = false;
                        break;
                    case 2:
                        Data.TBChan2Used = false;
                        break;
                    case 0:
                        Data.MOXAChanUsed = false;
                        break;
                    default:
                        break;

                }
                comboBox_channel.Enabled = true;

                RealTimeTag = false;
                ReviewTag = false;
            }
        }

        public Queue<byte[]> DataQueue_Review = new Queue<byte[]>();
        private void FunReview()
        {
            FileStream file = new FileStream(textBox3.Text, FileMode.Open);
            BinaryReader br = new BinaryReader(file);
            int TotalFileLen = (int)file.Length;

            int pos = 0;

            int speed = int.Parse(comboBox1.Text);
            int sleepTime = 1000 / speed;

            while (ReviewTag)
            {
                if (pos + 1024 <= TotalFileLen)
                {
                    byte[] TempRead = new byte[1024];
                    //br.Read(TempRead, pos, 1024);
                    Array.Copy(br.ReadBytes(1024), TempRead, 1024);
                    pos += 1024;

                    DataQueue_Review.Enqueue(TempRead);

                    Thread.Sleep(sleepTime);
                }

            }

            file.Close();

        }

        private void Fun_MPDUCounts()
        {
            while (RealTimeTag)
            {
                this.dataGridView2.Rows[0].Cells[0].Value = RightMPDU;
                this.dataGridView2.Rows[0].Cells[1].Value = EmptyMPDU;
                this.dataGridView2.Rows[0].Cells[2].Value = WrongMPDU;
                this.dataGridView2.Rows[0].Cells[3].Value = WrongEPDU;
                Thread.Sleep(500);
            }
        }

        private void DealWithLastMPDU()
        {
            UpdateDataGrid UpdateDG = new UpdateDataGrid(UpdataDataGrid1);

            if (TempMPDU.MPDU1_Used)
            {
                TempMPDU.MPDU1_Used = false;
                int len = 1002 - TempMPDU.Pointer1Value;
                byte[] Deal_LastMPDU = new byte[len];
                Array.Copy(TempMPDU.MPDU_Body1, TempMPDU.Pointer1Value, Deal_LastMPDU, 0, len);

                while (len > 6)
                {
                    EPDU_Struct myEPDU = new EPDU_Struct();

                    Array.Copy(Deal_LastMPDU, 0, myEPDU.EPDU_Head, 0, 6);
                    Array.Copy(Deal_LastMPDU, 6, Deal_LastMPDU, 0, len - 6);
                    len -= 6;
                    myEPDU.DealEPDUHead();
                    if (myEPDU.Count > 249)
                    {
                        WrongEPDU += 1;
                        Trace.WriteLine(myEPDU.EPDU_Head[0]);
                        Trace.WriteLine(myEPDU.EPDU_Head[1]);
                        Trace.WriteLine(myEPDU.EPDU_Head[2]);
                        Trace.WriteLine(myEPDU.EPDU_Head[3]);
                        Trace.WriteLine(myEPDU.EPDU_Head[4]);
                        Trace.WriteLine(myEPDU.EPDU_Head[5]);
                        Trace.WriteLine("EPDU包长大于249，出错跳出");
                        break;
                    }

                    if (len > myEPDU.Count + 1)
                    {
                        Array.Copy(Deal_LastMPDU, 0, myEPDU.EPDU_Body, 0, myEPDU.Count + 1);
                        Array.Copy(Deal_LastMPDU, myEPDU.Count + 1, Deal_LastMPDU, 0, len - myEPDU.Count - 1);
                        len -= (myEPDU.Count + 1);

                        //将一个EPDU显示出来
                        //   dataGridView1.Invoke(UpdateDG, myEPDU);
                        switch (channel)
                        {
                            case 1:
                                dataGridView1.Invoke(UpdateDG, myEPDU, Data.APIDList, Data.Apid_EPDU_Dictionary, Data.myDictionary1);
                                break;
                            case 2:
                                dataGridView1.Invoke(UpdateDG, myEPDU, Data.APIDList2, Data.Apid_EPDU_Dictionary2, Data.myDictionary2);
                                break;
                            case 0:
                                dataGridView1.Invoke(UpdateDG, myEPDU, Data.APIDList3, Data.Apid_EPDU_Dictionary3, Data.myDictionary3);
                                break;
                            default:
                                dataGridView1.Invoke(UpdateDG, myEPDU, Data.APIDList, Data.Apid_EPDU_Dictionary, Data.myDictionary1);
                                break;
                        }
                    }
                    else
                    {
                        Trace.WriteLine("有帧头，但长度不足myEpdu.Count+1");
                    }

                }
            }
        }
        private void FunRealTime(int chan)
        {
            Trace.WriteLine("进入--实时解析遥控包线程" + chan.ToString() + "通道");
            Queue<byte[]> DataQueue;
            switch (chan)
            {
                case 0:
                    DataQueue = Data.DataQueue3;
                    break;
                case 1:
                    DataQueue = Data.DataQueue1;
                    break;
                case 2:
                    DataQueue = Data.DataQueue2;
                    break;
                case 99:
                    DataQueue = DataQueue_Review;
                    break;
                default:
                    DataQueue = Data.DataQueue1;
                    break;
            }

            byte[] TwoMPDU = new byte[2048];
            while (RealTimeTag)
            {
                if (DataQueue.Count > 0)
                {
                    Trace.WriteLine(DataQueue.Count);
                    if (TempMPDU.MPDU1_Used == false)
                    {
                        TempMPDU.MPDU1_Used = true;
                        byte[] temp = DataQueue.Dequeue();

                        Array.Copy(temp, 18, TempMPDU.MPDU_Head1, 0, 2);
                        Array.Copy(temp, 20, TempMPDU.MPDU_Body1, 0, 1002);

                        String BeiYong1 = Convert.ToString(TempMPDU.MPDU_Head1[0], 2).PadLeft(8, '0').Substring(0, 5);
                        if (BeiYong1 == "00000")
                        {
                            TempMPDU.GetMUDU_Pointer1();
                            if (TempMPDU.Pointer1 == "11111111110")
                            {
                                //全是填充数据
                                EmptyMPDU += 1;
                                TempMPDU.MPDU1_Used = false;
                            }
                            if (TempMPDU.Pointer1 == "11111111111")
                            {
                                //
                                WrongMPDU += 1;
                                TempMPDU.MPDU1_Used = false;
                            }

                            if (TempMPDU.Pointer1Value > 1001)
                            {
                                WrongMPDU += 1;
                                TempMPDU.MPDU1_Used = false;
                            }
                        }
                        else
                        {
                            WrongMPDU += 1;
                            TempMPDU.MPDU1_Used = false;
                        }
                    }
                    else if (TempMPDU.MPDU2_Used == false)
                    {
                        TempMPDU.MPDU2_Used = true;
                        byte[] temp = DataQueue.Dequeue();
                        Array.Copy(temp, 18, TempMPDU.MPDU_Head2, 0, 2);
                        Array.Copy(temp, 20, TempMPDU.MPDU_Body2, 0, 1002);

                        String BeiYong2 = Convert.ToString(TempMPDU.MPDU_Head2[0], 2).PadLeft(8, '0').Substring(0, 5);
                        if (BeiYong2 == "00000")
                        {
                            TempMPDU.GetMUDU_Pointer2();
                            if (TempMPDU.Pointer2 == "11111111110")
                            {
                                //全是填充数据
                                EmptyMPDU += 1;
                                TempMPDU.MPDU2_Used = false;
                            }
                            if (TempMPDU.Pointer2 == "11111111111")
                            {
                                //全是填充数据
                                WrongMPDU += 1;
                                TempMPDU.MPDU2_Used = false;
                            }
                            if (TempMPDU.Pointer2Value > 1001)
                            {
                                WrongMPDU += 1;
                                TempMPDU.MPDU2_Used = false;
                            }
                        }
                        else
                        {
                            WrongMPDU += 1;
                            TempMPDU.MPDU2_Used = false;
                        }
                    }
                    else
                    {
                //        Trace.WriteLine("MPDU两次正确，进入处理");
                    }

                    if (TempMPDU.MPDU1_Used && TempMPDU.MPDU2_Used)
                    {
                        TempMPDU.GetMUDU_Pointer1();
                        if (TempMPDU.Pointer1 == "00000000000")      //第一个字节就是源包数据
                        {
                            RightMPDU += 1;
                            Array.Copy(TempMPDU.MPDU_Body1, 0, TempMPDU.Temp_EPDUS, 0, 1002);
                            TempMPDU.Temp_EPDUS_Count += 1002;
                        }
                        else
                        {
                            int Pointer_addr = Convert.ToInt32(TempMPDU.Pointer1, 2);
                            Array.Copy(TempMPDU.MPDU_Body1, Pointer_addr, TempMPDU.Temp_EPDUS, 0, 1002 - Pointer_addr);
                            TempMPDU.Temp_EPDUS_Count = 1002 - Pointer_addr;
                        }

                        TempMPDU.GetMUDU_Pointer2();
                        if (TempMPDU.Pointer2 == "00000000000")      //第一个字节就是源包数据
                        {

                        }
                        else
                        {
                            int Pointer_addr = Convert.ToInt32(TempMPDU.Pointer2, 2);
                            Array.Copy(TempMPDU.MPDU_Body2, 0, TempMPDU.Temp_EPDUS, TempMPDU.Temp_EPDUS_Count, Pointer_addr);
                            TempMPDU.Temp_EPDUS_Count += Pointer_addr;
                        }

                        byte[] Deal_TwoMPDU = new byte[TempMPDU.Temp_EPDUS_Count];
                        Array.Copy(TempMPDU.Temp_EPDUS, Deal_TwoMPDU, TempMPDU.Temp_EPDUS_Count);
                        DealWith_Temp_MPDUS(Deal_TwoMPDU);

                        //将MPDU2给到MPDU1，清空Temp_EPDUS
                        Array.Copy(TempMPDU.MPDU_Head2, TempMPDU.MPDU_Head1, 2);
                        Array.Copy(TempMPDU.MPDU_Body2, TempMPDU.MPDU_Body1, 1002);
                        TempMPDU.MPDU1_Used = true;
                        TempMPDU.MPDU2_Used = false;
                        TempMPDU.Temp_EPDUS_Count = 0;
                        for (int i = 0; i < 2048; i++) TempMPDU.Temp_EPDUS[i] = 0;
                    }
                }
            }
            Trace.WriteLine("退出--实时解析遥控包线程");
        }

        private void DealWith_Temp_MPDUS(byte[] Deal_TwoMPDU)
        {
            UpdateDataGrid UpdateDG = new UpdateDataGrid(UpdataDataGrid1);
            int len = Deal_TwoMPDU.Length;

            while (len > 6)
            {
         //       Trace.WriteLine("Deal_TwoMPUD中的数据长度是：" + len.ToString());

                EPDU_Struct myEPDU = new EPDU_Struct();

                Array.Copy(Deal_TwoMPDU, 0, myEPDU.EPDU_Head, 0, 6);
                Array.Copy(Deal_TwoMPDU, 6, Deal_TwoMPDU, 0, len - 6);
                len -= 6;
                myEPDU.DealEPDUHead();
                if (myEPDU.Count > 249)
                {
         //           Trace.WriteLine(myEPDU.APID);
                    if (myEPDU.APID == "11111111111")
                    {
         //               Trace.WriteLine("收到超过249的空闲EPDU包！");
                    }
                    else
                    {
                        WrongEPDU += 1;
        //                Trace.WriteLine("EPDU包长大于249，出错跳出");
                        break;
                    }

                }

                if (len >= myEPDU.Count + 1)
                {
                    Array.Copy(Deal_TwoMPDU, 0, myEPDU.EPDU_Body, 0, myEPDU.Count + 1);
                    Array.Copy(Deal_TwoMPDU, myEPDU.Count + 1, Deal_TwoMPDU, 0, len - myEPDU.Count - 1);
                    len -= (myEPDU.Count + 1);

                    //将一个EPDU显示出来
                    switch (channel) {
                        case 1:
                            dataGridView1.Invoke(UpdateDG, myEPDU,Data.APIDList,Data.Apid_EPDU_Dictionary, Data.myDictionary1);
                            break;
                        case 2:
                            dataGridView1.Invoke(UpdateDG, myEPDU, Data.APIDList2,Data.Apid_EPDU_Dictionary2, Data.myDictionary2);
                            break;
                        case 0:
                            dataGridView1.Invoke(UpdateDG, myEPDU, Data.APIDList3, Data.Apid_EPDU_Dictionary3, Data.myDictionary3);
                            break;
                        default:
                            dataGridView1.Invoke(UpdateDG, myEPDU, Data.APIDList, Data.Apid_EPDU_Dictionary, Data.myDictionary1);
                            break;
                    }
                }
                else
                {
           //         Trace.WriteLine("有帧头，但长度不足myEpdu.Count+1");
                }
            }
         //   Trace.WriteLine("剩余长度不足6Byte");

        }

        private void UpdataDataGrid1(ref EPDU_Struct myEPDU,ref List<string> APIDList,
            ref Dictionary<string, Queue<byte[]>> Apid_EPDU_Dictionary,ref Dictionary<string, BinaryWriter> myDictionary)
        {
            //int apid_int = Convert.ToInt32(textBox2.Text, 16);
            //string apid_s = Convert.ToString(apid_int, 2).PadLeft(11, '0');

            dataGridView1.Rows[0].Cells[0].Value = myEPDU.Version;
            dataGridView1.Rows[0].Cells[1].Value = myEPDU.Type;
            dataGridView1.Rows[0].Cells[2].Value = myEPDU.FuDT;
            dataGridView1.Rows[0].Cells[3].Value = myEPDU.APID;
            dataGridView1.Rows[0].Cells[4].Value = myEPDU.DivdTag;
            dataGridView1.Rows[0].Cells[5].Value = myEPDU.BagCount;
            dataGridView1.Rows[0].Cells[6].Value = myEPDU.CountStr;


            if (myEPDU.APID.Substring(0, 8) == "00001111")
            {
                byte[] Temp_Send2Udp = new byte[6 + myEPDU.EPDU_Body.Length];
                Array.Copy(myEPDU.EPDU_Head, 0, Temp_Send2Udp, 0, 6);
                Array.Copy(myEPDU.EPDU_Body, 0, Temp_Send2Udp, 6, myEPDU.EPDU_Body.Length);
                Data.DataQueue_UDP2Server.Enqueue(Temp_Send2Udp);
                
            }

            string str = "";
            for (int i = 0; i < myEPDU.Count; i++)
            {
                str += myEPDU.EPDU_Body[i].ToString("x2");
            }
            textBox1.AppendText(str + "\n");
            if (textBox1.Lines.Count() > 50)
                textBox1.Clear();

            //switch (chan)
            //{
            //    case 1:
            //        APIDList = Data.APIDList;
            //        Apid_EPDU_Dictionary = Data.Apid_EPDU_Dictionary;
            //        break;
            //    case 2:
            //        APIDList = Data.APIDList2;
            //        Apid_EPDU_Dictionary = Data.Apid_EPDU_Dictionary2;
            //        break;
            //    case 99:
            //        APIDList = Data.APIDList3;
            //        Apid_EPDU_Dictionary = Data.Apid_EPDU_Dictionary3;
            //        break;
            //    default:
            //        APIDList = Data.APIDList;
            //        Apid_EPDU_Dictionary = Data.Apid_EPDU_Dictionary;
            //        break;
            //}

            if (APIDList.IndexOf(myEPDU.APID) < 0)
            {
                APIDList.Add(myEPDU.APID);
                Queue<byte[]> myEPDUqueue = new Queue<byte[]>();
                Apid_EPDU_Dictionary.Add(myEPDU.APID, myEPDUqueue);

                string timestr = string.Format("{0}-{1:D2}-{2:D2} {3:D2}：{4:D2}：{5:D2}", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
                string filename0 = Save_path + myEPDU.APID + "_" + timestr + ".dat";
                FileStream ApidFile = new FileStream(filename0, FileMode.Create);
                BinaryWriter bw1 = new BinaryWriter(ApidFile);
                Trace.WriteLine("此时的通道channel："+channel.ToString());
                myDictionary.Add(myEPDU.APID, bw1);

            }

            if (myDictionary.ContainsKey(myEPDU.APID))
            {
                myDictionary[myEPDU.APID].Write(myEPDU.EPDU_Head);
                myDictionary[myEPDU.APID].Write(myEPDU.EPDU_Body);
                myDictionary[myEPDU.APID].Flush();

                byte[] tempEPDU = new byte[6 + myEPDU.Count + 1];
                Array.Copy(myEPDU.EPDU_Head, 0, tempEPDU, 0, 6);
                Array.Copy(myEPDU.EPDU_Body, 0, tempEPDU, 6, myEPDU.Count + 1);
                Apid_EPDU_Dictionary[myEPDU.APID].Enqueue(tempEPDU);
            }
            else
            {
                Trace.WriteLine("未找到APID对应的File文件!!Error!!!");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            String Path = Program.GetStartupPath() + @"数传机箱数据\";
            openFileDialog1.InitialDirectory = Path;
            Trace.WriteLine(openFileDialog1.InitialDirectory);

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                this.textBox3.Text = openFileDialog1.FileName;
            }
        }

        List<FrameAPID> apidformList = new List<FrameAPID>();
        private void textBox1_Click(object sender, EventArgs e)
        {
            FrameAPID apidform = new FrameAPID(this);
            apidform.Show();
            apidformList.Add(apidform);

        }
    }
}
