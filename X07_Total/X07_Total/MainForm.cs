using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CyUSB;
using System.Threading;
using System.IO;
using System.IO.Ports;
using System.Diagnostics;
using System.Collections;
using System.Xml;
using System.Configuration;
using System.Xml.Linq;

namespace X07_Total
{
    public partial class MainForm : Form
    {
        SaveFile FileThread = null;
        FrameProduceForm myFrameProdeceForm;
        RS422FrameProduceForm myRs422FrameProduceForm;
        FrameHeaderProduceForm myHeaderProduceForm;
        DAModifyForm myDAModifyForm;
        RModifyForm myRModifyForm;

        public string file2usb = null;      //发送给422USB板卡的码本
        public string Path = null;          //程序运行的目录

        bool RecvTag = false;               //接收422USB数据的线程

        public byte[] TempStoreBuf = new byte[8192];
        public int TempStoreBufTag = 0;

        public byte[] TempStoreBuf_OC = new byte[8192];
        public int TempStoreBufTag_OC = 0;

        public ReaderWriterLockSlim Lock_DataQueue_422 = new ReaderWriterLockSlim();
        public Queue<byte[]> DataQueue_422 = new Queue<byte[]>();
        Thread RS422Thread = null;
        bool RS422Tag = false;

        public Byte[] SynByte = new byte[24];       //10个通道异步422和2个通道GNC的同步头

        public Queue<byte[]> DataQueue_SerialPort = new Queue<byte[]>();
        Thread SerialPortThread = null;
        bool SerialPortTag = false;

        public DateTime startDT;

        public ManualResetEvent RS422Event = new ManualResetEvent(true);

        public AutoResetEvent WaitRecvEvent_OC = new AutoResetEvent(false);
        public AutoResetEvent WaitRecvEvent_SC = new AutoResetEvent(false);

        public int ChannelTag = 0;
        public string Rs422_Channel_Name;           //
        public string Rs422_HeaderChn_Name;         //设置串口遥测包的通道text值

        public string JYB_Str = "2D2D";
        public int JYB_Time;
        public int JYB_Time_1;
        public int JYB_Time_2;
        public int JYB_Time_3;
        public int JYB_Time_4;
        public int JYB_Time_5;
        public int JYB_Time_6;
        public int JYB_Time_7;
        public int JYB_Time_8;
        public int JYB_Time_9;
        public int JYB_Time_10;
        public int JYB_Time_11;
        public int JYB_Time_12;
        public int[] JYB_Times;         //每个通道的响应时间,放入JYB_Time_1~JYB_Time_12
        public struct RS422_STRUCT
        {
            //状态名称
            public string RS1;
            public string RS2;
            public string RS3;
            public string RS4;
            public string RS5;
            public string RS6;
            public string RS7;
            public string RS8;
            public string RS9;
            public string RS10;
            public string RS11;
            public string RS12;
        }
        public RS422_STRUCT RS422 = new RS422_STRUCT();

        public struct VCID_STRUCT
        {
            public string Name;
            public int RecvNums;
        }

        public List<VCID_STRUCT> VcidList1 = new List<VCID_STRUCT>();

        public List<VCID_STRUCT> VcidList2 = new List<VCID_STRUCT>();

        TextBox[] myTextBox_422_YKs;               //发送到板卡的，用来做接收到422数据，同步的头
        TextBox[] myTextBox_422_Sends;              //发送到板卡的GNC，异步422参数数据

        DataGridView[] myDGVs;                        //dataGridView_V1~dataGridView_V4
        DataGridView[] myDGVs_R;                        //dataGridView_R1~dataGridView_R2
        public byte[] DAByteA = new byte[128];
        public byte[] DAByteB = new byte[128];
        public byte[] DAByteC = new byte[128];
        public byte[] DAByteD = new byte[128];

        public byte[] RByteA = new byte[96];
        public byte[] RByteB = new byte[96];
        public byte[] RByteC = new byte[96];
        public byte[] RByteD = new byte[96];

        public SerialPort ComPortSend;
        public SerialPort ComPortRecv;

        DataTable[] dtOCList;
        private DataTable dtOC1 = new DataTable();
        private DataTable dtOC2 = new DataTable();
        private DataTable dtOC3 = new DataTable();
        private DataTable dtOC4 = new DataTable();

        DataTable[] dtDAList;
        private DataTable dtDA1 = new DataTable();
        private DataTable dtDA2 = new DataTable();
        private DataTable dtDA3 = new DataTable();
        private DataTable dtDA4 = new DataTable();

        DataTable[] dtRList;
        private DataTable dtR1 = new DataTable();
        private DataTable dtR2 = new DataTable();
        private DataTable dtR3 = new DataTable();
        private DataTable dtR4 = new DataTable();

        private DataTable dtSCRecv = new DataTable();

        private DataTable dtSCVcid1 = new DataTable();
        private DataTable dtSCVcid2 = new DataTable();

        public static Queue<byte> DataQueue_TB1 = new Queue<byte>();   //处理FF01同步422第1通道的数据
        public static ReaderWriterLockSlim Lock_TB1 = new ReaderWriterLockSlim();

        public static Queue<byte> DataQueue_TB2 = new Queue<byte>();   //处理FF02同步422第2通道的数据
        public static ReaderWriterLockSlim Lock_TB2 = new ReaderWriterLockSlim();


        public static Queue<byte> DataQueue_TBSC1 = new Queue<byte>();   //处理FF03数传第1通道的数据
        public static ReaderWriterLockSlim Lock_TBSC1 = new ReaderWriterLockSlim();

        public static Queue<byte> DataQueue_TBSC2 = new Queue<byte>();   //处理FF04数传第2通道的数据
        public static ReaderWriterLockSlim Lock_TBSC2 = new ReaderWriterLockSlim();

        public static Queue<byte> DataQueue_1D0E = new Queue<byte>();   //处理FF08异步数传通道的数据
        public static ReaderWriterLockSlim Lock_1D0E = new ReaderWriterLockSlim();

        public static Queue<byte> DataQueue_1D0F = new Queue<byte>();   //处理FF08异步数传通道的数据
        public static ReaderWriterLockSlim Lock_1D0F = new ReaderWriterLockSlim();

        /// <summary>
        /// 修改AppSettings中配置
        /// </summary>
        /// <param name="key">key值</param>
        /// <param name="value">相应值</param>
        public static bool SetConfigValue(string key, string value)
        {
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings[key] != null)
                    config.AppSettings.Settings[key].Value = value;
                else
                    config.AppSettings.Settings.Add(key, value);
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public ToolStripStatusLabel[] myToolStripStatus_USB;

        string[] ToDataShow1 = new string[8];//显示同步数传1解析结果
        string[] ToDataShow2 = new string[8];//显示同步数传2解析结果
        public MainForm()
        {
            myToolStripStatus_USB = new ToolStripStatusLabel[] {toolStripStatus_usb00, toolStripStatus_usb01,
            toolStripStatus_usb02,toolStripStatus_usb03,toolStripStatus_usb04 };

            Control.CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
            Path = Program.GetStartupPath();
            //启动日志
            MyLog.richTextBox1 = richTextBox1;
            MyLog.path = Program.GetStartupPath() + @"LogData\";
            MyLog.lines = 50;
            MyLog.start();
            startDT = System.DateTime.Now;

            // Create the list of USB devices attached to the CyUSB3.sys driver.
            USB.usbDevices = new USBDeviceList(CyConst.DEVICES_CYUSB);

            //Assign event handlers for device attachment and device removal.
            USB.usbDevices.DeviceAttached += new EventHandler(UsbDevices_DeviceAttached);
            USB.usbDevices.DeviceRemoved += new EventHandler(UsbDevices_DeviceRemoved);

            USB.Init();

        }




        private void InitDatable()
        {
            #region 初始化OC
            try
            {
                dtOC1.Columns.Add("ID", typeof(Int32));
                dtOC1.Columns.Add("通道名称", typeof(String));
                dtOC1.Columns.Add("计数", typeof(Int32));
                dtOC1.Columns.Add("脉宽", typeof(Int32));
                for (int i = 0; i < 48; i++)
                {
                    DataRow dr = dtOC1.NewRow();
                    dr["ID"] = i + 1;
                    dr["通道名称"] = Data.GetConfigStr(Data.OCconfigPath, "OC_Channel_" + i.ToString(), "name");
                    dr["计数"] = 0;
                    dr["脉宽"] = 0;
                    dtOC1.Rows.Add(dr);
                }
                dataGridView_OC1.DataSource = dtOC1;
                dataGridView_OC1.AllowUserToAddRows = false;

                dtOC2.Columns.Add("ID", typeof(Int32));
                dtOC2.Columns.Add("通道名称", typeof(String));
                dtOC2.Columns.Add("计数", typeof(Int32));
                dtOC2.Columns.Add("脉宽", typeof(Int32));
                for (int i = 0; i < 48; i++)
                {
                    DataRow dr = dtOC2.NewRow();
                    dr["ID"] = i + 49;
                    dr["通道名称"] = Data.GetConfigStr(Data.OCconfigPath, "OC_Channel_" + (i + 48).ToString(), "name");
                    dr["计数"] = 0;
                    dr["脉宽"] = 0;
                    dtOC2.Rows.Add(dr);
                }
                dataGridView_OC2.DataSource = dtOC2;
                dataGridView_OC2.AllowUserToAddRows = false;

                dtOC3.Columns.Add("ID", typeof(Int32));
                dtOC3.Columns.Add("通道名称", typeof(String));
                dtOC3.Columns.Add("计数", typeof(Int32));
                dtOC3.Columns.Add("脉宽", typeof(Int32));
                for (int i = 0; i < 48; i++)
                {
                    DataRow dr = dtOC3.NewRow();
                    dr["ID"] = i + 97;
                    dr["通道名称"] = Data.GetConfigStr(Data.OCconfigPath, "OC_Channel_" + (i + 96).ToString(), "name");
                    dr["计数"] = 0;
                    dr["脉宽"] = 0;
                    dtOC3.Rows.Add(dr);
                }
                dataGridView_OC3.DataSource = dtOC3;
                dataGridView_OC3.AllowUserToAddRows = false;

                dtOC4.Columns.Add("ID", typeof(Int32));
                dtOC4.Columns.Add("通道名称", typeof(String));
                dtOC4.Columns.Add("计数", typeof(Int32));
                dtOC4.Columns.Add("脉宽", typeof(Int32));
                for (int i = 0; i < 48; i++)
                {
                    DataRow dr = dtOC4.NewRow();
                    dr["ID"] = i + 145;
                    dr["通道名称"] = Data.GetConfigStr(Data.OCconfigPath, "OC_Channel_" + (i + 144).ToString(), "name");
                    dr["计数"] = 0;
                    dr["脉宽"] = 0;
                    dtOC4.Rows.Add(dr);
                }
                dataGridView_OC4.DataSource = dtOC4;
                dataGridView_OC4.AllowUserToAddRows = false;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("初始化OC列表：" + ex.Message);
            }
            #endregion

            #region 初始化DA
            try
            {
                dtDA1.Columns.Add("ID", typeof(Int32));
                dtDA1.Columns.Add("名称", typeof(String));
                dtDA1.Columns.Add("电压", typeof(Double));
                for (int i = 0; i < 32; i++)
                {
                    DataRow dr = dtDA1.NewRow();
                    dr["ID"] = i + 1;
                    dr["名称"] = Data.GetConfigStr(Data.DAconfigPath, "DA_Channel_" + i.ToString(), "name");
                    dr["电压"] = 0;
                    dtDA1.Rows.Add(dr);
                }


                dtDA2.Columns.Add("ID", typeof(Int32));
                dtDA2.Columns.Add("名称", typeof(String));
                dtDA2.Columns.Add("电压", typeof(Double));
                for (int i = 0; i < 32; i++)
                {
                    DataRow dr = dtDA2.NewRow();
                    dr["ID"] = i + 33;
                    dr["名称"] = Data.GetConfigStr(Data.DAconfigPath, "DA_Channel_" + (i + 32).ToString(), "name");
                    dr["电压"] = 0;
                    dtDA2.Rows.Add(dr);
                }

                dtDA3.Columns.Add("ID", typeof(Int32));
                dtDA3.Columns.Add("名称", typeof(String));
                dtDA3.Columns.Add("电压", typeof(Double));
                for (int i = 0; i < 32; i++)
                {
                    DataRow dr = dtDA3.NewRow();
                    dr["ID"] = i + 65;
                    dr["名称"] = Data.GetConfigStr(Data.DAconfigPath, "DA_Channel_" + (i + 64).ToString(), "name");
                    dr["电压"] = 0;
                    dtDA3.Rows.Add(dr);
                }

                dtDA4.Columns.Add("ID", typeof(Int32));
                dtDA4.Columns.Add("名称", typeof(String));
                dtDA4.Columns.Add("电压", typeof(Double));
                for (int i = 0; i < 32; i++)
                {
                    DataRow dr = dtDA4.NewRow();
                    dr["ID"] = i + 97;
                    dr["名称"] = Data.GetConfigStr(Data.DAconfigPath, "DA_Channel_" + (i + 96).ToString(), "name");
                    dr["电压"] = 0;
                    dtDA4.Rows.Add(dr);
                }

                dataGridView_V1.DataSource = dtDA1;
                dataGridView_V2.DataSource = dtDA2;
                dataGridView_V3.DataSource = dtDA3;
                dataGridView_V4.DataSource = dtDA4;
                dataGridView_V1.AllowUserToAddRows = false;
                dataGridView_V2.AllowUserToAddRows = false;
                dataGridView_V3.AllowUserToAddRows = false;
                dataGridView_V4.AllowUserToAddRows = false;

            }
            catch (Exception ex)
            {
                Trace.WriteLine("初始化DA列表：" + ex.Message);
            }
            #endregion

            #region 初始化R
            try
            {
                dtR1.Columns.Add("ID", typeof(Int32));
                dtR1.Columns.Add("名称", typeof(String));
                dtR1.Columns.Add("电阻", typeof(Double));
                for (int i = 0; i < 24; i++)
                {
                    DataRow dr = dtR1.NewRow();
                    dr["ID"] = i + 1;
                    dr["名称"] = "电阻通道" + (i + 1).ToString();
                    dr["电阻"] = 256;
                    dtR1.Rows.Add(dr);
                }


                dtR2.Columns.Add("ID", typeof(Int32));
                dtR2.Columns.Add("名称", typeof(String));
                dtR2.Columns.Add("电阻", typeof(Double));
                for (int i = 0; i < 24; i++)
                {
                    DataRow dr = dtR2.NewRow();
                    dr["ID"] = i + 25;
                    dr["名称"] = "电阻通道" + (i + 25).ToString();
                    dr["电阻"] = 256;
                    dtR2.Rows.Add(dr);
                }

                dtR3.Columns.Add("ID", typeof(Int32));
                dtR3.Columns.Add("名称", typeof(String));
                dtR3.Columns.Add("电阻", typeof(Double));
                for (int i = 0; i < 24; i++)
                {
                    DataRow dr = dtR3.NewRow();
                    dr["ID"] = i + 49;
                    dr["名称"] = "电阻通道" + (i + 49).ToString();
                    dr["电阻"] = 256;
                    dtR3.Rows.Add(dr);
                }

                dtR4.Columns.Add("ID", typeof(Int32));
                dtR4.Columns.Add("名称", typeof(String));
                dtR4.Columns.Add("电阻", typeof(Double));
                for (int i = 0; i < 24; i++)
                {
                    DataRow dr = dtR4.NewRow();
                    dr["ID"] = i + 73;
                    dr["名称"] = "电阻通道" + (i + 73).ToString();
                    dr["电阻"] = 256;
                    dtR4.Rows.Add(dr);
                }

                dataGridView_R1.DataSource = dtR1;
                dataGridView_R2.DataSource = dtR2;
                dataGridView_R3.DataSource = dtR3;
                dataGridView_R4.DataSource = dtR4;
                dataGridView_R1.AllowUserToAddRows = false;
                dataGridView_R2.AllowUserToAddRows = false;
                dataGridView_R3.AllowUserToAddRows = false;
                dataGridView_R4.AllowUserToAddRows = false;

            }
            catch (Exception ex)
            {
                Trace.WriteLine("初始化R列表：" + ex.Message);
            }
            #endregion

            #region 初始化datagridview5
            try
            {
                dtSCRecv.Columns.Add("通道名称", typeof(String));
                dtSCRecv.Columns.Add("版本号", typeof(String));
                dtSCRecv.Columns.Add("航天器标识", typeof(String));
                dtSCRecv.Columns.Add("虚拟信道标识", typeof(String));
                dtSCRecv.Columns.Add("虚拟信道帧计数", typeof(String));
                dtSCRecv.Columns.Add("回放", typeof(String));
                dtSCRecv.Columns.Add("保留", typeof(String));
                dtSCRecv.Columns.Add("插入域", typeof(String));

                for (int i = 0; i < 2; i++)
                {
                    DataRow dr = dtSCRecv.NewRow();
                    dr["通道名称"] = "接收通道" + (i + 1).ToString();
                    dtSCRecv.Rows.Add(dr);
                }

                dataGridView5.DataSource = dtSCRecv;

                dataGridView5.AllowUserToAddRows = false;
                dataGridView5.Height = (dataGridView5.Rows[0].Height) * 2 + dataGridView5.ColumnHeadersHeight + 5;

            }
            catch (Exception ex)
            {
                Trace.WriteLine("初始化数传接收列表：" + ex.Message);
            }
            #endregion

            #region 初始化datagridview_sc1,2
            try
            {
                dtSCVcid1.Columns.Add("VCID", typeof(String));
                dtSCVcid1.Columns.Add("数量", typeof(int));
                dataGridView_sc1.DataSource = dtSCVcid1;
                dataGridView_sc1.AllowUserToAddRows = false;

                dtSCVcid2.Columns.Add("VCID", typeof(String));
                dtSCVcid2.Columns.Add("数量", typeof(int));
                dataGridView_sc2.DataSource = dtSCVcid2;
                dataGridView_sc2.AllowUserToAddRows = false;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("初始化数传接收列表：" + ex.Message);
            }
            #endregion

        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            //Set and search the device with VID-PID 04b4-1003 and if found, selects the end point
            SetDevice(false);

            if (ConfigurationManager.AppSettings["参数校准"] == "FALSE")
            {
                btn_MdfyEnable.Visible = false;
            }
            else
            {
                btn_MdfyEnable.Visible = true;
            }

            myFrameProdeceForm = new FrameProduceForm(this);
            myRs422FrameProduceForm = new RS422FrameProduceForm(this);
            myHeaderProduceForm = new FrameHeaderProduceForm(this);

            myDGVs = new DataGridView[] { dataGridView_V1, dataGridView_V2, dataGridView_V3, dataGridView_V4 };
            myDGVs_R = new DataGridView[] { dataGridView_R1, dataGridView_R2, dataGridView_R3, dataGridView_R4 };
            myTextBox_422_YKs = new TextBox[] { textBox_422_YK1 , textBox_422_YK2, textBox_422_YK3 , textBox_422_YK4 , textBox_422_YK5 , textBox_422_YK6,
            textBox_422_YK7,textBox_422_YK8,textBox_422_YK9,textBox_422_YK10,textBox_422_YK11,textBox_422_YK12};
            myTextBox_422_Sends = new TextBox[] {textBox_422_1, textBox_422_2, textBox_422_3, textBox_422_4, textBox_422_5, textBox_422_6,
                textBox_422_7, textBox_422_8, textBox_422_9, textBox_422_10, textBox_422_11, textBox_422_12};

            JYB_Times = new int[] { JYB_Time_1, JYB_Time_2, JYB_Time_3, JYB_Time_4, JYB_Time_5, JYB_Time_6, JYB_Time_7, JYB_Time_8, JYB_Time_9, JYB_Time_10, JYB_Time_11, JYB_Time_12, };

            this.radioButton1.Checked = true;
            toolStripStatusLabel2.Text = "存储路径" + Path;

            #region 初始化dataGridView1
            dataGridView1.Columns[6].Width = dataGridView1.Width - dataGridView1.Columns[0].Width - dataGridView1.Columns[1].Width
                - dataGridView1.Columns[2].Width - dataGridView1.Columns[3].Width - dataGridView1.Columns[4].Width - dataGridView1.Columns[5].Width;
            dataGridView1.Rows.Add();
            dataGridView1.Rows.Add();
            dataGridView1.Rows.Add();
            dataGridView1.Rows.Add();
            dataGridView1.Rows.Add();
            dataGridView1.Rows.Add();
            dataGridView1.Rows.Add();
            dataGridView1.Rows.Add();
            dataGridView1.Rows.Add();
            dataGridView1.Rows.Add();
            dataGridView1.Rows.Add();
            dataGridView1.Rows.Add();

            dataGridView1.Columns[6].ReadOnly = true;
            dataGridView1.ReadOnly = true;

            dataGridView1.Rows[0].Cells[0].Value = "通道1";
            dataGridView1.Rows[1].Cells[0].Value = "通道2";
            dataGridView1.Rows[2].Cells[0].Value = "通道3";
            dataGridView1.Rows[3].Cells[0].Value = "通道4";
            dataGridView1.Rows[4].Cells[0].Value = "通道5";
            dataGridView1.Rows[5].Cells[0].Value = "通道6";
            dataGridView1.Rows[6].Cells[0].Value = "通道7";
            dataGridView1.Rows[7].Cells[0].Value = "通道8";
            dataGridView1.Rows[8].Cells[0].Value = "通道9";
            dataGridView1.Rows[9].Cells[0].Value = "通道10";
            dataGridView1.Rows[10].Cells[0].Value = "GNC通道1";
            dataGridView1.Rows[11].Cells[0].Value = "GNC通道2";

            dataGridView1.AllowUserToAddRows = false;
            #endregion

            #region 初始化dataGridView2

            dataGridView2.Rows.Add();
            dataGridView2.Rows.Add();
            dataGridView2.Rows.Add();
            dataGridView2.Rows.Add();
            dataGridView2.Rows.Add();
            dataGridView2.Rows.Add();
            dataGridView2.Rows.Add();
            dataGridView2.Rows.Add();

            dataGridView2.Rows[0].Cells[0].Value = "LVDS发送1";
            dataGridView2.Rows[1].Cells[0].Value = "LVDS发送2";
            dataGridView2.Rows[2].Cells[0].Value = "LVDS发送3";
            dataGridView2.Rows[3].Cells[0].Value = "LVDS发送4";
            dataGridView2.Rows[4].Cells[0].Value = "LVDS发送5";
            dataGridView2.Rows[5].Cells[0].Value = "LVDS发送6";
            dataGridView2.Rows[6].Cells[0].Value = "LVDS发送7";
            dataGridView2.Rows[7].Cells[0].Value = "RS422发送";


            dataGridView2.AllowUserToAddRows = false;
            dataGridView2.AllowUserToResizeColumns = false;
            dataGridView2.AllowUserToResizeRows = false;

            dataGridView2.Rows[0].Cells[2].Value = ConfigurationManager.AppSettings["ShuChuan01"];
            dataGridView2.Rows[1].Cells[2].Value = ConfigurationManager.AppSettings["ShuChuan02"];
            dataGridView2.Rows[2].Cells[2].Value = ConfigurationManager.AppSettings["ShuChuan03"];
            dataGridView2.Rows[3].Cells[2].Value = ConfigurationManager.AppSettings["ShuChuan04"];
            dataGridView2.Rows[4].Cells[2].Value = ConfigurationManager.AppSettings["ShuChuan05"];
            dataGridView2.Rows[5].Cells[2].Value = ConfigurationManager.AppSettings["ShuChuan06"];
            dataGridView2.Rows[6].Cells[2].Value = ConfigurationManager.AppSettings["ShuChuan07"];
            dataGridView2.Rows[7].Cells[2].Value = ConfigurationManager.AppSettings["ShuChuan08"];

            dataGridView2.Rows[0].Cells[3].Value = ConfigurationManager.AppSettings["ShuChuan01_Rate"];
            dataGridView2.Rows[1].Cells[3].Value = ConfigurationManager.AppSettings["ShuChuan02_Rate"];
            dataGridView2.Rows[2].Cells[3].Value = ConfigurationManager.AppSettings["ShuChuan03_Rate"];
            dataGridView2.Rows[3].Cells[3].Value = ConfigurationManager.AppSettings["ShuChuan04_Rate"];
            dataGridView2.Rows[4].Cells[3].Value = ConfigurationManager.AppSettings["ShuChuan05_Rate"];
            dataGridView2.Rows[5].Cells[3].Value = ConfigurationManager.AppSettings["ShuChuan06_Rate"];
            dataGridView2.Rows[6].Cells[3].Value = ConfigurationManager.AppSettings["ShuChuan07_Rate"];
            dataGridView2.Rows[7].Cells[3].Value = ConfigurationManager.AppSettings["ShuChuan08_Rate"];

            dataGridView2.Height = (dataGridView2.Rows[0].Height) * 8 + dataGridView2.ColumnHeadersHeight + 20;

            #endregion

            #region 初始化dataGridView4

            dataGridView4.Rows.Add();
            dataGridView4.Rows.Add();


            dataGridView4.Rows[0].Cells[0].Value = "数字量遥测输出通道1";
            dataGridView4.Rows[1].Cells[0].Value = "数字量遥测输出通道2";


            dataGridView4.AllowUserToAddRows = false;
            dataGridView4.AllowUserToResizeColumns = false;
            dataGridView4.AllowUserToResizeRows = false;

            dataGridView4.Rows[0].Cells[2].Value = ConfigurationManager.AppSettings["ShuChuan01"];
            dataGridView4.Rows[1].Cells[2].Value = ConfigurationManager.AppSettings["ShuChuan02"];

            dataGridView4.Rows[0].Cells[1].Value = ConfigurationManager.AppSettings["ShuChuan01_Len"];
            dataGridView4.Rows[1].Cells[1].Value = ConfigurationManager.AppSettings["ShuChuan02_Len"];


            dataGridView4.Height = (dataGridView4.Rows[0].Height) * 2 + dataGridView4.ColumnHeadersHeight;

            #endregion

            #region 初始化dataGridView3

            dataGridView3.Rows.Add();
            dataGridView3.Rows.Add();

            dataGridView3.Rows[0].Cells[0].Value = "接收通道1";
            dataGridView3.Rows[1].Cells[0].Value = "接收通道2";

            dataGridView3.Rows[0].Cells[3].Value = Program.GetStartupPath() + @"数传机箱数据\数传接收1\";
            dataGridView3.Rows[1].Cells[3].Value = Program.GetStartupPath() + @"数传机箱数据\数传接收2\";


            dataGridView3.Rows[0].Cells[1].Value = true;
            dataGridView3.Rows[1].Cells[1].Value = true;

            dataGridView3.AllowUserToAddRows = false;
            dataGridView3.Height = (dataGridView3.Rows[0].Height) * 2 + dataGridView3.ColumnHeadersHeight + 5;

            #endregion


            #region 初始化dataGridView6,7

            for (int i = 0; i < 8; i++)
            {
                dataGridView6.Rows.Add();
                dataGridView6.Rows[i].Cells[0].Value = "通道" + (i + 1).ToString();

                dataGridView6.Rows[i].Cells[1].Value = ConfigurationManager.AppSettings["OCOutValue1_0" + (1 + i).ToString()];

            }

            dataGridView6.AllowUserToAddRows = false;

            dataGridView6.Height = (dataGridView6.Rows[0].Height) * 8 + dataGridView6.ColumnHeadersHeight + 5;


            for (int i = 0; i < 8; i++)
            {
                dataGridView7.Rows.Add();
                dataGridView7.Rows[i].Cells[0].Value = "通道" + (i + 9).ToString();
                dataGridView7.Rows[i].Cells[1].Value = ConfigurationManager.AppSettings["OCOutValue2_0" + (1 + i).ToString()];
            }

            dataGridView7.AllowUserToAddRows = false;

            dataGridView7.Height = (dataGridView7.Rows[0].Height) * 8 + dataGridView7.ColumnHeadersHeight + 5;

            #endregion

            #region 初始化dataGridView_OC

            InitDatable();
            dtOCList = new DataTable[] { dtOC1, dtOC2, dtOC3, dtOC4 };
            for (int i = 0; i < 768; i++) Data.LastOC[i] = 0;

            dtDAList = new DataTable[] { dtDA1, dtDA2, dtDA3, dtDA4 };
            dtRList = new DataTable[] { dtR1, dtR2, dtR3, dtR4 };

            #endregion

            #region 初始化listview

            ImageList imgList = new ImageList();
            imgList.ImageSize = new Size(1, 25);

            this.listView2.Items.Add("通道1");
            this.listView2.Items[0].SubItems.Add("1ACFFC1D");
            this.listView2.Items[0].SubItems.Add("0");
            this.listView2.Items[0].SubItems.Add("0");
            this.listView2.Items[0].SubItems.Add("0");
            this.listView2.Items[0].SubItems.Add("0");
            this.listView2.Items[0].SubItems.Add("0");
            this.listView2.Items[0].SubItems.Add("0");
            this.listView2.Items[0].SubItems.Add("0");
            this.listView2.Items.Add("通道2");
            this.listView2.Items[1].SubItems.Add("1ACFFC1D");
            this.listView2.Items[1].SubItems.Add("0");
            this.listView2.Items[1].SubItems.Add("0");
            this.listView2.Items[1].SubItems.Add("0");
            this.listView2.Items[1].SubItems.Add("0");
            this.listView2.Items[1].SubItems.Add("0");
            this.listView2.Items[1].SubItems.Add("0");
            this.listView2.Items[1].SubItems.Add("0");
            listView2.SmallImageList = imgList;
            listView2.Height = 25 + 25 * 2;
            listView2.Columns[8].Width = listView2.Width - listView2.Columns[0].Width - listView2.Columns[1].Width - listView2.Columns[2].Width
                - listView2.Columns[3].Width - listView2.Columns[4].Width - listView2.Columns[5].Width - listView2.Columns[6].Width - listView2.Columns[7].Width - 20;
            #endregion

            Function.Init();

            for (int i = 0; i < 32; i++)
            {
                DAByteA[0 + 4 * i] = 0x00;
                DAByteA[1 + 4 * i] = (byte)(0x40 + (i / 4));
                DAByteA[2 + 4 * i] = (byte)(((i % 4) & 0x03) << 6);
                DAByteA[3 + 4 * i] = 0x00;

                DAByteB[0 + 4 * i] = 0x00;
                DAByteB[1 + 4 * i] = (byte)(0x40 + (i / 4));
                DAByteB[2 + 4 * i] = (byte)(((i % 4) & 0x03) << 6);
                DAByteB[3 + 4 * i] = 0x00;

                DAByteC[0 + 4 * i] = 0x00;
                DAByteC[1 + 4 * i] = (byte)(0x40 + (i / 4));
                DAByteC[2 + 4 * i] = (byte)(((i % 4) & 0x03) << 6);
                DAByteC[3 + 4 * i] = 0x00;

                DAByteD[0 + 4 * i] = 0x00;
                DAByteD[1 + 4 * i] = (byte)(0x40 + (i / 4));
                DAByteD[2 + 4 * i] = (byte)(((i % 4) & 0x03) << 6);
                DAByteD[3 + 4 * i] = 0x00;
            }

            foreach (Control ct in panel1.Controls)
            {
                ct.Enabled = false;
            }

            foreach (Control ct in panel2.Controls)
            {
                ct.Enabled = false;
            }
            checkBox5.Enabled = true;
            checkBox6.Enabled = true;

            for (int i = 0; i < 8; i++)
            {
                ToDataShow1[i] = "null";
                ToDataShow2[i] = "null";
            }

            timer1.Enabled = true;
        }

        private void MainForm_ResizeBegin(object sender, EventArgs e)
        {
            Trace.WriteLine("MainForm_ResizeBegin!");
            listView2.Columns[7].Width = listView2.Width - listView2.Columns[0].Width - listView2.Columns[1].Width - listView2.Columns[2].Width
                - listView2.Columns[3].Width - listView2.Columns[4].Width - listView2.Columns[5].Width - listView2.Columns[6].Width - 20;

        }

        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            Trace.WriteLine("MainForm_Paint!");

            dataGridView_R1.Refresh();
        }

        private void dataGridView2_Paint(object sender, PaintEventArgs e)
        {
            int temp = dataGridView2.Width;
            for (int i = 0; i < dataGridView2.ColumnCount; i++)
            {
                if (i != 2)
                    temp -= dataGridView2.Columns[i].Width;
            }
            dataGridView2.Columns[2].Width = temp - dataGridView2.RowHeadersWidth;
            dataGridView2.Height = (dataGridView2.Rows[0].Height) * 8 + dataGridView2.ColumnHeadersHeight;
        }

        void UsbDevices_DeviceAttached(object sender, EventArgs e)
        {
            SetDevice(false);
        }

        /*Summary
        This is the event handler for device removal. This method resets the device count and searches for the device with VID-PID 04b4-1003
        */
        void UsbDevices_DeviceRemoved(object sender, EventArgs e)
        {
            USBEventArgs evt = (USBEventArgs)e;
            USBDevice RemovedDevice = evt.Device;

            string RemovedDeviceName = evt.FriendlyName;
            MyLog.Error(RemovedDeviceName + "板卡断开");

            int key = int.Parse(evt.ProductID.ToString("x4").Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            USB.MyDeviceList[key] = null;
            myToolStripStatus_USB[key].Image = Properties.Resources.red;

            if (button3.Text.Equals("开始读取") == false)
            {
                button3.Text = "开始读取";
                button3.BackColor = Color.Aquamarine;
            }
        }



        /*Summary
        Search the device with VID-PID 04b4-00F1 and if found, select the end point
        */
        private void SetDevice(bool bPreserveSelectedDevice)
        {
            myToolStripStatus_USB = new ToolStripStatusLabel[] {toolStripStatus_usb00, toolStripStatus_usb01,
            toolStripStatus_usb02,toolStripStatus_usb03,toolStripStatus_usb04,toolStripStatus_usb05,toolStripStatus_usb06,toolStripStatus_usb07,toolStripStatus_usb08 };

            int nDeviceList = USB.usbDevices.Count;
            for (int nCount = 0; nCount < nDeviceList; nCount++)
            {
                USBDevice fxDevice = USB.usbDevices[nCount];
                String strmsg;
                strmsg = "(0x" + fxDevice.VendorID.ToString("X4") + " - 0x" + fxDevice.ProductID.ToString("X4") + ") " + fxDevice.FriendlyName;

                int key = int.Parse(fxDevice.ProductID.ToString("x4").Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                if (USB.MyDeviceList[key] == null)
                {
                    USB.MyDeviceList[key] = (CyUSBDevice)fxDevice;

                    myToolStripStatus_USB[key].Visible = true;
                    myToolStripStatus_USB[key].Text = ConfigurationManager.AppSettings[USB.MyDeviceList[key].FriendlyName];
                    myToolStripStatus_USB[key].Image = Properties.Resources.green1;

                    MyLog.Info(USB.MyDeviceList[key].FriendlyName + ConfigurationManager.AppSettings[USB.MyDeviceList[key].FriendlyName] + "板卡连接");

                    if (key == Data.DARid) initRBoard();//初始化电阻板卡，各个通道发送1C03
                }
            }

            button3.Enabled = false;
            for (int i = 0; i < USB.MyDeviceList.Count(); i++)
            {
                if (USB.MyDeviceList[i] != null)
                {
                    button3.Enabled = true;
                    break;
                }
            }

        }

        private void InitEveryThingBefStart()
        {
            #region 清空OC界面
            foreach (var dt in dtOCList)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    dr["脉宽"] = 0;
                    dr["计数"] = 0;
                }
            }
            #endregion
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (this.button3.Text == "一键开始")
            {
                button2_Click(sender, e);

                InitEveryThingBefStart();

                for (int i = 0; i < 9; i++)
                {
                    if (USB.MyDeviceList[i] != null)
                    {
                        USB.MyDeviceList[i].Reset();
                        CyControlEndPoint CtrlEndPt = null;
                        CtrlEndPt = USB.MyDeviceList[i].ControlEndPt;
                        if (CtrlEndPt != null)
                        {
                            Register.Byte80H = (byte)(Register.Byte80H | 0x04);
                            USB.SendCMD(i, 0x80, Register.Byte80H);
                        }
                        else
                        {
                            MyLog.Error("CtrlEndPt == null!!");
                        }
                    }
                }

                //存储线程
                FileThread = new SaveFile();
                FileThread.FileInit();
                FileThread.FileSaveStart();

                MyLog.Info("开始读取");
                RecvTag = true;

                DataQueue_TB1.Clear();
                DataQueue_TB2.Clear();
                DataQueue_TBSC1.Clear();
                DataQueue_TBSC2.Clear();
                DataQueue_1D0F.Clear();
                DataQueue_1D0E.Clear();

                new Thread(() => { RecvAllUSB(); }).Start();

                this.button3.Text = "停止读取";
                this.button3.BackColor = Color.Gray;


                RS422Thread = new Thread(FunRS422);
                RS422Tag = true;
                RS422Thread.Start();

            }
            else
            {
                RS422Tag = false;
                for (int t = 0; t < 12; t++) DealRs422Tag[t] = false;

                RecvTag = false;

                MyLog.Info("停止读取");


                #region 保存按钮停止时候剩余在内存中的数据

                /*
                if(TempStoreBufTag>2078)
                {
                    if (TempStoreBuf[4] == 0xff && TempStoreBuf[5] == 0x01)
                    {
                        byte[] DataFF01 = new byte[26];
                        Array.Copy(TempStoreBuf, 4, DataFF01, 0, 26);
                        SaveFile.DataQueue_1.Enqueue(DataFF01);
                    }
                    if (TempStoreBuf[30] == 0xff && TempStoreBuf[31] == 0x02)
                    {
                        byte[] DataFF02 = new byte[1024];
                        Array.Copy(TempStoreBuf, 30, DataFF02, 0, 1024);
                        if ((DataFF02[2] == 255) && (DataFF02[3] == 255))
                            SaveFile.DataQueue_2.Enqueue(DataFF02);
                    }
                    if (TempStoreBuf[1054] == 0xff && TempStoreBuf[1055] == 0x03)
                    {
                        byte[] DataFF03 = new byte[1024];
                        Array.Copy(TempStoreBuf, 1054, DataFF03, 0, 1024);
                        if ((DataFF03[2] == 255) && (DataFF03[3] == 255))
                            SaveFile.DataQueue_3.Enqueue(DataFF03);
                    }
                    Trace.WriteLine("内存中剩余数据量{0} > 2078", TempStoreBufTag.ToString());
                }
                else if(TempStoreBufTag>1054)
                {
                    if (TempStoreBuf[4] == 0xff && TempStoreBuf[5] == 0x01)
                    {
                        byte[] DataFF01 = new byte[26];
                        Array.Copy(TempStoreBuf, 4, DataFF01, 0, 26);
                        SaveFile.DataQueue_1.Enqueue(DataFF01);
                    }
                    if (TempStoreBuf[30] == 0xff && TempStoreBuf[31] == 0x02)
                    {
                        byte[] DataFF02 = new byte[1024];
                        Array.Copy(TempStoreBuf, 30, DataFF02, 0, 1024);
                        if ((DataFF02[2] == 255) && (DataFF02[3] == 255))
                            SaveFile.DataQueue_2.Enqueue(DataFF02);
                    }
                    if (TempStoreBufTag > 1058)
                    {
                        if (TempStoreBuf[1054] == 0xff && TempStoreBuf[1055] == 0x03)
                        {
                            byte[] DataFF03 = new byte[TempStoreBufTag - 1054];
                            Array.Copy(TempStoreBuf, 1054, DataFF03, 0, TempStoreBufTag - 1054);
                            if ((DataFF03[2] == 255) && (DataFF03[3] == 255))
                                SaveFile.DataQueue_3.Enqueue(DataFF03);
                        }
                    }
                    Trace.WriteLine("内存中剩余数据量{0} > 1054", TempStoreBufTag.ToString());
                }
                else if(TempStoreBufTag>30)
                {
                    if (TempStoreBuf[4] == 0xff && TempStoreBuf[5] == 0x01)
                    {
                        byte[] DataFF01 = new byte[26];
                        Array.Copy(TempStoreBuf, 4, DataFF01, 0, 26);
                        SaveFile.DataQueue_1.Enqueue(DataFF01);
                    }
                    if (TempStoreBufTag > 34)
                    {
                        if (TempStoreBuf[30] == 0xff && TempStoreBuf[31] == 0x02)
                        {
                            byte[] DataFF02 = new byte[TempStoreBufTag - 30];
                            Array.Copy(TempStoreBuf, 30, DataFF02, 0, TempStoreBufTag - 30);
                            if ((DataFF02[2] == 255) && (DataFF02[3] == 255))
                                SaveFile.DataQueue_2.Enqueue(DataFF02);
                        }
                    }
                    Trace.WriteLine("内存中剩余数据量{0} > 30", TempStoreBufTag.ToString());
                }
                else if(TempStoreBufTag>6)
                {
                    if (TempStoreBuf[4] == 0xff && TempStoreBuf[5] == 0x01)
                    {
                        byte[] DataFF01 = new byte[TempStoreBufTag - 4];
                        Array.Copy(TempStoreBuf, 4, DataFF01, 0, TempStoreBufTag-4);
                        SaveFile.DataQueue_1.Enqueue(DataFF01);
                    }
                    Trace.WriteLine("内存中剩余数据量{0} > 4", TempStoreBufTag.ToString());
                }
                else
                {
                    Trace.WriteLine("内存中剩余字数少于4");
                }

                */
                #endregion


                button2_Click(sender, e);

            }
        }

        bool[] DealRs422Tag = new bool[12] { false, false, false, false, false, false, false, false, false, false, false, false };
        int[] TotalCount_422 = new int[12];
        int[] RightCount_422 = new int[12];
        int[] WrongCount_422 = new int[12];
        // List<List<byte>> Rs422_ChanList = new List<List<byte>>();
        List<List<byte>> Rs422_ChanList = new List<List<byte>>();

        /// <summary>
        /// FunRS422线程用于处理收到的RS422数据并显示
        /// </summary>
        private void FunRS422()
        {
            Trace.WriteLine("进入FunRS422");
            for (int i = 0; i < 12; i++) TotalCount_422[i] = 0;
            for (int i = 0; i < 12; i++) RightCount_422[i] = 0;
            for (int i = 0; i < 12; i++) WrongCount_422[i] = 0;

            List<byte> Rs422_Chan1 = new List<byte>();
            List<byte> Rs422_Chan2 = new List<byte>();
            List<byte> Rs422_Chan3 = new List<byte>();
            List<byte> Rs422_Chan4 = new List<byte>();
            List<byte> Rs422_Chan5 = new List<byte>();
            List<byte> Rs422_Chan6 = new List<byte>();
            List<byte> Rs422_Chan7 = new List<byte>();
            List<byte> Rs422_Chan8 = new List<byte>();
            List<byte> Rs422_Chan9 = new List<byte>();
            List<byte> Rs422_Chan10 = new List<byte>();
            List<byte> Rs422_Chan11 = new List<byte>();
            List<byte> Rs422_Chan12 = new List<byte>();
            Rs422_ChanList.Add(Rs422_Chan1);
            Rs422_ChanList.Add(Rs422_Chan2);
            Rs422_ChanList.Add(Rs422_Chan3);
            Rs422_ChanList.Add(Rs422_Chan4);
            Rs422_ChanList.Add(Rs422_Chan5);
            Rs422_ChanList.Add(Rs422_Chan6);
            Rs422_ChanList.Add(Rs422_Chan7);
            Rs422_ChanList.Add(Rs422_Chan8);
            Rs422_ChanList.Add(Rs422_Chan9);
            Rs422_ChanList.Add(Rs422_Chan10);
            Rs422_ChanList.Add(Rs422_Chan11);
            Rs422_ChanList.Add(Rs422_Chan12);

            for (int i = 0; i < 12; i++)
            {
                SynByte[2 * i] = (Byte)int.Parse(myTextBox_422_YKs[i].Text.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                SynByte[2 * i + 1] = (Byte)int.Parse(myTextBox_422_YKs[i].Text.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            }

            //  object synObj_listadd = new object();
            while (RS422Tag)
            {
                if (DataQueue_422.Count() > 0)
                {
                    byte[] tempbuf = new byte[24];
                    try
                    {
                        Lock_DataQueue_422.EnterReadLock();
                        tempbuf = DataQueue_422.Dequeue();
                        Lock_DataQueue_422.ExitReadLock();
                        if (tempbuf == null)
                        {
                            Trace.WriteLine("TMD收到的tempbuf就是null!!!");
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine("异常来自FunRS422中Dequeue");
                        Trace.WriteLine(ex.Message);
                        break;
                    }

                    try
                    {
                        if (tempbuf != null)
                        {
                            if (tempbuf[0] == 255)
                                Rs422_Chan1.Add(tempbuf[1]);
                            if (tempbuf[2] == 255)
                                Rs422_Chan2.Add(tempbuf[3]);
                            if (tempbuf[4] == 255)
                                Rs422_Chan3.Add(tempbuf[5]);
                            if (tempbuf[6] == 255)
                                Rs422_Chan4.Add(tempbuf[7]);
                            if (tempbuf[8] == 255)
                                Rs422_Chan5.Add(tempbuf[9]);
                            if (tempbuf[10] == 255)
                                Rs422_Chan6.Add(tempbuf[11]);
                            if (tempbuf[12] == 255)
                                Rs422_Chan7.Add(tempbuf[13]);
                            if (tempbuf[14] == 255)
                                Rs422_Chan8.Add(tempbuf[15]);
                            if (tempbuf[16] == 255)
                                Rs422_Chan9.Add(tempbuf[17]);
                            if (tempbuf[18] == 255)
                                Rs422_Chan10.Add(tempbuf[19]);
                            if (tempbuf[20] == 255)
                                Rs422_Chan11.Add(tempbuf[21]);
                            if (tempbuf[22] == 255)
                                Rs422_Chan12.Add(tempbuf[23]);
                            //  }
                        }
                        else
                        {
                            Trace.WriteLine("异常来自于tempbuf为Null");
                        }
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine("异常来自FunRS422中List.add");
                        Trace.WriteLine(e.Message);
                        break;
                    }
                }
                else
                {
                    Thread.Sleep(500);
                }
            }
            for (int t = 0; t < 12; t++)
            {
                DealRs422Tag[t] = false;
                Rs422_ChanList.RemoveAt(0);
            }
            Trace.WriteLine("退出FunRS422");
        }

        private void UpdateDataGrid(string[] dataRe, int channel)
        {
            dataGridView1.Rows[channel].Cells[2].Value = dataRe[0];
            dataGridView1.Rows[channel].Cells[3].Value = dataRe[1];
            dataGridView1.Rows[channel].Cells[4].Value = dataRe[2];
            dataGridView1.Rows[channel].Cells[5].Value = dataRe[3];
            dataGridView1.Rows[channel].Cells[6].Value = dataRe[4];
        }

        private void UpdataDataGrid_OC(Byte[] dataRe)
        {
            int muti = 2;
            for (int i = 0; i < 192; i++)
            {
                if (i >= 0 && i < 48)
                {
                    dtOC1.Rows[i]["计数"] = dataRe[4 * i] * 256 + dataRe[4 * i + 1];
                    dtOC1.Rows[i]["脉宽"] = muti * (dataRe[4 * i + 2] * 256 + dataRe[4 * i + 3]);
                }
                if (i >= 48 && i < 96)
                {
                    dtOC2.Rows[i - 48]["计数"] = dataRe[4 * i] * 256 + dataRe[4 * i + 1];
                    dtOC2.Rows[i - 48]["脉宽"] = muti * (dataRe[4 * i + 2] * 256 + dataRe[4 * i + 3]);
                }
                if (i >= 96 && i < 144)
                {
                    dtOC3.Rows[i - 96]["计数"] = dataRe[4 * i] * 256 + dataRe[4 * i + 1];
                    dtOC3.Rows[i - 96]["脉宽"] = muti * (dataRe[4 * i + 2] * 256 + dataRe[4 * i + 3]);
                }
                if (i >= 144 && i < 192)
                {
                    dtOC4.Rows[i - 144]["计数"] = dataRe[4 * i] * 256 + dataRe[4 * i + 1];
                    dtOC4.Rows[i - 144]["脉宽"] = muti * (dataRe[4 * i + 2] * 256 + dataRe[4 * i + 3]);
                }
            }

        }

        //      private void DealWithRs422Fun(ref List<byte> myList, int channel)
        private void DealWithRs422Fun(int channel)
        {
            Trace.WriteLine("进入 DealWithRs422Fun：" + channel.ToString());
            DeleUpdateDataGrid deleupdatedatagrid = new DeleUpdateDataGrid(UpdateDataGrid);

            if (Rs422_ChanList.Count() >= (channel - 1))
            {
                var myList = Rs422_ChanList[channel - 1];

                byte b1 = SynByte[2 * (channel - 1)];
                byte b2 = SynByte[2 * (channel - 1) + 1];

                while (DealRs422Tag[channel - 1])
                {
                    int t1 = myList.IndexOf(b1);
                    if (t1 >= 0)
                    {
                        if (myList.Count >= t1 + 7)
                        {
                            if (myList[t1 + 1] == b2)
                            {
                                int len = myList[t1 + 5] + myList[t1 + 4] * 256;
                                if (myList.Count >= t1 + len + 7)
                                {
                                    Trace.WriteLine("长度足够开始运算:  " + len.ToString());
                                    String tempstr = "";
                                    for (int i = t1; i < t1 + len + 7; i++)
                                    {
                                        tempstr += myList[i].ToString("x2");
                                    }
                                    SaveFile.DataQueue_asynList[channel - 1].Enqueue(DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss]") + "  " + tempstr + "\r\n");

                                    Trace.WriteLine(tempstr);

                                    //收到总包数计数+1
                                    TotalCount_422[channel - 1]++;

                                    string[] DataGridStr = new string[5];
                                    DataGridStr[0] = TotalCount_422[channel - 1].ToString();
                                    DataGridStr[1] = WrongCount_422[channel - 1].ToString();
                                    DataGridStr[2] = RightCount_422[channel - 1].ToString();
                                    DataGridStr[3] = DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss]");
                                    DataGridStr[4] = tempstr;
                                    dataGridView1.Invoke(deleupdatedatagrid, DataGridStr, channel - 1);

                                    //     Trace.WriteLine("MyList t1 = " + t1.ToString()+"len+7 = :"+(len+7).ToString());
                                    myList.RemoveRange(0, t1 + len + 7);
                                }
                            }
                        }
                    }
                    else
                    {
                        //处理当mylist中找不到头的时候
                        //myList.clear()??
                        Thread.Sleep(400);
                    }
                }
            }
            else
            {
                MyLog.Error("尚未开始读取数据，此时无法启动监测");
            }
            Trace.WriteLine("退出 DealWithRs422Fun:" + channel.ToString());
        }

        void DealWithOCFrame(ref byte[] TempBuf, ref int TempTag)
        {
            DeleUpdateDataGridOC deleupdatedatagrid = new DeleUpdateDataGridOC(UpdataDataGrid_OC);

            int Len_OC = 768;
            int Len_ShortFrame = 772;

            while (TempBuf[0] == 0xff && TempBuf[1] == 0x0 && TempTag >= Len_ShortFrame)
            {
                byte[] buf_ShortFrame = new byte[Len_ShortFrame];
                byte[] buf_ShortFrame_OC = new byte[Len_OC];//192路OC的值

                //Trace.WriteLine("---------------------收到短帧--------------");
                Array.Copy(TempBuf, 0, buf_ShortFrame, 0, Len_ShortFrame);
                Array.Copy(TempBuf, Len_ShortFrame, TempBuf, 0, TempTag - Len_ShortFrame);
                TempTag -= Len_ShortFrame;

                try
                {
                    SaveFile.Lock_4.EnterWriteLock();
                    SaveFile.DataQueue_SC4.Enqueue(buf_ShortFrame);
                    SaveFile.Lock_4.ExitWriteLock();

                    Array.Copy(buf_ShortFrame, 4, buf_ShortFrame_OC, 0, Len_OC);

                    dataGridView_OC1.Invoke(deleupdatedatagrid, buf_ShortFrame_OC);

                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                    Trace.WriteLine("异常来自DealWithShortFrame");
                    break;
                }
            }
        }

        private void RecvAllUSB()
        {
            CyUSBDevice MyDevice01 = USB.MyDeviceList[Data.SCid];
            CyUSBDevice MyDevice02 = USB.MyDeviceList[Data.OCid];
            bool MyDevice01_Enable = false;
            bool MyDevice02_Enable = false;

            int RecvSC_counts = 0;

            Trace.WriteLine("RecvAllUSB start!!!!");

            if (MyDevice01 != null)
            {
                MyDevice01_Enable = true;

                //USB.SendCMD(Data.SCid, 0xE7, 0x04);
                //Thread.Sleep(100);
                //USB.SendCMD(Data.SCid, 0xE8, 0x04);
                //MyLog.Info("0xE8");
                //USB.SendCMD(Data.SCid, 0xE9, 0x04);
                //MyLog.Info("0xE9");
                //USB.SendCMD(Data.SCid, 0xEA, 0x04);
                //MyLog.Info("0xEA");
                new Thread(() => { DealWithTBFrame_TB1(); }).Start();
                new Thread(() => { DealWithTBFrame_TB2(); }).Start();
                new Thread(() => { DealWithTBFrame_TBSC1(); }).Start();
                new Thread(() => { DealWithTBFrame_TBSC2(); }).Start();
                new Thread(() => { DealWithFF08Frame_1D0E(); }).Start();
                new Thread(() => { DealWithFF08Frame_1D0F(); }).Start();
            }
            else
            {
                MyDevice01_Enable = false;
            }

            if (MyDevice02 != null)
            {
                MyDevice02_Enable = true;
            }
            else
            {
                MyDevice02_Enable = false;
            }

            while (RecvTag)
            {
                RecvSC_counts++;
                if (MyDevice01_Enable)
                {
                    if (MyDevice01.BulkInEndPt != null)
                    {
                        byte[] buf = new byte[4096];
                        int buflen = 4096;
                        lock (USB.MyDeviceList[Data.SCid])
                        {
                            MyDevice01.BulkInEndPt.XferData(ref buf, ref buflen);
                        }
                        if (buflen > 0)
                        {
                            Trace.WriteLine("数传422机箱 收到数据包长度为：" + buflen.ToString());
                            Array.Copy(buf, 0, TempStoreBuf, TempStoreBufTag, buflen);
                            TempStoreBufTag += buflen;

                            byte[] Svbuf = new byte[buflen];
                            Array.Copy(buf, Svbuf, buflen);
                            SaveFile.Lock_1.EnterWriteLock();
                            SaveFile.DataQueue_SC1.Enqueue(Svbuf);
                            SaveFile.Lock_1.ExitWriteLock();

                            while (TempStoreBufTag >= 4096)
                            {
                                if (TempStoreBuf[0] == 0xff && TempStoreBuf[1] == 0x0)
                                {
                                    DealWithShortFrame(ref TempStoreBuf, ref TempStoreBufTag);
                                }
                                else if (TempStoreBuf[0] == 0xff && (0x0 < TempStoreBuf[1]) && (TempStoreBuf[1] < 0x11))
                                {
                                    DealWithLongFrame(ref TempStoreBuf, ref TempStoreBufTag);
                                    if (TempStoreBufTag > 32)
                                        DealWithShortFrame(ref TempStoreBuf, ref TempStoreBufTag);
                                }
                                else
                                {
                                    MyLog.Error("数传422机箱 收到异常帧！");
                                    Trace.WriteLine("收到异常帧" + TempStoreBufTag.ToString());
                                    Array.Clear(TempStoreBuf, 0, TempStoreBufTag);
                                    TempStoreBufTag = 0;
                                }
                            }
                        }
                        else if (buflen == 0)
                        {
                            //     Trace.WriteLine("数传422机箱 收到0包-----0000000000");
                        }
                        else
                        {
                            Trace.WriteLine("数传422机箱 收到buflen <0");
                        }
                    }
                }

                if (MyDevice02_Enable && RecvSC_counts >= 5)
                {
                    RecvSC_counts = 0;
                    if (MyDevice02.BulkInEndPt != null)
                    {
                        byte[] buf = new byte[4096];
                        int buflen = 4096;

                        lock (USB.MyDeviceList[Data.OCid])
                        {
                            MyDevice02.BulkInEndPt.XferData(ref buf, ref buflen);
                        }

                        if (buflen > 0)
                        {
                            //Trace.WriteLine("OC机箱 收到数据包长度为：" + buflen.ToString());

                            SaveFile.Lock_2.EnterWriteLock();
                            SaveFile.DataQueue_SC2.Enqueue(buf);
                            SaveFile.Lock_2.ExitWriteLock();

                            Array.Copy(buf, 0, TempStoreBuf_OC, TempStoreBufTag_OC, buflen);
                            TempStoreBufTag_OC += buflen;

                            while (TempStoreBufTag_OC >= 772)
                            {
                                if (TempStoreBuf_OC[0] == 0xff && TempStoreBuf_OC[1] == 0x0)
                                {
                                    Trace.WriteLine("OC机箱--------收到有效帧");
                                    DealWithOCFrame(ref TempStoreBuf_OC, ref TempStoreBufTag_OC);
                                }
                                else if (TempStoreBuf_OC[0] == 0xff && TempStoreBuf_OC[1] == 0xff)
                                {
                                    //         Trace.WriteLine("OC机箱--------收到空闲帧");
                                    Array.Copy(TempStoreBuf_OC, 772, TempStoreBuf_OC, 0, TempStoreBufTag_OC - 772);
                                    TempStoreBufTag_OC -= 772;
                                }
                                else
                                {
                                    Trace.WriteLine("OC机箱--------收到异常帧" + TempStoreBufTag_OC.ToString());
                                    Array.Clear(TempStoreBuf_OC, 0, TempStoreBufTag_OC);
                                    TempStoreBufTag_OC = 0;
                                }
                            }
                        }
                        else if (buflen == 0)
                        {
                            //     Trace.WriteLine("OC机箱 收到0包-----0000000000");
                        }
                        else
                        {
                            Trace.WriteLine("OC机箱 收到buflen <0");
                        }
                    }
                }
            }
        }



        int ThisCount = 0;
        int LastCount = 0;

        void DealWithLongFrame(ref byte[] TempBuf, ref int TempTag)
        {
            ThisCount = TempStoreBuf[2] * 256 + TempStoreBuf[3];
            if (LastCount != 0 && ThisCount != 0 && (ThisCount - LastCount != 1))
            {
                MyLog.Error("出现漏帧情况！！");
                Trace.WriteLine("出现漏帧情况:" + LastCount.ToString("x4") + "--" + ThisCount.ToString("x4"));
            }
            LastCount = ThisCount;

            byte[] buf_LongFrame = new byte[4096];
            Array.Copy(TempStoreBuf, 0, buf_LongFrame, 0, 4096);

            SaveFile.Lock_5.EnterWriteLock();
            SaveFile.DataQueue_SC5.Enqueue(buf_LongFrame);
            SaveFile.Lock_5.ExitWriteLock();

            Array.Copy(TempStoreBuf, 4096, TempStoreBuf, 0, TempStoreBufTag - 4096);
            TempStoreBufTag -= 4096;

            if (buf_LongFrame[0] == 0xff && buf_LongFrame[1] == 0x03)
            {
                //数传1通道
                byte[] bufsav = new byte[4092];
                Array.Copy(buf_LongFrame, 4, bufsav, 0, 4092);

                if (Data.SaveSC1)
                {
                    SaveFile.Lock_7.EnterWriteLock();
                    SaveFile.DataQueue_SC7.Enqueue(bufsav);
                    SaveFile.Lock_7.ExitWriteLock();
                }

                Lock_TBSC1.EnterWriteLock();
                for (int i = 0; i < 4092; i++) DataQueue_TBSC1.Enqueue(bufsav[i]);
                Lock_TBSC1.ExitWriteLock();


            }
            if (buf_LongFrame[0] == 0xff && buf_LongFrame[1] == 0x04)
            {
                //数传2通道
                byte[] bufsav = new byte[4092];
                Array.Copy(buf_LongFrame, 4, bufsav, 0, 4092);
                if (Data.SaveSC2)
                {
                    SaveFile.Lock_8.EnterWriteLock();
                    SaveFile.DataQueue_SC8.Enqueue(bufsav);
                    SaveFile.Lock_8.ExitWriteLock();
                }
                Lock_TBSC2.EnterWriteLock();
                for (int i = 0; i < 4092; i++) DataQueue_TBSC2.Enqueue(bufsav[i]);
                Lock_TBSC2.ExitWriteLock();
            }
            if (buf_LongFrame[0] == 0xff && buf_LongFrame[1] == 0x08)
            {
                //短帧通道
                byte[] bufsav = new byte[4092];
                Array.Copy(buf_LongFrame, 4, bufsav, 0, 4092);
                SaveFile.Lock_10.EnterWriteLock();
                SaveFile.DataQueue_SC10.Enqueue(bufsav);
                SaveFile.Lock_10.ExitWriteLock();

                for (int i = 0; i < 31; i++)
                {
                    if (bufsav[i * 132 + 0] == 0x1D && bufsav[i * 132 + 1] == 0x0C)
                    {
                        int num = bufsav[i * 132 + 2] * 256 + bufsav[i * 132 + 3];//有效位
                        Lock_1D0E.EnterWriteLock();
                        for (int j = 0; j < num; j++)
                        {
                            DataQueue_1D0E.Enqueue(bufsav[i * 132 + 4 + j]);
                        }
                        Lock_1D0E.ExitWriteLock();
                    }
                    else if (bufsav[i * 132 + 0] == 0x1D && bufsav[i * 132 + 1] == 0x0D)
                    {
                        int num = bufsav[i * 132 + 2] * 256 + bufsav[i * 132 + 3];//有效位
                        Lock_1D0F.EnterWriteLock();
                        for (int j = 0; j < num; j++)
                        {
                            DataQueue_1D0F.Enqueue(bufsav[i * 132 + 4 + j]);
                        }
                        Lock_1D0F.ExitWriteLock();
                    }
                    else if (bufsav[i * 132 + 0] == 0x1D && bufsav[i * 132 + 1] == 0x0F)
                    {
                        Trace.WriteLine("短帧空闲序列！");
                    }

                    else
                    {
                        Trace.WriteLine("FF08通道出错!");
                    }
                }


            }

        }


        void DealWithShortFrame(ref byte[] TempBuf, ref int TempTag)
        {
            while (TempBuf[0] == 0xff && TempBuf[1] == 0x0 && TempTag >= 32)
            {
                ThisCount = TempBuf[2] * 256 + TempBuf[3];
                if (LastCount == 0 | ThisCount == 0)
                    LastCount = ThisCount;
                else
                {
                    if (ThisCount - LastCount != 1)
                        MyLog.Error("出现漏帧情况！！");
                    else
                        LastCount = ThisCount;
                }

                byte[] buf_ShortFrame = new byte[32];

                //Trace.WriteLine("---------------------收到短帧--------------");
                Array.Copy(TempBuf, 0, buf_ShortFrame, 0, 32);
                Array.Copy(TempBuf, 32, TempBuf, 0, TempTag - 32);
                TempTag -= 32;

                try
                {
                    SaveFile.Lock_3.EnterWriteLock();
                    SaveFile.DataQueue_SC3.Enqueue(buf_ShortFrame);
                    SaveFile.Lock_3.ExitWriteLock();


                    byte[] For422Deal = new byte[24];
                    Array.Copy(buf_ShortFrame, 8, For422Deal, 0, 24);

                    Lock_DataQueue_422.EnterWriteLock();
                    DataQueue_422.Enqueue(For422Deal);
                    Lock_DataQueue_422.ExitWriteLock();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                    Trace.WriteLine("异常来自DealWithShortFrame");
                    break;
                }
            }

        }


        void DealWithTBFrame_TB1()
        {
            DeleUpdateListView deleupdatelistview1 = new DeleUpdateListView(UpdateListViewTB1);
            while (RecvTag)
            {
                if (DataQueue_TB1.Count() >= 1024)
                {
                    try
                    {
                        Lock_TB1.EnterReadLock();
                        byte[] ToQueueCADU = new byte[1024];
                        for (int i = 0; i < 1024; i++) ToQueueCADU[i] = DataQueue_TB1.Dequeue();
                        Lock_TB1.ExitReadLock();
                        Data.DataQueue1.Enqueue(ToQueueCADU);                           //将收到的通道1同步422数据推入DataQueue1                
                        if (Data.DataQueue1.Count > 100) Data.DataQueue1.Clear();       //防止不处理DataQueue1，导致数据越来越多            

                        listView2.Invoke(deleupdatelistview1, ToQueueCADU);

                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message);
                    }
                }
                else
                {
                    Thread.Sleep(200);
                }
            }
        }

        void DealWithTBFrame_TB2()
        {
            DeleUpdateListView deleupdatelistview2 = new DeleUpdateListView(UpdateListViewTB2);

            while (RecvTag)
            {
                if (DataQueue_TB2.Count() >= 1024)
                {
                    try
                    {
                        Lock_TB2.EnterReadLock();
                        byte[] ToQueueCADU = new byte[1024];
                        for (int i = 0; i < 1024; i++) ToQueueCADU[i] = DataQueue_TB2.Dequeue();
                        Lock_TB2.ExitReadLock();

                        Data.DataQueue2.Enqueue(ToQueueCADU);                                       //将收到的通道2同步422数据推入DataQueue2
                        if (Data.DataQueue2.Count > 100) Data.DataQueue2.Clear();              //防止不处理DataQueue2，导致数据越来越多
                        listView2.Invoke(deleupdatelistview2, ToQueueCADU);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message);
                    }
                }
                else
                {
                    Thread.Sleep(200);
                }
            }
        }

        private void DealWithFF08Frame_1D0E()
        {
            DeleUpdateListView deleupdatelistview1 = new DeleUpdateListView(UpdateListViewTB1);

            while (RecvTag)
            {
                if (DataQueue_1D0E.Count() >= 1024)
                {
                    try
                    {
                        Lock_1D0E.EnterReadLock();
                        byte[] ToQueueCADU = new byte[1024];
                        for (int i = 0; i < 1024; i++) ToQueueCADU[i] = DataQueue_1D0E.Dequeue();
                        Lock_1D0E.ExitReadLock();

                        SaveFile.Lock_6.EnterWriteLock();
                        SaveFile.DataQueue_SC6.Enqueue(ToQueueCADU);
                        SaveFile.Lock_6.ExitWriteLock();

                        Data.DataQueue1.Enqueue(ToQueueCADU);                                       //将收到的通道2同步422数据推入DataQueue2
                        if (Data.DataQueue1.Count > 100) Data.DataQueue1.Clear();              //防止不处理DataQueue2，导致数据越来越多
                        listView2.Invoke(deleupdatelistview1, ToQueueCADU);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message);
                    }
                }
                else
                {
                    Thread.Sleep(200);
                }

            }
        }

        /// <summary>
        /// 处理收到的FF08带来的短实时帧
        /// </summary>
        private void DealWithFF08Frame_1D0F()
        {
            DeleUpdateListView deleupdatelistview2 = new DeleUpdateListView(UpdateListViewTB2);

            while (RecvTag)
            {
                if (DataQueue_1D0F.Count() >= 1024)
                {
                    try
                    {
                        Lock_1D0F.EnterReadLock();
                        byte[] ToQueueCADU = new byte[1024];
                        for (int i = 0; i < 1024; i++) ToQueueCADU[i] = DataQueue_1D0F.Dequeue();
                        Lock_1D0F.ExitReadLock();

                        SaveFile.Lock_9.EnterWriteLock();
                        SaveFile.DataQueue_SC9.Enqueue(ToQueueCADU);
                        SaveFile.Lock_9.ExitWriteLock();

                        Data.DataQueue2.Enqueue(ToQueueCADU);                                       //将收到的通道2同步422数据推入DataQueue2
                        if (Data.DataQueue2.Count > 100) Data.DataQueue2.Clear();              //防止不处理DataQueue2，导致数据越来越多
                        listView2.Invoke(deleupdatelistview2, ToQueueCADU);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message);
                    }
                }
                else
                {
                    Thread.Sleep(200);
                }

            }
        }

        public List<string> VCID_List_1 = new List<string>();
        public List<string> VCID_List_2 = new List<string>();

        void DealWithTBFrame_TBSC1()
        {
            while (RecvTag)
            {
                if (DataQueue_TBSC1.Count() >= 1024)
                {
                    try
                    {
                        Lock_TBSC1.EnterReadLock();
                        byte[] ToQueueCADU = new byte[1024];
                        for (int i = 0; i < 1024; i++) ToQueueCADU[i] = DataQueue_TBSC1.Dequeue();
                        Lock_TBSC1.ExitReadLock();

                        string tbstr = ToQueueCADU[0].ToString("x2") + ToQueueCADU[1].ToString("x2") + ToQueueCADU[2].ToString("x2") + ToQueueCADU[3].ToString("x2");

                        string VersinStr = ((ToQueueCADU[4] & 0xC0) >> 6).ToString("x2");

                        byte b1 = (byte)((ToQueueCADU[4] & 0x3f) << 2);
                        byte b2 = (byte)((ToQueueCADU[5] & 0xC0) >> 6);
                        string SCIDstr = (b1 + b2).ToString("x2");

                        string VCIDstr = (ToQueueCADU[5] & 0x3f).ToString("x2");

                        string VCIDCountStr = ToQueueCADU[6].ToString("x2") + ToQueueCADU[7].ToString("x2") + ToQueueCADU[8].ToString("x2");

                        string ReviewFlagStr = ((ToQueueCADU[9] & 0x80) >> 7).ToString("x2");

                        string ReserveFlagStr = (ToQueueCADU[9] & 0x7f).ToString("x2");

                        byte[] tempRe = new byte[6];
                        Array.Copy(ToQueueCADU, 10, tempRe, 0, 6);
                        string TimeStr = Function.DecodeTime(tempRe);

                        ToDataShow1[0] = tbstr;
                        ToDataShow1[1] = VersinStr;
                        ToDataShow1[2] = SCIDstr;
                        ToDataShow1[3] = VCIDstr;
                        ToDataShow1[4] = VCIDCountStr;
                        ToDataShow1[5] = ReviewFlagStr;
                        ToDataShow1[6] = ReserveFlagStr;
                        ToDataShow1[7] = TimeStr;

                        Data.SCRecvCounts1 += 1;

                        bool AlreadInList = false;
                        foreach (var item in VcidList1)
                        {
                            if (item.Name == VCIDstr)
                            {
                                AlreadInList = true;
                                break;
                            }
                        }
                        if (AlreadInList == false)
                        {
                            VCID_STRUCT temp = new VCID_STRUCT();
                            temp.Name = VCIDstr;
                            temp.RecvNums = 1;
                            VcidList1.Add(temp);
                            MyLog.Info("New VCID detected!");
                            //此处增加datagridview_sc1的新增行
                            DataRow dr = dtSCVcid1.NewRow();
                            dr["VCID"] = temp.Name;
                            dr["数量"] = temp.RecvNums;
                            dtSCVcid1.Rows.Add(dr);
                            //timer1中增加显示
                        }
                        else
                        {
                            for (int i = 0; i < VcidList1.Count; i++)
                            {
                                if (VcidList1[i].Name == VCIDstr)
                                {
                                    VCID_STRUCT temp = new VCID_STRUCT();
                                    temp.Name = VcidList1[i].Name;
                                    temp.RecvNums = VcidList1[i].RecvNums + 1;
                                    VcidList1.Remove(VcidList1[i]);
                                    VcidList1.Add(temp);
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message);
                    }
                }
                else
                {
                    Thread.Sleep(200);
                }
            }
        }

        void DealWithTBFrame_TBSC2()
        {
            while (RecvTag)
            {
                if (DataQueue_TBSC2.Count() >= 1024)
                {
                    try
                    {
                        Lock_TBSC2.EnterReadLock();
                        byte[] ToQueueCADU = new byte[1024];
                        for (int i = 0; i < 1024; i++) ToQueueCADU[i] = DataQueue_TBSC2.Dequeue();
                        Lock_TBSC2.ExitReadLock();

                        string tbstr = ToQueueCADU[0].ToString("x2") + ToQueueCADU[1].ToString("x2") + ToQueueCADU[2].ToString("x2") + ToQueueCADU[3].ToString("x2");

                        string VersinStr = ((ToQueueCADU[4] & 0xC0) >> 6).ToString("x2");

                        byte b1 = (byte)((ToQueueCADU[4] & 0x3f) << 2);
                        byte b2 = (byte)((ToQueueCADU[5] & 0xC0) >> 6);
                        string SCIDstr = (b1 + b2).ToString("x2");

                        string VCIDstr = (ToQueueCADU[5] & 0x3f).ToString("x2");

                        string VCIDCountStr = ToQueueCADU[6].ToString("x2") + ToQueueCADU[7].ToString("x2") + ToQueueCADU[8].ToString("x2");

                        string ReviewFlagStr = ((ToQueueCADU[9] & 0x80) >> 7).ToString("x2");

                        string ReserveFlagStr = (ToQueueCADU[9] & 0x7f).ToString("x2");

                        byte[] tempRe = new byte[6];
                        Array.Copy(ToQueueCADU, 10, tempRe, 0, 6);
                        string TimeStr = Function.DecodeTime(tempRe);

                        ToDataShow2[0] = tbstr;
                        ToDataShow2[1] = VersinStr;
                        ToDataShow2[2] = SCIDstr;
                        ToDataShow2[3] = VCIDstr;
                        ToDataShow2[4] = VCIDCountStr;
                        ToDataShow2[5] = ReviewFlagStr;
                        ToDataShow2[6] = ReserveFlagStr;
                        ToDataShow2[7] = TimeStr;

                        Data.SCRecvCounts2 += 1;

                        bool AlreadInList = false;
                        foreach (var item in VcidList2)
                        {
                            if (item.Name == VCIDstr)
                            {
                                AlreadInList = true;
                                break;
                            }
                        }
                        if (AlreadInList == false)
                        {
                            VCID_STRUCT temp = new VCID_STRUCT();
                            temp.Name = VCIDstr;
                            temp.RecvNums = 1;
                            VcidList2.Add(temp);
                            MyLog.Info("New VCID detected!");
                            //此处增加datagridview_sc1的新增行
                            DataRow dr = dtSCVcid2.NewRow();
                            dr["VCID"] = temp.Name;
                            dr["数量"] = temp.RecvNums;
                            dtSCVcid2.Rows.Add(dr);
                            //timer1中增加显示
                        }
                        else
                        {
                            for (int i = 0; i < VcidList2.Count; i++)
                            {
                                if (VcidList2[i].Name == VCIDstr)
                                {
                                    VCID_STRUCT temp = new VCID_STRUCT();
                                    temp.Name = VcidList2[i].Name;
                                    temp.RecvNums = VcidList2[i].RecvNums + 1;
                                    VcidList2.Remove(VcidList2[i]);
                                    VcidList2.Add(temp);
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message);
                    }
                }
                else
                {
                    Thread.Sleep(200);
                }
            }
        }
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            RS422Tag = false;
            for (int t = 0; t < 12; t++) DealRs422Tag[t] = false;
            RecvTag = false;
            Thread.Sleep(300);

            if (USB.usbDevices != null)
            {
                USB.usbDevices.DeviceRemoved -= UsbDevices_DeviceRemoved;
                USB.usbDevices.DeviceAttached -= UsbDevices_DeviceAttached;
                USB.usbDevices.Dispose();
            }

            if (FileThread != null)
                FileThread.FileClose();

            Thread.Sleep(200);

            this.Dispose();
        }



        private void timer1_Tick(object sender, EventArgs e)
        {
            toolStripStatusLabel3.Text = "剩余空间" + DiskInfo.GetFreeSpace(Path[0].ToString()) + "MB";
            toolStripStatusLabel4.Text = "当前时间：" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + " ";

            TimeSpan ts = DateTime.Now.Subtract(startDT);

            toolStripStatusLabel5.Text = "已运行：" + ts.Days.ToString() + "天" +
                ts.Hours.ToString() + "时" +
                ts.Minutes.ToString() + "分" +
                ts.Seconds.ToString() + "秒";


            #region 显示下行数传收到总包数

            dataGridView3.Rows[0].Cells[4].Value = Data.SCRecvCounts1;
            dataGridView3.Rows[1].Cells[4].Value = Data.SCRecvCounts2;
            #endregion

            #region 显示下行数传解析结果
            // ToDataShow[0]是同步码
            dtSCRecv.Rows[0]["版本号"] = ToDataShow1[1];
            dtSCRecv.Rows[0]["航天器标识"] = ToDataShow1[2];
            dtSCRecv.Rows[0]["虚拟信道标识"] = ToDataShow1[3];
            dtSCRecv.Rows[0]["虚拟信道帧计数"] = ToDataShow1[4];
            dtSCRecv.Rows[0]["回放"] = ToDataShow1[5];
            dtSCRecv.Rows[0]["保留"] = ToDataShow1[6];
            dtSCRecv.Rows[0]["插入域"] = ToDataShow1[7];

            dtSCRecv.Rows[1]["版本号"] = ToDataShow2[1];
            dtSCRecv.Rows[1]["航天器标识"] = ToDataShow2[2];
            dtSCRecv.Rows[1]["虚拟信道标识"] = ToDataShow2[3];
            dtSCRecv.Rows[1]["虚拟信道帧计数"] = ToDataShow2[4];
            dtSCRecv.Rows[1]["回放"] = ToDataShow2[5];
            dtSCRecv.Rows[1]["保留"] = ToDataShow2[6];
            dtSCRecv.Rows[1]["插入域"] = ToDataShow2[7];
            #endregion

            #region 显示数传VCID解析结果
            if (VcidList1.Count > 0)
            {
                for (int i = 0; i < VcidList1.Count; i++)
                {
                    foreach (DataRow dr in dtSCVcid1.Rows)
                    {
                        if ((string)dr["VCID"] == VcidList1[i].Name)
                        {
                            dr["数量"] = VcidList1[i].RecvNums;
                        }
                    }
                }
            }

            if (VcidList2.Count > 0)
            {
                for (int i = 0; i < VcidList2.Count; i++)
                {
                    foreach (DataRow dr in dtSCVcid2.Rows)
                    {
                        if ((string)dr["VCID"] == VcidList2[i].Name)
                        {
                            dr["数量"] = VcidList2[i].RecvNums;
                        }
                    }
                }
            }
            #endregion

        }

        /// <summary>
        /// 十六进制String转化为BYTE数组
        /// </summary>
        /// <param name="hexString">参数：输入的十六进制String</param>
        /// <returns>BYTE数组</returns>
        private static byte[] StrToHexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "").Replace("\r", "").Replace("\n", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";

            byte[] returnBytes = new byte[hexString.Length / 2];

            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;

        }

        public string file2usb_send = null;
        private void Btn_LoadFile2USB_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = Path;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                openFileDialog1.InitialDirectory = Program.GetStartupPath();
                try
                {
                    string temp = System.IO.File.ReadAllText(openFileDialog1.FileName);
                    this.textBox1.Text = temp.Replace(" ", "");
                }
                catch
                {
                    // MyLog.Error("加载发送码本失败！");             
                    //MessageBox.Show("运行日志打开失败！");
                }
            }
            Console.WriteLine(file2usb);
        }

        public int TagContinueTB_1 = 10;
        public int TagContinueTB_2 = 10;
        public int TagContinueTB_3 = 10;
        private void Btn_SendFile2USB_Click(object sender, EventArgs e)
        {
            //Byte81H = (byte)(Byte81H | 0x01);
            //USB.SendCMD(Data.SCidx81, Byte81H);
            //USB.SendCMD(Data.SCidx81, 0x05);
            if (radioButton1.Checked)
            {
                string Str_Content = this.textBox1.Text.Replace(" ", "");

                int AddAlen = 16 - (Str_Content.Length % 16);
                Str_Content = Str_Content.PadRight(AddAlen, 'A');

                int lenth = (Str_Content.Length) / 2;
                byte[] temp = StrToHexByte("1D0C" + lenth.ToString("x4") + Str_Content + "C0DEC0DE");
                USB.SendDataByInt(Data.SCid, temp);
            }
            else if (radioButton2.Checked)
            {
                this.timer_send1.Enabled = true;
                this.timer_send1.Interval = int.Parse(comboBox5.Text);
                this.TagContinueTB_1 = int.Parse(comboBox6.Text);
            }
            else
            {
                MyLog.Error("选择单次发送或周期发送！");
            }
        }

        private void timer_send1_Tick(object sender, EventArgs e)
        {
            string Str_Content = this.textBox1.Text.Replace(" ", "");

            int AddAlen = 16 - (Str_Content.Length % 16);
            Str_Content = Str_Content.PadRight(AddAlen, 'A');

            int lenth = (Str_Content.Length) / 2;
            byte[] temp = StrToHexByte("1D0C" + lenth.ToString("x4") + Str_Content + "C0DEC0DE");
            USB.SendDataByInt(Data.SCid, temp);
            TagContinueTB_1 -= 1;
            if (TagContinueTB_1 < 1)
            {
                this.timer_send1.Enabled = false;
            }
        }


        private void Btn_SendFile2USB_2_Click(object sender, EventArgs e)
        {
            if (radioButton3.Checked)
            {
                string Str_Content = this.textBox2.Text.Replace(" ", "");
                int AddAlen = 16 - (Str_Content.Length % 16);
                Str_Content = Str_Content.PadRight(AddAlen, 'A');

                int lenth = (Str_Content.Length) / 2;
                byte[] temp = StrToHexByte("1D0D" + lenth.ToString("x4") + Str_Content + "C0DEC0DE");
                USB.SendDataByInt(Data.SCid, temp);
            }
            else if (radioButton4.Checked)
            {
                this.timer_send2.Enabled = true;
                this.timer_send2.Interval = int.Parse(comboBox2.Text);
                this.TagContinueTB_2 = int.Parse(comboBox11.Text);
            }
            else
            {
                MyLog.Error("选择单次发送或周期发送！");
            }
        }

        private void timer_send2_Tick(object sender, EventArgs e)
        {
            string Str_Content = this.textBox2.Text.Replace(" ", "");

            int AddAlen = 16 - (Str_Content.Length % 16);
            Str_Content = Str_Content.PadRight(AddAlen, 'A');

            int lenth = (Str_Content.Length) / 2;
            byte[] temp = StrToHexByte("1D0D" + lenth.ToString("x4") + Str_Content + "C0DEC0DE");
            USB.SendDataByInt(Data.SCid, temp);
            TagContinueTB_2 -= 1;
            if (TagContinueTB_2 < 1)
            {
                this.timer_send2.Enabled = false;
            }
        }


        private void Btn_LoadFile2USB_2_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = Path;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                openFileDialog1.InitialDirectory = Program.GetStartupPath();
                try
                {
                    string temp = System.IO.File.ReadAllText(openFileDialog1.FileName);
                    this.textBox2.Text = temp.Replace(" ", "");
                }
                catch
                {
                    // MyLog.Error("加载发送码本失败！");             
                    //MessageBox.Show("运行日志打开失败！");
                }
            }
            Console.WriteLine(file2usb);
        }


        private void button_Load422_1_Click(object sender, EventArgs e)
        {
            // System.Diagnostics.Debug.WriteLine("Start button_Load422_1_Click!\n");
            openFileDialog1.InitialDirectory = Path;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                openFileDialog1.InitialDirectory = Program.GetStartupPath();
                string temp = System.IO.File.ReadAllText(openFileDialog1.FileName);
                this.textBox_422_1.Text = temp.Replace(" ", "");
            }
        }

        private void button_Load422_2_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = Path;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                openFileDialog1.InitialDirectory = Program.GetStartupPath();
                string temp = System.IO.File.ReadAllText(openFileDialog1.FileName);
                this.textBox_422_2.Text = temp.Replace(" ", "");
            }
        }

        private void button_Load422_3_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = Path;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                openFileDialog1.InitialDirectory = Program.GetStartupPath();
                string temp = System.IO.File.ReadAllText(openFileDialog1.FileName);
                this.textBox_422_3.Text = temp.Replace(" ", "");
            }
        }

        private void button_Load422_4_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = Path;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                openFileDialog1.InitialDirectory = Program.GetStartupPath();
                string temp = System.IO.File.ReadAllText(openFileDialog1.FileName);
                this.textBox_422_4.Text = temp.Replace(" ", "");
            }
        }

        private void button_Load422_5_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = Path;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                openFileDialog1.InitialDirectory = Program.GetStartupPath();
                string temp = System.IO.File.ReadAllText(openFileDialog1.FileName);
                this.textBox_422_5.Text = temp.Replace(" ", "");
            }
        }

        private void button_Load422_6_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = Path;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                openFileDialog1.InitialDirectory = Program.GetStartupPath();
                string temp = System.IO.File.ReadAllText(openFileDialog1.FileName);
                this.textBox_422_6.Text = temp.Replace(" ", "");
            }
        }

        private void button_Load422_7_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = Path;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                openFileDialog1.InitialDirectory = Program.GetStartupPath();
                string temp = System.IO.File.ReadAllText(openFileDialog1.FileName);
                this.textBox_422_7.Text = temp.Replace(" ", "");
            }
        }

        private void button_Load422_8_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = Path;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                openFileDialog1.InitialDirectory = Program.GetStartupPath();
                string temp = System.IO.File.ReadAllText(openFileDialog1.FileName);
                this.textBox_422_8.Text = temp.Replace(" ", "");
            }
        }

        private void button_Load422_9_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = Path;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                openFileDialog1.InitialDirectory = Program.GetStartupPath();
                string temp = System.IO.File.ReadAllText(openFileDialog1.FileName);
                this.textBox_422_9.Text = temp.Replace(" ", "");
            }
        }

        private void button_Load422_10_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = Path;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                openFileDialog1.InitialDirectory = Program.GetStartupPath();
                string temp = System.IO.File.ReadAllText(openFileDialog1.FileName);
                this.textBox_422_10.Text = temp.Replace(" ", "");
            }
        }

        private void button_Load422_11_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = Path;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                openFileDialog1.InitialDirectory = Program.GetStartupPath();
                string temp = System.IO.File.ReadAllText(openFileDialog1.FileName);
                this.textBox_422_11.Text = temp.Replace(" ", "");
            }
        }

        private void button_Load422_12_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = Path;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                openFileDialog1.InitialDirectory = Program.GetStartupPath();
                string temp = System.IO.File.ReadAllText(openFileDialog1.FileName);
                this.textBox_422_12.Text = temp.Replace(" ", "");
            }
        }


        private void button_Send422_11_Click(object sender, EventArgs e)
        {
            Button tempBtn = (Button)sender;
            string btnName = tempBtn.Name;
            int key = 0;
            int.TryParse(btnName.Substring(15), out key); //button_Send422_11去掉button_Send422_剩余为key

            byte addr1 = (byte)(0x91 + key - 1);
            byte addr2 = (byte)(0xB5 + 2 * (key - 1));
            byte addr3 = (byte)(addr2 + 1);

            int temphead = int.Parse("1D00", System.Globalization.NumberStyles.HexNumber);
            string header = (temphead + key - 1).ToString("x4");

            DealWith_btn_Send422_Click(key, myTextBox_422_Sends[key - 1], header, addr1, addr2, addr3);

        }

        private void DealWith_btn_Send422_Click(int chan, TextBox mytextbox, string header, byte addr1, byte addr2, byte addr3)
        {
            string Str_Content = mytextbox.Text.Replace(" ", "");
            int lenth = (Str_Content.Length) / 2;
            byte[] temp = StrToHexByte(header + lenth.ToString("x4") + Str_Content + "C0DEC0DE");
            Register.Byte9xHs[chan - 1] = (byte)(Register.Byte9xHs[chan - 1] | 0x40);
            USB.SendCMD(Data.SCid, addr1, Register.Byte9xHs[chan - 1]);
            Register.Byte9xHs[chan - 1] = (byte)(Register.Byte9xHs[chan - 1] & 0xBF);
            USB.SendCMD(Data.SCid, addr1, Register.Byte9xHs[chan - 1]);
            USB.SendDataByInt(Data.SCid, temp);
            USB.SendCMD(Data.SCid, addr2, (byte)(JYB_Times[chan - 1] & 0x7f));
            USB.SendCMD(Data.SCid, addr3, (byte)(((JYB_Times[chan - 1] & 0x3f80) >> 7) & 0x7f));


            if (JYB_Str != null)
            {
                int JYB_Str_int = int.Parse(JYB_Str, System.Globalization.NumberStyles.HexNumber);
                USB.SendCMD(Data.SCid, 0xCD, (byte)(JYB_Str_int & 0x7f));//校验包指令前7位
                USB.SendCMD(Data.SCid, 0xCE, (byte)((JYB_Str_int & 0x80) >> 7));//校验包指令第8位
            }

        }


        private void button2_Click(object sender, EventArgs e)
        {
            //当处于周期发送时，复位强制将发送次数设置为0
            TagContinueTB_1 = 0;
            TagContinueTB_2 = 0;
            TagContinueTB_3 = 0;

            MyLog.Info("复位设备");
            checkBox5.Checked = false;
            checkBox6.Checked = false;

            CyUSBDevice MyDevice01 = USB.MyDeviceList[Data.SCid];
            if (MyDevice01 != null)
            {
                CyControlEndPoint CtrlEndPt = null;
                CtrlEndPt = MyDevice01.ControlEndPt;
                if (CtrlEndPt != null)
                {
                    //复位
                    USB.SendCMD(Data.SCid, 0x80, 0x01);
                    USB.SendCMD(Data.SCid, 0x80, 0x00);
                    USB.SendCMD(Data.SCid, 0x81, 0x0);
                    USB.SendCMD(Data.SCid, 0x88, 0x0);
                    USB.SendCMD(Data.SCid, 0x91, 0x00);
                    USB.SendCMD(Data.SCid, 0x92, 0x00);
                    USB.SendCMD(Data.SCid, 0x93, 0x00);
                    USB.SendCMD(Data.SCid, 0x94, 0x00);
                    USB.SendCMD(Data.SCid, 0x95, 0x00);
                    USB.SendCMD(Data.SCid, 0x96, 0x00);
                    USB.SendCMD(Data.SCid, 0x97, 0x00);
                    USB.SendCMD(Data.SCid, 0x98, 0x00);
                    USB.SendCMD(Data.SCid, 0x99, 0x00);
                    USB.SendCMD(Data.SCid, 0x9A, 0x00);
                    USB.SendCMD(Data.SCid, 0x9B, 0x00);
                    USB.SendCMD(Data.SCid, 0x9C, 0x00);

                    MyDevice01.Reset();
                }
            }

            CyUSBDevice MyDevice02 = USB.MyDeviceList[Data.OCid];
            if (MyDevice02 != null)
            {
                CyControlEndPoint CtrlEndPt = null;
                CtrlEndPt = MyDevice02.ControlEndPt;
                if (CtrlEndPt != null)
                {
                    //复位
                    USB.SendCMD(Data.OCid, 0x80, 0x01);
                    USB.SendCMD(Data.OCid, 0x80, 0x00);

                    MyDevice02.Reset();
                }
            }


            CyUSBDevice MyDevice03 = USB.MyDeviceList[Data.DARid];
            if (MyDevice03 != null)
            {
                CyControlEndPoint CtrlEndPt = null;
                CtrlEndPt = MyDevice03.ControlEndPt;
                if (CtrlEndPt != null)
                {
                    //复位
                    USB.SendCMD(Data.DARid, 0x80, 0x01);
                    USB.SendCMD(Data.DARid, 0x80, 0x00);

                    MyDevice03.Reset();
                }
            }

            this.button3.Text = "一键开始";
            this.button3.BackColor = Color.Aquamarine;

            RecvTag = false;

            Thread.Sleep(200);
            if (FileThread != null)
                FileThread.FileClose();

            Thread.Sleep(200);

            if (RS422Thread != null)
            {
                RS422Tag = false;
                if (RS422Thread.IsAlive)
                    RS422Thread.Abort();
                RS422Thread = null;
            }

            for (int i = 0; i < 8192; i++) TempStoreBuf[i] = 0;
            TempStoreBufTag = 0;

            for (int i = 0; i < 8192; i++) TempStoreBuf_OC[i] = 0;
            TempStoreBufTag_OC = 0;

        }


        private void textBox3_Click(object sender, EventArgs e)
        {
            ChannelTag = 1;
            if (myFrameProdeceForm != null)
            {
                myFrameProdeceForm.Activate();
            }
            else
            {
                myFrameProdeceForm = new FrameProduceForm(this);
            }
            myFrameProdeceForm.ShowDialog();
        }

        private void textBox2_Click(object sender, EventArgs e)
        {
            ChannelTag = 2;
            if (myFrameProdeceForm != null)
            {
                myFrameProdeceForm.Activate();
            }
            else
            {
                myFrameProdeceForm = new FrameProduceForm(this);
            }
            myFrameProdeceForm.ShowDialog();
        }

        private void textBox_422_1_Click(object sender, EventArgs e)
        {
            Rs422_Channel_Name = ((TextBox)sender).Name;
            if (myRs422FrameProduceForm != null)
            {
                myRs422FrameProduceForm.Activate();
            }
            else
            {
                myRs422FrameProduceForm = new RS422FrameProduceForm(this);
            }
            myRs422FrameProduceForm.ShowDialog();
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (this.radioButton2.Checked)
            {
                this.comboBox5.Enabled = true;
                this.comboBox6.Enabled = true;
            }
            else
            {
                this.comboBox5.Enabled = false;
                this.comboBox6.Enabled = false;
            }
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (this.radioButton4.Checked)
            {
                this.comboBox2.Enabled = true;
                this.comboBox11.Enabled = true;
            }
            else
            {
                this.comboBox2.Enabled = false;
                this.comboBox11.Enabled = false;
            }
        }


        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox4.SelectedIndex == 0)
            {
                MyLog.Info("通道1切换到--突发模式");
                Register.Byte81H = (byte)(Register.Byte81H | 0x02);
                USB.SendCMD(Data.SCid, 0x81, Register.Byte81H);
                groupBox3.Enabled = true;
            }
            else if (comboBox4.SelectedIndex == 1)
            {
                MyLog.Info("通道1切换到--连续模式");
                Register.Byte81H = (byte)(Register.Byte81H & 0xFD);
                USB.SendCMD(Data.SCid, 0x81, Register.Byte81H);
                groupBox3.Enabled = false;
            }
            else
            {
                groupBox3.Enabled = false;
                MyLog.Error("选择有效的工作模式");
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 0)
            {
                MyLog.Info("通道2切换到--突发模式");
                Register.Byte88H = (byte)(Register.Byte88H | 0x02);
                USB.SendCMD(Data.SCid, 0x88, Register.Byte88H);
                groupBox5.Enabled = true;
            }
            else if (comboBox1.SelectedIndex == 1)
            {
                MyLog.Info("通道2切换到--连续模式");
                Register.Byte88H = (byte)(Register.Byte88H & 0xFD);
                USB.SendCMD(Data.SCid, 0x88, Register.Byte88H);
                groupBox5.Enabled = false;
            }
            else
            {
                groupBox5.Enabled = false;
                MyLog.Error("选择有效的工作模式");
            }
        }

        /// <summary>
        /// 当点击开启监测时会调用次函数
        /// </summary>
        /// <param name="ctr">操作符,表示启动或关闭</param>
        /// <param name="chan">通道号</param>
        /// <param name="mytextbox">对应的textbox</param>
        /// <param name="addr1">串口设置91-9C</param>
        /// <param name="addr2">串口设置9D-B4</param>
        /// <param name="addr3">串口设置9D-B4</param>
        private void DealWith_btn_StartRs422_1_Click(string ctr, int chan, TextBox mytextbox, byte addr1, byte addr2, byte addr3)
        {
            if (ctr == "open")
            {
                int temphead = int.Parse(mytextbox.Text, System.Globalization.NumberStyles.HexNumber);
                Register.Byte9xHs[chan - 1] = (byte)((((temphead & 0x03) << 3) | 0x20) | Register.Byte9xHs[chan - 1]);
                USB.SendCMD(Data.SCid, addr1, Register.Byte9xHs[chan - 1]);
                USB.SendCMD(Data.SCid, addr2, (byte)(((temphead & 0x01FC) >> 2) & 0x7f));
                USB.SendCMD(Data.SCid, addr3, (byte)(((temphead & 0xFE00) >> 9) & 0x7f));
                MyLog.Info("通道" + chan.ToString() + "开启监测");
            }
            else if (ctr == "close")
            {
                USB.SendCMD(Data.SCid, addr1, 0x00);
                USB.SendCMD(Data.SCid, addr2, 0x00);
                USB.SendCMD(Data.SCid, addr3, 0x00);
                MyLog.Info("通道" + chan.ToString() + "关闭监测");
            }
            else
            {
                MyLog.Error("Unexpected Error from DealWith_btn_StartRs422_1_Click");
            }
        }


        private void btn_StartRs422_1_Click(object sender, EventArgs e)
        {
            if (button3.Enabled)
            {
                Button tempBtn = (Button)sender;
                string btnName = tempBtn.Name;
                int key = 0;
                int.TryParse(btnName.Substring(15), out key); //btn_StartRs422_1去掉btn_StartRs422_剩余为key
                byte addr1 = (byte)(0x91 + key - 1);
                byte addr2 = (byte)(0x9D + 2 * (key - 1));
                byte addr3 = (byte)(addr2 + 1);

                if (tempBtn.Text == "启动监测")
                {
                    SynByte[2 * (key - 1)] = (Byte)int.Parse(myTextBox_422_YKs[key - 1].Text.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    SynByte[2 * (key - 1) + 1] = (Byte)int.Parse(myTextBox_422_YKs[key - 1].Text.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);

                    DealWith_btn_StartRs422_1_Click("open", key, myTextBox_422_YKs[key - 1], addr1, addr2, addr3);
                    DealRs422Tag[key - 1] = true;
                    new Thread(() => { DealWithRs422Fun(key); }).Start();

                    tempBtn.Text = "停止检测";

                }
                else
                {
                    DealRs422Tag[key - 1] = false;
                    tempBtn.Text = "启动监测";
                    DealWith_btn_StartRs422_1_Click("close", key, myTextBox_422_YKs[key - 1], addr1, addr2, addr3);
                }
            }
            else
            {
                MyLog.Error("尚未开始接收数据，无法进行监测");
            }
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox5.Checked == false)
            {
                foreach (Control ctr in panel1.Controls)
                {
                    ctr.Enabled = false;
                }
                groupBox2.Enabled = false;
                checkBox5.Enabled = true;

                Register.Byte81H = (byte)(Register.Byte81H & 0xFE);
                USB.SendCMD(Data.SCid, 0x81, Register.Byte81H);

                MyLog.Info("通道1使能关闭");
            }
            else
            {
                foreach (Control ctr in panel1.Controls)
                {
                    ctr.Enabled = true;
                }
                groupBox2.Enabled = true;

                Register.Byte81H = (byte)(Register.Byte81H | 0x01);
                USB.SendCMD(Data.SCid, 0x81, Register.Byte81H);

                MyLog.Info("通道1使能打开");
            }
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox6.Checked == false)
            {
                foreach (Control ctr in panel2.Controls)
                {
                    ctr.Enabled = false;
                }
                groupBox4.Enabled = false;
                checkBox6.Enabled = true;

                Register.Byte88H = (byte)(Register.Byte88H & 0xFE);
                USB.SendCMD(Data.SCid, 0x88, Register.Byte88H);
                MyLog.Info("通道2使能关闭");
            }
            else
            {
                foreach (Control ctr in panel2.Controls)
                {
                    ctr.Enabled = true;
                }
                groupBox4.Enabled = true;

                Register.Byte88H = (byte)(Register.Byte88H | 0x01);
                USB.SendCMD(Data.SCid, 0x88, Register.Byte88H);
                MyLog.Info("通道2使能打开");
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewColumn column = dataGridView1.Columns[e.ColumnIndex];
                if (column is DataGridViewButtonColumn)
                {
                    dataGridView1.Rows[e.RowIndex].Cells[2].Value = 0;
                    dataGridView1.Rows[e.RowIndex].Cells[3].Value = 0;
                    dataGridView1.Rows[e.RowIndex].Cells[4].Value = 0;

                    TotalCount_422[e.RowIndex] = 0;
                    RightCount_422[e.RowIndex] = 0;
                    WrongCount_422[e.RowIndex] = 0;
                }
            }
        }

        private void textBox_422_YK1_Click(object sender, EventArgs e)
        {
            Rs422_HeaderChn_Name = ((TextBox)sender).Name;

            if (myHeaderProduceForm != null)
            {
                myHeaderProduceForm.Activate();
            }
            else
            {
                myHeaderProduceForm = new FrameHeaderProduceForm(this);
            }
            myHeaderProduceForm.ShowDialog();
        }



        private void button16_Click(object sender, EventArgs e)
        {
            //clk正常
            Register.Byte81H = (byte)(Register.Byte81H & 0xFB);
            USB.SendCMD(Data.SCid, 0x81, Register.Byte81H);

            button16.BackColor = Color.Yellow;
            button14.BackColor = Color.Transparent;
        }





        private void button11_Click(object sender, EventArgs e)
        {
            //数据故障
            Register.Byte81H = (byte)(Register.Byte81H | 0x10);
            USB.SendCMD(Data.SCid, 0x81, Register.Byte81H);
            button11.BackColor = Color.Yellow;
            button12.BackColor = Color.Transparent;
        }


        private void button12_Click(object sender, EventArgs e)
        {
            //数据正常
            Register.Byte81H = (byte)(Register.Byte81H & 0xEF);
            USB.SendCMD(Data.SCid, 0x81, Register.Byte81H);
            button12.BackColor = Color.Yellow;
            button11.BackColor = Color.Transparent;
        }


        private void button13_Click(object sender, EventArgs e)
        {//锁定故障
            Register.Byte81H = (byte)(Register.Byte81H | 0x08);
            USB.SendCMD(Data.SCid, 0x81, Register.Byte81H);
            button13.BackColor = Color.Yellow;
            button15.BackColor = Color.Transparent;
        }


        private void button14_Click(object sender, EventArgs e)
        {
            //clk故障
            Register.Byte81H = (byte)(Register.Byte81H | 0x04);
            USB.SendCMD(Data.SCid, 0x81, Register.Byte81H);
            button14.BackColor = Color.Yellow;
            button16.BackColor = Color.Transparent;
        }

        private void button15_Click(object sender, EventArgs e)
        {//锁定正常
            Register.Byte81H = (byte)(Register.Byte81H & 0xF7);
            USB.SendCMD(Data.SCid, 0x81, Register.Byte81H);
            button15.BackColor = Color.Yellow;
            button13.BackColor = Color.Transparent;
        }

        private void button22_Click(object sender, EventArgs e)
        {
            Register.Byte88H = (byte)(Register.Byte88H & 0xFB);
            USB.SendCMD(Data.SCid, 0x88, Register.Byte88H);
            button22.BackColor = Color.Yellow;
            button20.BackColor = Color.Transparent;
        }

        private void button20_Click(object sender, EventArgs e)
        {
            Register.Byte88H = (byte)(Register.Byte88H | 0x04);
            USB.SendCMD(Data.SCid, 0x88, Register.Byte88H);
            button20.BackColor = Color.Yellow;
            button22.BackColor = Color.Transparent;
        }

        private void button21_Click(object sender, EventArgs e)
        {
            Register.Byte88H = (byte)(Register.Byte88H & 0xF7);
            USB.SendCMD(Data.SCid, 0x88, Register.Byte88H);
            button21.BackColor = Color.Yellow;
            button19.BackColor = Color.Transparent;
        }

        private void button19_Click(object sender, EventArgs e)
        {
            Register.Byte88H = (byte)(Register.Byte88H | 0x08);
            USB.SendCMD(Data.SCid, 0x88, Register.Byte88H);
            button19.BackColor = Color.Yellow;
            button21.BackColor = Color.Transparent;
        }

        private void button18_Click(object sender, EventArgs e)
        {
            Register.Byte88H = (byte)(Register.Byte88H & 0xEF);
            USB.SendCMD(Data.SCid, 0x88, Register.Byte88H);
            button18.BackColor = Color.Yellow;
            button17.BackColor = Color.Transparent;
        }

        private void button17_Click(object sender, EventArgs e)
        {
            Register.Byte88H = (byte)(Register.Byte88H | 0x10);
            USB.SendCMD(Data.SCid, 0x88, Register.Byte88H);
            button17.BackColor = Color.Yellow;
            button18.BackColor = Color.Transparent;
        }

        private void button23_Click(object sender, EventArgs e)
        {
            if (comboBox8.Text == "0")
            {
                MessageBox.Show("捕获序列至少为1");
                comboBox8.Text = "1";
            }
            int temp2 = 0;
            bool ret = int.TryParse(comboBox8.Text, out temp2);
            if (!ret)
            {
                MessageBox.Show("检查输入参数！");
                comboBox8.Text = "1";
            }

            button24.Enabled = true;
            button24.BackColor = Color.Aquamarine;
        }

        private void button24_Click(object sender, EventArgs e)
        {
            byte Byte82H = 0x00;
            byte Byte83H = 0x00;
            byte Byte84H = 0x00;
            byte Byte85H = 0x00;
            byte Byte86H = 0x00;
            byte Byte87H = 0x00;

            string str1 = comboBox7.Text;
            if (str1 == "1")
                Byte82H = (byte)(Byte82H | 0x01);
            else
                Byte82H = (byte)(Byte82H & 0xFE);

            int temp2 = int.Parse(comboBox8.Text);

            Byte82H = (byte)(Byte82H | ((temp2 & 0x3F) << 1));
            Byte83H = (byte)((temp2 & 0x7CF) >> 6);

            string str3 = comboBox9.Text;
            int temp3 = int.Parse(str3);
            Byte84H = (byte)((temp3 & 0x3F) << 1);
            Byte85H = (byte)((temp3 & 0x7CF) >> 6);

            string str4 = comboBox10.Text;
            int temp4 = int.Parse(str4);

            if (temp4 > 0)
                Byte87H = (byte)(Byte87H | 0x40);
            else
                Byte87H = (byte)(Byte87H & 0x3F);

            temp4 = System.Math.Abs(temp4);
            Byte86H = (byte)(temp4 & 0x7F);
            Byte87H = (byte)(Byte87H | ((temp4 & 0xF80) >> 7));

            USB.SendCMD(Data.SCid, 0x82, Byte82H);
            USB.SendCMD(Data.SCid, 0x83, Byte83H);

            USB.SendCMD(Data.SCid, 0x84, Byte84H);
            USB.SendCMD(Data.SCid, 0x85, Byte85H);

            if (checkBox2.Checked)
            {
                USB.SendCMD(Data.SCid, 0x86, Byte86H);
                USB.SendCMD(Data.SCid, 0x87, Byte87H);
            }


            button24.Enabled = false;
            button24.BackColor = Color.Gray;
            MyLog.Info("通道1加载配置完成");
        }

        private void button26_Click(object sender, EventArgs e)
        {
            if (comboBox13.Text == "0")
            {
                MessageBox.Show("捕获序列至少为1");
                comboBox13.Text = "1";
            }

            int temp2 = 0;
            bool ret = int.TryParse(comboBox13.Text, out temp2);
            if (!ret)
            {
                MessageBox.Show("检查输入参数！");
                comboBox13.Text = "1";
            }

            button25.Enabled = true;
            button25.BackColor = Color.Aquamarine;
        }

        private void button25_Click(object sender, EventArgs e)
        {
            byte Byte89H = 0x00;
            byte Byte8AH = 0x00;
            byte Byte8BH = 0x00;
            byte Byte8CH = 0x00;
            byte Byte8DH = 0x00;
            byte Byte8EH = 0x00;

            string str1 = comboBox14.Text;
            if (str1 == "1")
                Byte89H = (byte)(Byte89H | 0x01);
            else
                Byte89H = (byte)(Byte89H & 0xFE);

            int temp2 = int.Parse(comboBox13.Text);
            Byte89H = (byte)(Byte89H | ((temp2 & 0x3F) << 1));
            Byte8AH = (byte)((temp2 & 0x7CF) >> 6);

            string str3 = comboBox3.Text;
            int temp3 = int.Parse(str3);
            Byte8BH = (byte)((temp3 & 0x3F) << 1);
            Byte8CH = (byte)((temp3 & 0x7CF) >> 6);

            string str4 = comboBox12.Text;
            int temp4 = int.Parse(str4);

            if (temp4 > 0)
                Byte8EH = (byte)(Byte8EH | 0x40);
            else
                Byte8EH = (byte)(Byte8EH & 0x3F);

            temp4 = System.Math.Abs(temp4);
            Byte8DH = (byte)(temp4 & 0x7F);
            Byte8EH = (byte)(Byte8EH | ((temp4 & 0xF80) >> 7));

            USB.SendCMD(Data.SCid, 0x89, Byte89H);
            USB.SendCMD(Data.SCid, 0x8A, Byte8AH);

            USB.SendCMD(Data.SCid, 0x8B, Byte8BH);
            USB.SendCMD(Data.SCid, 0x8C, Byte8CH);

            if (checkBox3.Checked)
            {
                USB.SendCMD(Data.SCid, 0x8D, Byte8DH);
                USB.SendCMD(Data.SCid, 0x8E, Byte8EH);
            }


            button25.Enabled = false;
            button25.BackColor = Color.Gray;

            MyLog.Info("通道2加载配置完成");
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox7.Checked)
            {
                foreach (Control btn in groupBox6.Controls)
                {
                    btn.Enabled = true;
                }
            }
            else
            {
                foreach (Control btn in groupBox6.Controls)
                {
                    btn.Enabled = false;
                    btn.BackColor = Color.Transparent;
                }
                Register.Byte81H = (byte)(Register.Byte81H & 0xFB);
                USB.SendCMD(Data.SCid, 0x81, Register.Byte81H);
                Register.Byte81H = (byte)(Register.Byte81H & 0xF7);
                USB.SendCMD(Data.SCid, 0x81, Register.Byte81H);
                Register.Byte81H = (byte)(Register.Byte81H & 0xEF);
                USB.SendCMD(Data.SCid, 0x81, Register.Byte81H);
            }
            checkBox7.Enabled = true;
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox8.Checked)
            {
                foreach (Control btn in groupBox7.Controls)
                {
                    btn.Enabled = true;
                }
            }
            else
            {
                foreach (Control btn in groupBox7.Controls)
                {
                    btn.Enabled = false;
                    btn.BackColor = Color.Transparent;
                }
                Register.Byte88H = (byte)(Register.Byte88H & 0xFB);
                USB.SendCMD(Data.SCid, 0x88, Register.Byte88H);
                Register.Byte88H = (byte)(Register.Byte88H & 0xF7);
                USB.SendCMD(Data.SCid, 0x88, Register.Byte88H);
                Register.Byte88H = (byte)(Register.Byte88H & 0xEF);
                USB.SendCMD(Data.SCid, 0x88, Register.Byte88H);
            }
            checkBox8.Enabled = true;
        }



        public delegate void DeleUpdateTextbox(string dataRe);
        public delegate void DeleUpdateListView(byte[] byteRe);
        public delegate void DeleUpdateDataGrid(string[] dataRe, int channel);

        public delegate void DeleUpdateDataGridOC(Byte[] dataRe);

        byte[] CADU = new byte[1024];
        byte[] RecvTemp = new byte[2048];
        byte[] RecvFirst = new byte[4096];
        int RecvFirstTag = 0;

        int RecvTempTag = 0;

        bool NotFindHead = true;
        int HeadPosition = 0;

        public int RecvTotalBag = 0;
        public int RecvWrongBag = 0;
        public int RecvRightBag = 0;
        public int RecvTotalBag_tb1 = 0;
        public int RecvWrongBag_tb1 = 0;
        public int RecvRightBag_tb1 = 0;
        public static int RecvTotalBag_tb2 = 0;
        public static int RecvWrongBag_tb2 = 0;
        public static int RecvRightBag_tb2 = 0;

        private void UpdateTextbox_YC1(string dataRe)
        {
            this.textBox_ShowYC1.AppendText(DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss] ") + dataRe + "\n");
        }

        private void UpdateTextbox_YC2(string dataRe)
        {
            this.textBox_ShowYC2.AppendText(DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss] ") + dataRe + "\n");
        }

        /// <summary>
        /// 更新收到的同步下行遥测数据listview
        /// </summary>
        /// <param name="dataRe"></param>
        private void UpdateListViewTB1(byte[] byteRe)
        {
            string tbstr = byteRe[0].ToString("x2") + byteRe[1].ToString("x2") + byteRe[2].ToString("x2") + byteRe[3].ToString("x2");
            listView2.Items[0].SubItems[1].Text = tbstr;
            string VersinStr = ((byteRe[4] & 0xC0) >> 6).ToString("x2");
            listView2.Items[0].SubItems[2].Text = VersinStr;

            byte b1 = (byte)((byteRe[4] & 0x3f) << 2);
            byte b2 = (byte)((byteRe[5] & 0xC0) >> 6);
            string SCIDstr = (b1 + b2).ToString("x2");
            listView2.Items[0].SubItems[3].Text = SCIDstr;

            string VCIDstr = (byteRe[5] & 0x3f).ToString("x2");
            listView2.Items[0].SubItems[4].Text = VCIDstr;

            //VCUDCiybterStr用10进制表示
            string VCIDCountStr = (byteRe[6] * 65536 + byteRe[7] * 256 + byteRe[8]).ToString();
            listView2.Items[0].SubItems[5].Text = VCIDCountStr;

            string ReviewFlagStr = ((byteRe[9] & 0x80) >> 7).ToString("x2");
            listView2.Items[0].SubItems[6].Text = ReviewFlagStr;

            string ReserveFlagStr = (byteRe[9] & 0x7f).ToString("x2");
            listView2.Items[0].SubItems[7].Text = ReserveFlagStr;

            byte[] tempRe = new byte[6];
            Array.Copy(byteRe, 10, tempRe, 0, 6);
            listView2.Items[0].SubItems[8].Text = Function.DecodeTime(tempRe);

            RecvTotalBag_tb1 += 1;
            this.textBox3.Text = RecvTotalBag_tb1.ToString();


            textBox_ShowYC1.Clear();
            string tempstr = "";
            for (int i = 0; i < 1024; i++) tempstr += byteRe[i].ToString("x2");
            textBox_ShowYC1.AppendText(tempstr);

            ushort CRC = 0xffff;
            ushort genpoly = 0x1021;
            for (int i = 4; i < 1022; i = i + 1)
            {
                CRC = Function.CRChware(byteRe[i], genpoly, CRC);
            }
            MyLog.Info("Calc 通道1 CRC = " + CRC.ToString("x4"));

            ushort temp = (ushort)((ushort)(byteRe[1022] << 8) | (ushort)byteRe[1023]);
            if (CRC == temp)
            {
                RecvRightBag_tb1++;
            }
            else
            {
                RecvWrongBag_tb1++;
            }
            this.textBox4.Text = RecvWrongBag_tb1.ToString();
            this.textBox5.Text = RecvRightBag_tb1.ToString();
        }

        /// <summary>
        /// 更新收到的同步下行遥测数据listview2
        /// </summary>
        /// <param name="dataRe"></param>
        private void UpdateListViewTB2(byte[] byteRe)
        {
            string tbstr = byteRe[0].ToString("x2") + byteRe[1].ToString("x2") + byteRe[2].ToString("x2") + byteRe[3].ToString("x2");
            listView2.Items[1].SubItems[1].Text = tbstr;
            string VersinStr = ((byteRe[4] & 0xC0) >> 6).ToString("x2");
            listView2.Items[1].SubItems[2].Text = VersinStr;

            byte b1 = (byte)((byteRe[4] & 0x3f) << 2);
            byte b2 = (byte)((byteRe[5] & 0xC0) >> 6);
            string SCIDstr = (b1 + b2).ToString("x2");
            listView2.Items[1].SubItems[3].Text = SCIDstr;

            string VCIDstr = (byteRe[5] & 0x3f).ToString("x2");
            listView2.Items[1].SubItems[4].Text = VCIDstr;

            //VCUDCiybterStr用10进制表示
            string VCIDCountStr = (byteRe[6] * 65536 + byteRe[7] * 256 + byteRe[8]).ToString();
            listView2.Items[1].SubItems[5].Text = VCIDCountStr;

            string ReviewFlagStr = ((byteRe[9] & 0x80) >> 7).ToString("x2");
            listView2.Items[1].SubItems[6].Text = ReviewFlagStr;

            string ReserveFlagStr = (byteRe[9] & 0x7f).ToString("x2");
            listView2.Items[1].SubItems[7].Text = ReserveFlagStr;

            byte[] tempRe = new byte[6];
            Array.Copy(byteRe, 10, tempRe, 0, 6);
            listView2.Items[1].SubItems[8].Text = Function.DecodeTime(tempRe);

            RecvTotalBag_tb2 += 1;
            this.textBox6.Text = RecvTotalBag_tb2.ToString();

            string tempstr = "";
            for (int i = 0; i < 1024; i++) tempstr += byteRe[i].ToString("x2");

            textBox_ShowYC2.Clear();

            textBox_ShowYC2.AppendText(tempstr);

            ushort CRC = 0xffff;
            ushort genpoly = 0x1021;
            for (int i = 4; i < 1022; i = i + 1)
            {
                CRC = Function.CRChware(byteRe[i], genpoly, CRC);
            }
            //     MyLog.Info("Calc 通道2 CRC = " + CRC.ToString("x4"));

            ushort temp = (ushort)((ushort)(byteRe[1022] << 8) | (ushort)byteRe[1023]);
            if (CRC == temp)
            {
                RecvRightBag_tb2++;
            }
            else
            {
                RecvWrongBag_tb2++;
            }

            this.textBox7.Text = RecvWrongBag_tb2.ToString();
            this.textBox8.Text = RecvRightBag_tb2.ToString();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            textBox3.Clear();
            textBox4.Clear();
            textBox5.Clear();
            textBox_ShowYC1.Clear();
            RecvTotalBag_tb1 = 0;
            RecvWrongBag_tb1 = 0;
            RecvRightBag_tb1 = 0;
        }

        private void button10_Click(object sender, EventArgs e)
        {
            textBox6.Clear();
            textBox7.Clear();
            textBox8.Clear();
            textBox_ShowYC2.Clear();
            RecvTotalBag_tb2 = 0;
            RecvWrongBag_tb2 = 0;
            RecvRightBag_tb2 = 0;
        }


        FrameEPDU myFrameEPDU1;
        FrameEPDU myFrameEPDU2;
        private void button_EPDU_Click(object sender, EventArgs e)
        {
            TextBox myText = (TextBox)sender;
            switch (myText.Name)
            {
                case "textBox_ShowYC1":
                    Data.Channel = 1;
                    if (myFrameEPDU1 == null || myFrameEPDU1.IsDisposed)
                    {
                        myFrameEPDU1 = new FrameEPDU();
                    }
                    else
                    {
                        myFrameEPDU1.Activate();
                    }
                    myFrameEPDU1.Show();
                    break;
                case "textBox_ShowYC2":
                    Data.Channel = 2;
                    if (myFrameEPDU2 == null || myFrameEPDU2.IsDisposed)
                    {
                        myFrameEPDU2 = new FrameEPDU();
                    }
                    else
                    {
                        myFrameEPDU2.Activate();
                    }
                    myFrameEPDU2.Show();
                    break;
                default:
                    Data.Channel = 1;
                    break;
            }
        }


        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                if (e.ColumnIndex == 1)
                {
                    //加载码本
                    openFileDialog2.InitialDirectory = Program.GetStartupPath() + @"码本\数传码源码本";
                    if (openFileDialog2.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            string temp = System.IO.File.ReadAllText(openFileDialog2.FileName);
                            dataGridView2.Rows[e.RowIndex].Cells[2].Value = temp;
                        }
                        catch
                        {
                            MyLog.Error("加载发送码本失败！");
                        }
                    }
                }

                if (e.ColumnIndex == 4)
                {
                    if (dataGridView2.Rows[e.RowIndex].Cells[4].FormattedValue.ToString() == "发送")
                    {
                        dataGridView2.Rows[e.RowIndex].Cells[4].Value = "停止";
                        dataGridView2.Rows[e.RowIndex].Cells[3].ReadOnly = true;
                        //设置输出码速率
                        int tempfreq = 1;
                        string Str_Rate = dataGridView2.Rows[e.RowIndex].Cells[3].FormattedValue.ToString();
                        int.TryParse(Str_Rate.Substring(0, Str_Rate.Length - 4), out tempfreq);
                        if (tempfreq <= 0 || tempfreq > 60)
                        {
                            MyLog.Error("频率设置必须在0-60之间");
                        }
                        int tempbyte = 60 / tempfreq;
                        USB.SendCMD(Data.SCid, (byte)(0xD7 + e.RowIndex), (byte)tempbyte);


                        //获得设置的输出内容
                        string Str_Content = (string)dataGridView2.Rows[e.RowIndex].Cells[2].Value;
                        if ((Str_Content.Length % 2) != 0)
                        {
                            MyLog.Error("请以16进制格式输入内容，如00代表0，ff代表255");
                            return;
                        }
                        int outlenth = Str_Content.Length / 2;
                        if (outlenth > 2040)
                        {
                            MyLog.Error("码本包含数据超过2K，无法发送");
                            return;
                        }

                        byte addr = (byte)(0xCF + e.RowIndex);
                        string Header = (int.Parse("1D0E", System.Globalization.NumberStyles.HexNumber) + e.RowIndex).ToString("x4");

                        UpdateSC_MY(addr, Str_Content, Header);
                        MyLog.Info("数传码源通道:" + (e.RowIndex + 1).ToString() + "--发送内容:" + Str_Content + "--发送速率:" + Str_Rate);

                        String ConfigStr = "ShuChuan0" + (1 + e.RowIndex).ToString();
                        SetConfigValue(ConfigStr, Str_Content);

                        String ConfigStr_Rate = "ShuChuan0" + (1 + e.RowIndex).ToString() + "_Rate";
                        SetConfigValue(ConfigStr_Rate, Str_Rate);
                    }
                    else
                    {
                        dataGridView2.Rows[e.RowIndex].Cells[4].Value = "发送";
                        dataGridView2.Rows[e.RowIndex].Cells[3].ReadOnly = false;
                        byte addr = (byte)(0xCF + e.RowIndex);
                        USB.SendCMD(Data.SCid, addr, 0x40);
                        MyLog.Info("数传码源通道:" + (e.RowIndex + 1).ToString() + "--停止发送");
                    }
                }
            }
        }

        private void UpdateSC_MY(byte addr, string data, string Header)
        {
            USB.SendCMD(Data.SCid, addr, 0x40);
            USB.SendCMD(Data.SCid, addr, 0x00);

            string Str_Content = data;

            int lenth = (Str_Content.Length) / 8;

            byte[] temp = StrToHexByte(Header + lenth.ToString("x4") + Str_Content + "C0DEC0DEC0DEC0DEC0DEC0DEC0DEC0DE");

            USB.SendData(Data.SCid, temp);

            Trace.WriteLine("发送数传指令：" + addr.ToString("x4") + "--头：" + Header);

        }

        private void UpdateSCROOM(byte addr, string data, string Header)
        {

            USB.SendCMD(Data.SCid, addr, 0x40);
            USB.SendCMD(Data.SCid, addr, 0x00);

            string Str_Content = data;
            //      int lenth = (Str_Content.Length) / 2;
            int lenth = (Str_Content.Length) / 8;
            byte[] temp = StrToHexByte(Header + lenth.ToString("x4") + Str_Content + "C0DEC0DEC0DEC0DE");
            // USB.SendDataByInt(Data.SCid, temp);
            USB.SendData(Data.SCid, temp);
            Trace.WriteLine("发送数传指令：" + addr.ToString("x4") + "--头：" + Header);
        }

        private void dataGridView3_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                if (e.ColumnIndex == 1)
                {
                    //是否存盘
                    DataGridViewCheckBoxCell checkcell = (DataGridViewCheckBoxCell)dataGridView3.Rows[e.RowIndex].Cells[1];
                    Boolean flag = Convert.ToBoolean(checkcell.EditedFormattedValue);
                    if (flag == true)
                    {
                        if (e.RowIndex == 0) Data.SaveSC1 = true;
                        if (e.RowIndex == 1) Data.SaveSC2 = true;
                    }
                    else
                    {
                        if (e.RowIndex == 0) Data.SaveSC1 = false;
                        if (e.RowIndex == 1) Data.SaveSC2 = false;
                    }

                }
                if (e.ColumnIndex == 2)
                {
                    //点击选择目录
                    DataGridViewColumn column = dataGridView3.Columns[e.ColumnIndex];
                    if (column is DataGridViewButtonColumn)
                    {
                        FolderBrowserDialog dialog = new FolderBrowserDialog();
                        dialog.Description = "请选择要保存数据数据的目录";
                        dialog.SelectedPath = System.AppDomain.CurrentDomain.BaseDirectory.ToString();
                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            string foldPath = dialog.SelectedPath;
                            dataGridView3.Rows[e.RowIndex].Cells[3].Value = foldPath;
                        }
                    }
                }
                if (e.ColumnIndex == 5)
                {
                    //启动,禁止
                    DataGridViewColumn column = dataGridView3.Columns[e.ColumnIndex];
                    if (column is DataGridViewButtonColumn)
                    {
                        if (dataGridView3.Rows[e.RowIndex].Cells[5].FormattedValue.ToString() == "启动")
                        {
                            if (e.RowIndex == 0) Data.SCRecvCounts1 = 0;
                            if (e.RowIndex == 1) Data.SCRecvCounts2 = 0;

                            dataGridView3.Rows[e.RowIndex].Cells[5].Value = "停止";
                            byte addr = (byte)(0xE7 + e.RowIndex);
                            USB.SendCMD(Data.SCid, addr, 0x40);
                            Trace.WriteLine("发送下行数传" + e.RowIndex.ToString() + "通道使能");
                        }
                        else
                        {
                            dataGridView3.Rows[e.RowIndex].Cells[5].Value = "启动";
                            byte addr = (byte)(0xE7 + e.RowIndex);
                            USB.SendCMD(Data.SCid, addr, 0x00);
                            Trace.WriteLine("发送下行数传" + e.RowIndex.ToString() + "通道禁止");
                        }
                    }
                }

            }
        }

        private void dataGridView6_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                string temp = dataGridView6.Rows[e.RowIndex].Cells[1].FormattedValue.ToString();
                bool ret = false;
                int value = 0;
                ret = int.TryParse(temp, out value);
                if (ret)
                {
                    if (value <= 0 || value > 2000000)
                    {
                        dataGridView6.Rows[e.RowIndex].Cells[1].Value = "80000";
                        MessageBox.Show("脉宽必须设置在0-2000000us之间");
                    }
                }
                else
                {
                    dataGridView6.Rows[e.RowIndex].Cells[1].Value = "80000";
                    MessageBox.Show("输入有效的脉宽值");
                }
            }
        }

        private void dataGridView6_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                if (e.ColumnIndex == 2)                        //输出指令
                {
                    int CurrentRow = e.RowIndex;
                    string Str_MK = dataGridView6.Rows[e.RowIndex].Cells[1].FormattedValue.ToString();
                    int Value = 0;
                    int.TryParse(Str_MK, out Value);
                    if (Value <= 0 || Value > 2000000)
                    {
                        MyLog.Error("脉宽必须设置在0-2000000us之间");
                    }

                    byte addr1 = (byte)(0x81 + 4 * e.RowIndex);
                    byte addr2 = (byte)(0x82 + 4 * e.RowIndex);
                    byte addr3 = (byte)(0x83 + 4 * e.RowIndex);
                    byte addr4 = (byte)(0x84 + 4 * e.RowIndex);


                    byte b1 = (byte)(Value & 0x7f);
                    byte b2 = (byte)((Value & 0x3f80) >> 7);
                    byte b3 = (byte)((Value & 0x1fC000) >> 14);
                    USB.SendCMD(2, addr2, b1);
                    USB.SendCMD(2, addr3, b2);
                    USB.SendCMD(2, addr4, b3);
                    //边沿出发，发送一次脉冲
                    USB.SendCMD(2, addr1, 0x1);
                    USB.SendCMD(2, addr1, 0x0);

                    //byte addr = (byte)(0xCF + e.RowIndex);
                    //string Header = (int.Parse("1D0E", System.Globalization.NumberStyles.HexNumber) + e.RowIndex).ToString("x4");

                    MyLog.Info("指令输出:" + dataGridView6.Rows[e.RowIndex].Cells[0].Value + "--输出脉宽:" + Str_MK + "us");

                    String Str_Content = Str_MK;
                    String ConfigStr = "OCOutValue1_0" + (1 + e.RowIndex).ToString();
                    SetConfigValue(ConfigStr, Str_Content);
                }
            }
        }

        private void dataGridView7_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                if (e.ColumnIndex == 2)                        //输出指令
                {
                    int CurrentRow = e.RowIndex;
                    string Str_MK = dataGridView7.Rows[e.RowIndex].Cells[1].FormattedValue.ToString();
                    int Value = 0;
                    int.TryParse(Str_MK, out Value);
                    if (Value <= 0 || Value > 2000000)
                    {
                        MyLog.Error("脉宽必须设置在0-2000000us之间");
                    }

                    byte addr1 = (byte)(0x81 + 4 * (e.RowIndex + 8));
                    byte addr2 = (byte)(0x82 + 4 * (e.RowIndex + 8));
                    byte addr3 = (byte)(0x83 + 4 * (e.RowIndex + 8));
                    byte addr4 = (byte)(0x84 + 4 * (e.RowIndex + 8));


                    byte b1 = (byte)(Value & 0x7f);
                    byte b2 = (byte)((Value & 0x3f80) >> 7);
                    byte b3 = (byte)((Value & 0x1fC000) >> 14);
                    USB.SendCMD(2, addr2, b1);
                    USB.SendCMD(2, addr3, b2);
                    USB.SendCMD(2, addr4, b3);
                    //边沿出发，发送一次脉冲
                    USB.SendCMD(2, addr1, 0x1);
                    USB.SendCMD(2, addr1, 0x0);

                    MyLog.Info("指令输出:" + dataGridView6.Rows[e.RowIndex].Cells[0].Value + "--输出脉宽:" + Str_MK + "us");

                    String Str_Content = Str_MK;
                    String ConfigStr = "OCOutValue2_0" + (1 + e.RowIndex).ToString();
                    SetConfigValue(ConfigStr, Str_Content);

                }
            }
        }


        private void btn_modify_Click(object sender, EventArgs e)
        {
            if (myDAModifyForm != null)
            {
                myDAModifyForm.Activate();
            }
            else
            {
                myDAModifyForm = new DAModifyForm(this);
            }
            myDAModifyForm.ShowDialog();

            //foreach (var dt in dtRList)
            //{
            //    foreach (DataRow dr in dt.Rows)
            //    {
            //        dr["电阻"] = 100;
            //    }
            //}
        }

        private void btn_Vload_Click(object sender, EventArgs e)
        {
            String Path = Program.GetStartupPath() + @"码本\DA码本\";
            openFileDialog_da.InitialDirectory = Path;

            if (openFileDialog_da.ShowDialog() == DialogResult.OK)
            {
                MyLog.Info("载入DA码本成功！");

                string[] content = File.ReadAllLines(openFileDialog_da.FileName);
                string[] temp = new string[3];

                if (content.Length >= 128)
                {
                    for (int i = 0; i < 128; i++)
                    {
                        temp = content[i].Split(',');
                        if (i >= 0 && i < 32)
                        {
                            dtDA1.Rows[i]["名称"] = temp[1].Trim();
                            dtDA1.Rows[i]["电压"] = double.Parse(temp[2].Trim());
                        }
                        else if (i >= 32 && i < 64)
                        {
                            dtDA2.Rows[i - 32]["名称"] = temp[1].Trim();
                            dtDA2.Rows[i - 32]["电压"] = double.Parse(temp[2].Trim());
                        }
                        else if (i >= 64 && i < 96)
                        {
                            dtDA3.Rows[i - 64]["名称"] = temp[1].Trim();
                            dtDA3.Rows[i - 64]["电压"] = double.Parse(temp[2].Trim());
                        }
                        else if (i >= 96 && i < 128)
                        {
                            dtDA4.Rows[i - 96]["名称"] = temp[1].Trim();
                            dtDA4.Rows[i - 96]["电压"] = double.Parse(temp[2].Trim());
                        }
                        else
                        {
                            //Nothing happens
                        }
                    }
                }
            }
        }

        private void btn_VSave_Click(object sender, EventArgs e)
        {
            String Path = Program.GetStartupPath() + @"码本\DA码本\";
            if (!Directory.Exists(Path))
                Directory.CreateDirectory(Path);

            saveFileDialog_da.InitialDirectory = Path;

            saveFileDialog_da.Filter = "文本文件(*.txt)|*.txt|All files(*.*)|*.*";
            saveFileDialog_da.FilterIndex = 1;
            saveFileDialog_da.RestoreDirectory = true;

            if (saveFileDialog_da.ShowDialog() == DialogResult.OK)
            {
                String ModifyStr1 = "";
                String ModifyStr2 = "";
                String ModifyStr3 = "";
                String ModifyStr4 = "";
                for (int i = 0; i < 32; i++)
                {
                    ModifyStr1 += dtDA1.Rows[i]["ID"] + "," + dtDA1.Rows[i]["名称"] + "," + dtDA1.Rows[i]["电压"] + "\r\n";
                    ModifyStr2 += dtDA2.Rows[i]["ID"] + "," + dtDA2.Rows[i]["名称"] + "," + dtDA2.Rows[i]["电压"] + "\r\n";
                    ModifyStr3 += dtDA3.Rows[i]["ID"] + "," + dtDA3.Rows[i]["名称"] + "," + dtDA3.Rows[i]["电压"] + "\r\n";
                    ModifyStr4 += dtDA4.Rows[i]["ID"] + "," + dtDA4.Rows[i]["名称"] + "," + dtDA4.Rows[i]["电压"] + "\r\n";

                }
                string localFilePath = saveFileDialog_da.FileName.ToString(); //获得文件路径 

                FileStream file0 = new FileStream(localFilePath, FileMode.Create);
                StreamWriter sw = new StreamWriter(file0);
                sw.WriteLine(ModifyStr1 + ModifyStr2 + ModifyStr3 + ModifyStr4);
                sw.Flush();
                sw.Close();
                file0.Close();
                MessageBox.Show("存储文件成功！", "保存文件");
            }
        }

        private void btn_VOut_Click(object sender, EventArgs e)
        {
            for (int j = 0; j < 4; j++)
            {
                for (int i = 0; i < 32; i++)
                {
                    double value = (double)dtDAList[j].Rows[i]["电压"];

                    if (value < 0 || value > 4.8)
                    {
                        MessageBox.Show("请输入0-4.8V电压值!!");
                    }
                    else
                    {
                        clcDAValue(j, i, value);
                    }
                }

                byte[] DASend = new byte[128 + 8];
                DASend[0] = 0x1D;
                DASend[1] = (byte)(0x20 + j);         //1D20 1D21 1D22 1D23对应4个DA芯片
                DASend[2] = 0x00;
                DASend[3] = 0x20;//0x0080 = 128

                switch (j)
                {
                    case 0:
                        Array.Copy(DAByteA, 0, DASend, 4, 128);
                        break;
                    case 1:
                        Array.Copy(DAByteB, 0, DASend, 4, 128);
                        break;
                    case 2:
                        Array.Copy(DAByteC, 0, DASend, 4, 128);
                        break;
                    case 3:
                        Array.Copy(DAByteD, 0, DASend, 4, 128);
                        break;
                    default:
                        break;
                }
                DASend[132] = 0xC0;
                DASend[133] = 0xDE;
                DASend[134] = 0xC0;
                DASend[135] = 0xDE;

                USB.SendCMD(Data.DARid, 0x81, (byte)((0x01) << j));
                USB.SendCMD(Data.DARid, 0x81, 0x00);

                USB.SendData(Data.DARid, DASend);
            }
        }


        private void dataGridView_V1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView dgv = (DataGridView)sender;
            string SenderName = dgv.Name;
            int LastNameValue = 0;

            int.TryParse(SenderName.Substring(14), out LastNameValue);     //SenderName = dataGridView_V4

            if (e.RowIndex >= 0)
            {
                if (e.ColumnIndex == 2)
                {
                    try
                    {
                        double t = (double)dtDAList[LastNameValue - 1].Rows[e.RowIndex]["电压"];
                        if (t < 0 || t > 4.8)
                        {
                            dtDAList[LastNameValue - 1].Rows[e.RowIndex]["电压"] = 4.8;
                        }
                    }
                    catch (Exception ex)
                    {
                        MyLog.Error(ex.Message + "请输入正确的DA参数!");
                        dtDAList[LastNameValue - 1].Rows[e.RowIndex]["电压"] = 4.8;
                    }
                }
            }
        }

        private void dataGridView_V1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView dgv = (DataGridView)sender;
            string SenderName = dgv.Name;
            int LastNameValue = 0;
            int.TryParse(SenderName.Substring(14), out LastNameValue);     //SenderName = dataGridView_V4
            int t = LastNameValue - 1;

            if (e.RowIndex >= 0)
            {
                if (e.ColumnIndex == 2)
                {
                    double value = (double)dtDAList[t].Rows[e.RowIndex]["电压"];

                    clcDAValue(t, e.RowIndex, value);

                    byte[] DASend = new byte[128 + 8];
                    DASend[0] = 0x1D;
                    DASend[1] = (byte)(0x20 + t);         //1D20 1D21 1D22 1D23对应4个DA芯片
                    DASend[2] = 0x00;
                    DASend[3] = 0x20;//0x0080 = 128

                    switch (t)
                    {
                        case 0:
                            Array.Copy(DAByteA, 0, DASend, 4, 128);
                            break;
                        case 1:
                            Array.Copy(DAByteB, 0, DASend, 4, 128);
                            break;
                        case 2:
                            Array.Copy(DAByteC, 0, DASend, 4, 128);
                            break;
                        case 3:
                            Array.Copy(DAByteD, 0, DASend, 4, 128);
                            break;
                        default:
                            break;
                    }
                    DASend[132] = 0xC0;
                    DASend[133] = 0xDE;
                    DASend[134] = 0xC0;
                    DASend[135] = 0xDE;

                    USB.SendCMD(Data.DARid, 0x81, (byte)((0x01) << t));
                    USB.SendCMD(Data.DARid, 0x81, 0x00);

                    USB.SendData(Data.DARid, DASend);
                }
            }
        }

        /// <summary>
        /// 将电压值转化为下发的4Byte
        /// </summary>
        /// <param name="V">0~3:对应datagridview_V1~V4</param>
        /// <param name="row">行数</param>
        /// <param name="value">电压值</param>
        private void clcDAValue(int V, int row, double value)
        {
            Data.SaveConfig(Data.DAconfigPath, "DA_Channel_" + (row + V * 32).ToString(), value.ToString());

            double SendValue = Data.value_a[row + 32 * V] + (Data.value_b[row + 32 * V] * value) / 5.00;
            Int16 temp = Convert.ToInt16(SendValue);
            switch (V)
            {
                case 0:
                    DAByteA[0 + 4 * row] = 0x00;
                    DAByteA[1 + 4 * row] = (byte)(0x40 + (row / 4));
                    byte a = (byte)((temp & 0x3f00) >> 8);
                    byte b = (byte)(((row % 4) & 0x03) << 6);
                    DAByteA[2 + 4 * row] = (byte)(b + a);
                    DAByteA[3 + 4 * row] = (byte)(temp & 0xff);
                    break;
                case 1:
                    DAByteB[0 + 4 * row] = 0x00;
                    DAByteB[1 + 4 * row] = (byte)(0x40 + (row / 4));
                    byte a2 = (byte)((temp & 0x3f00) >> 8);
                    byte b2 = (byte)(((row % 4) & 0x03) << 6);
                    DAByteB[2 + 4 * row] = (byte)(b2 + a2);
                    DAByteB[3 + 4 * row] = (byte)(temp & 0xff);
                    break;
                case 2:
                    DAByteC[0 + 4 * row] = 0x00;
                    DAByteC[1 + 4 * row] = (byte)(0x40 + (row / 4));
                    byte a3 = (byte)((temp & 0x3f00) >> 8);
                    byte b3 = (byte)(((row % 4) & 0x03) << 6);
                    DAByteC[2 + 4 * row] = (byte)(b3 + a3);
                    DAByteC[3 + 4 * row] = (byte)(temp & 0xff);
                    break;
                case 3:
                    DAByteD[0 + 4 * row] = 0x00;
                    DAByteD[1 + 4 * row] = (byte)(0x40 + (row / 4));
                    byte a4 = (byte)((temp & 0x3f00) >> 8);
                    byte b4 = (byte)(((row % 4) & 0x03) << 6);
                    DAByteD[2 + 4 * row] = (byte)(b4 + a4);
                    DAByteD[3 + 4 * row] = (byte)(temp & 0xff);
                    break;
                default:
                    break;
            }


        }

        private void dataGridView_R1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView dgv = (DataGridView)sender;
            string SenderName = dgv.Name;
            int LastNameValue = 0;

            int.TryParse(SenderName.Substring(14), out LastNameValue);     //SenderName = dataGridView_V4

            if (e.RowIndex >= 0)
            {
                if (e.ColumnIndex == 2)
                {
                    double t = 0;
                    string temp = myDGVs_R[LastNameValue - 1].Rows[e.RowIndex].Cells[2].FormattedValue.ToString();
                    bool ret = double.TryParse(temp, out t);
                    if (t < 0 || t > 256 || ret == false)
                    {
                        myDGVs_R[LastNameValue - 1].Rows[e.RowIndex].Cells[2].Value = "256";
                    }

                }
            }
        }

        private void clcRValue(int V, int row, double value)
        {
            Data.SaveConfig(Data.RconfigPath, "R_Channel_" + (row + V * 24).ToString(), value.ToString("0.0"));

            int RValue = (int)(1024 * value);
            int SendR = 0;
            if (RValue < 102400)
            {
                //0~100k 10
                SendR = 0x8400 + ((RValue / 100) & 0x3ff);

            }
            else if (RValue == 102400)
            {
                SendR = 0x8400 + 0x3ff;
            }
            else if (RValue > 102400 && RValue < 204800)
            {
                //100k~200k 01
                SendR = 0x4400 + (((RValue - 102400) / 100) & 0x3ff) + Data.value_R1[row + 24 * V];
            }
            else if (RValue == 204800)
            {
                SendR = 0x4400 + 0x3ff;
            }
            else if (RValue > 204800 && RValue <= 262144)
            {
                //200k~300k 00
                SendR = 0x400 + (((RValue - 102400) / 100) & 0x3ff) + Data.value_R2[row + 24 * V];
            }
            else
            {
                //Deal With ERROR
            }

            switch (V)
            {
                case 0:
                    RByteA[0 + 4 * row] = 0x00;
                    RByteA[1 + 4 * row] = 0x00;
                    RByteA[2 + 4 * row] = (byte)((SendR & 0xff00) >> 8);
                    RByteA[3 + 4 * row] = (byte)(SendR & 0xff);

                    break;
                case 1:
                    RByteB[0 + 4 * row] = 0x00;
                    RByteB[1 + 4 * row] = 0x00;
                    RByteB[2 + 4 * row] = (byte)((SendR & 0xff00) >> 8);
                    RByteB[3 + 4 * row] = (byte)(SendR & 0xff);
                    break;

                case 2:
                    RByteC[0 + 4 * row] = 0x00;
                    RByteC[1 + 4 * row] = 0x00;
                    RByteC[2 + 4 * row] = (byte)((SendR & 0xff00) >> 8);
                    RByteC[3 + 4 * row] = (byte)(SendR & 0xff);

                    break;
                case 3:
                    RByteD[0 + 4 * row] = 0x00;
                    RByteD[1 + 4 * row] = 0x00;
                    RByteD[2 + 4 * row] = (byte)((SendR & 0xff00) >> 8);
                    RByteD[3 + 4 * row] = (byte)(SendR & 0xff);
                    break;
                default:
                    break;
            }


        }

        private void btn_Output_Click(object sender, EventArgs e)
        {
            byte[] RSend = new byte[8 + 192];//帧头4B+160有效40个通道+32无效8通道+帧尾4B
            RSend[0] = 0x1D;
            RSend[1] = 0x24;
            RSend[2] = 0x00;
            RSend[3] = 0x30;//0x0030 = 40

            // for (int i = 0; i < 32; i++) RSend[164 + i] = 0x0;

            RSend[196] = 0xC0;
            RSend[197] = 0xDE;
            RSend[198] = 0xC0;
            RSend[199] = 0xDE;

            byte[] RSend2 = new byte[8 + 192];
            RSend2[0] = 0x1D;
            RSend2[1] = 0x25;
            RSend2[2] = 0x00;
            RSend2[3] = 0x30;//0x0030 = 40
                             //            for (int i = 0; i < 32; i++) RSend2[164 + i] = 0x0;

            RSend2[196] = 0xC0;
            RSend2[197] = 0xDE;
            RSend2[198] = 0xC0;
            RSend2[199] = 0xDE;

            for (int j = 0; j < 4; j++)
            {
                for (int i = 0; i < 24; i++)
                {
                    double value = (double)dtRList[j].Rows[i]["电阻"];
                    clcRValue(j, i, value);

                }

                switch (j)
                {
                    case 0:
                        Array.Copy(RByteA, 0, RSend, 4, 96);
                        break;
                    case 1:
                        Array.Copy(RByteB, 0, RSend, 100, 96);
                        break;
                    case 2:
                        Array.Copy(RByteC, 0, RSend2, 4, 96);
                        break;
                    case 3:
                        Array.Copy(RByteD, 0, RSend2, 100, 96);
                        break;
                    default:
                        break;
                }
            }

            USB.SendCMD(Data.DARid, 0x81, 0x10);
            USB.SendCMD(Data.DARid, 0x81, 0x00);
            USB.SendData(Data.DARid, RSend);
            Thread.Sleep(500);//延时500ms再发送一次，确保设置到位
            USB.SendCMD(Data.DARid, 0x81, 0x10);
            USB.SendCMD(Data.DARid, 0x81, 0x00);
            USB.SendData(Data.DARid, RSend);
            string temp1 = "";
            for (int i = 0; i < 200; i++) temp1 += RSend[i].ToString("x2");
            Trace.WriteLine("Board1:" + temp1);

            USB.SendCMD(Data.DARid, 0x81, 0x20);
            USB.SendCMD(Data.DARid, 0x81, 0x00);
            USB.SendData(Data.DARid, RSend2);
            Thread.Sleep(500);//延时500ms再发送一次，确保设置到位
            USB.SendCMD(Data.DARid, 0x81, 0x20);
            USB.SendCMD(Data.DARid, 0x81, 0x00);
            USB.SendData(Data.DARid, RSend2);
            string temp2 = "";
            for (int i = 0; i < 200; i++) temp2 += RSend2[i].ToString("x2");
            Trace.WriteLine("Board2:" + temp2);
        }

        /// <summary>
        /// 固定板卡初始值，每个通道值设置为0x00000800向下发送
        /// 固定化板卡初始值，每个通道值设置为0x00000C00向下发送
        /// </summary>
        private void SetRBoard(byte b1, byte b2)
        {
            MyLog.Info("初始化数字电阻通道......");

            //初始化板卡，每个通道值设置为0x00000800向下发送
            byte[] RSend = new byte[8 + 192];//帧头4B+192有效48个通道+帧尾4B
            RSend[0] = 0x1D;
            RSend[1] = 0x24;
            RSend[2] = 0x00;
            RSend[3] = 0x30;//0x0030 = 48
            for (int i = 1; i < 49; i++)
            {
                RSend[4 * i] = 0x0;
                RSend[4 * i + 1] = 0x0;
                RSend[4 * i + 2] = b1;
                RSend[4 * i + 3] = b2;
            }
            RSend[196] = 0xC0;
            RSend[197] = 0xDE;
            RSend[198] = 0xC0;
            RSend[199] = 0xDE;

            byte[] RSend2 = new byte[8 + 192];
            RSend2[0] = 0x1D;
            RSend2[1] = 0x25;
            RSend2[2] = 0x00;
            RSend2[3] = 0x30;//0x0030 = 48
            for (int i = 1; i < 49; i++)
            {
                RSend2[4 * i] = 0x0;
                RSend2[4 * i + 1] = 0x0;
                RSend2[4 * i + 2] = b1;
                RSend2[4 * i + 3] = b2;
            }
            RSend2[196] = 0xC0;
            RSend2[197] = 0xDE;
            RSend2[198] = 0xC0;
            RSend2[199] = 0xDE;

            USB.SendCMD(Data.DARid, 0x81, 0x10);
            USB.SendCMD(Data.DARid, 0x81, 0x00);
            USB.SendData(Data.DARid, RSend);

            USB.SendCMD(Data.DARid, 0x81, 0x20);
            USB.SendCMD(Data.DARid, 0x81, 0x00);
            USB.SendData(Data.DARid, RSend2);

        }

        private void initRBoard()
        {
            MyLog.Info("初始化数字电阻通道......");

            //初始化板卡，每个通道值设置为0x00001C03向下发送
            byte[] RSend = new byte[8 + 192];//帧头4B+192有效48个通道+帧尾4B
            RSend[0] = 0x1D;
            RSend[1] = 0x24;
            RSend[2] = 0x00;
            RSend[3] = 0x30;//0x0030 = 48
            for (int i = 1; i < 49; i++)
            {
                RSend[4 * i] = 0x0;
                RSend[4 * i + 1] = 0x0;
                RSend[4 * i + 2] = 0x1c;
                RSend[4 * i + 3] = 0x03;
            }
            RSend[196] = 0xC0;
            RSend[197] = 0xDE;
            RSend[198] = 0xC0;
            RSend[199] = 0xDE;

            byte[] RSend2 = new byte[8 + 192];
            RSend2[0] = 0x1D;
            RSend2[1] = 0x25;
            RSend2[2] = 0x00;
            RSend2[3] = 0x30;//0x0030 = 48
            for (int i = 1; i < 49; i++)
            {
                RSend2[4 * i] = 0x0;
                RSend2[4 * i + 1] = 0x0;
                RSend2[4 * i + 2] = 0x1c;
                RSend2[4 * i + 3] = 0x03;
            }
            RSend2[196] = 0xC0;
            RSend2[197] = 0xDE;
            RSend2[198] = 0xC0;
            RSend2[199] = 0xDE;

            USB.SendCMD(Data.DARid, 0x81, 0x10);
            USB.SendCMD(Data.DARid, 0x81, 0x00);
            USB.SendData(Data.DARid, RSend);

            USB.SendCMD(Data.DARid, 0x81, 0x20);
            USB.SendCMD(Data.DARid, 0x81, 0x00);
            USB.SendData(Data.DARid, RSend2);
        }

        private void btn_RInit_Click(object sender, EventArgs e)
        {
            initRBoard();
        }

        private void dataGridView4_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                if (e.ColumnIndex == 3)//开始
                {
                    if (dataGridView4.Rows[e.RowIndex].Cells[3].FormattedValue.ToString() == "发送")
                    {
                        dataGridView4.Rows[e.RowIndex].Cells[3].Value = "停止";
                        //获得设置的输出长度
                        string outlenth = dataGridView4.Rows[e.RowIndex].Cells[1].FormattedValue.ToString();
                        int outlen = 1;
                        int.TryParse(outlenth.Substring(0, 1), out outlen);
                        if (outlen != 1 && outlen != 2 && outlen != 4)
                        {
                            MyLog.Error("数据长度设置必须在1或2或4字节");
                        }

                        //获得设置的输出内容
                        string Str_Content = (string)dataGridView4.Rows[e.RowIndex].Cells[2].Value;
                        if ((Str_Content.Length % 2) != 0)
                        {
                            MyLog.Error("请以16进制格式输入内容，如00代表0，ff代表255");
                            return;
                        }
                        if ((Str_Content.Length / 2) != outlen)
                        {
                            MyLog.Error("输入内容的长度与设置的数据长度不相等，请重新输入1");
                            return;
                        }
                        byte addr = (byte)(0xE1 + e.RowIndex);
                        string Header = (int.Parse("1D16", System.Globalization.NumberStyles.HexNumber) + e.RowIndex).ToString("x4");

                        UpdateSCROOM(addr, Str_Content, Header);

                    }
                    else
                    {
                        dataGridView4.Rows[e.RowIndex].Cells[3].Value = "发送";
                        byte addr = (byte)(0xE1 + e.RowIndex);
                        USB.SendCMD(Data.SCid, addr, 0x40);
                    }
                }


            }
        }



        private void radioButton7_Click(object sender, EventArgs e)
        {
            //radiobutton7 独立控制
            //radiobutton8 联合控制
            if (radioButton7.Checked)
            {
                panel_GNSS_alone.Enabled = true;
                panel_GNSS_both.Enabled = false;
            }
            else if (radioButton8.Checked)
            {
                panel_GNSS_alone.Enabled = false;
                panel_GNSS_both.Enabled = true;
            }
            else
            {
                MyLog.Error("没有选择秒脉冲控制模式！");
            }
        }

        private void btn_GNSS_A_Out_Click(object sender, EventArgs e)
        {
            if (btn_GNSS_A_Out.Text == "输出")
            {
                byte b1 = 0x0;
                string period = comboBox_GNSSA_period.Text;
                string width = comboBox_GNSSA_width.Text;

                switch (period)
                {
                    case "0.8":
                        b1 = (byte)(b1 | 0x0);
                        break;
                    case "0.9":
                        b1 = (byte)(b1 | 0x1);
                        break;
                    case "1":
                        b1 = (byte)(b1 | 0x2);
                        break;
                    case "1.1":
                        b1 = (byte)(b1 | 0x3);
                        break;
                    case "1.2":
                        b1 = (byte)(b1 | 0x4);
                        break;
                    default:
                        b1 = (byte)(b1 | 0x2);
                        break;
                }

                switch (width)
                {
                    case "0.8":
                        b1 = (byte)(b1 | 0x0);
                        break;
                    case "0.9":
                        b1 = (byte)(b1 | 0x8);
                        break;
                    case "1":
                        b1 = (byte)(b1 | 0x10);
                        break;
                    case "1.1":
                        b1 = (byte)(b1 | 0x18);
                        break;
                    case "1.2":
                        b1 = (byte)(b1 | 0x20);
                        break;
                    default:
                        b1 = (byte)(b1 | 0x10);
                        break;
                }
                USB.SendCMD(Data.SCid, 0xE5, 0x0);
                USB.SendCMD(Data.SCid, 0xE6, 0x0);
                USB.SendCMD(Data.SCid, 0xE3, b1);
                USB.SendCMD(Data.SCid, 0xE3, (byte)(b1 | 0x40));//Enable置1
                btn_GNSS_A_Out.Text = "停止";
                btn_GNSS_A_Out.BackColor = Color.Gray;
                comboBox_GNSSA_period.Enabled = false;
                comboBox_GNSSA_width.Enabled = false;
            }
            else
            {
                USB.SendCMD(Data.SCid, 0xE3, 0x0);
                btn_GNSS_A_Out.Text = "输出";
                btn_GNSS_A_Out.BackColor = Color.Aquamarine;
                comboBox_GNSSA_period.Enabled = true;
                comboBox_GNSSA_width.Enabled = true;
            }


        }

        private void btn_GNSS_B_Out_Click(object sender, EventArgs e)
        {
            if (btn_GNSS_B_Out.Text == "输出")
            {
                byte b1 = 0x00;
                string period = comboBox_GNSSB_period.Text;
                string width = comboBox_GNSSB_width.Text;

                switch (period)
                {
                    case "0.8":
                        b1 = (byte)(b1 | 0x0);
                        break;
                    case "0.9":
                        b1 = (byte)(b1 | 0x1);
                        break;
                    case "1":
                        b1 = (byte)(b1 | 0x2);
                        break;
                    case "1.1":
                        b1 = (byte)(b1 | 0x3);
                        break;
                    case "1.2":
                        b1 = (byte)(b1 | 0x4);
                        break;
                    default:
                        b1 = (byte)(b1 | 0x2);
                        break;
                }

                switch (width)
                {
                    case "0.8":
                        b1 = (byte)(b1 | 0x0);
                        break;
                    case "0.9":
                        b1 = (byte)(b1 | 0x8);
                        break;
                    case "1":
                        b1 = (byte)(b1 | 0x10);
                        break;
                    case "1.1":
                        b1 = (byte)(b1 | 0x18);
                        break;
                    case "1.2":
                        b1 = (byte)(b1 | 0x20);
                        break;
                    default:
                        b1 = (byte)(b1 | 0x10);
                        break;
                }
                USB.SendCMD(Data.SCid, 0xE5, 0x0);
                USB.SendCMD(Data.SCid, 0xE6, 0x0);
                USB.SendCMD(Data.SCid, 0xE4, b1);
                USB.SendCMD(Data.SCid, 0xE4, (byte)(b1 | 0x40));//Enable置1
                btn_GNSS_B_Out.Text = "停止";
                btn_GNSS_B_Out.BackColor = Color.Gray;
                comboBox_GNSSB_period.Enabled = false;
                comboBox_GNSSB_width.Enabled = false;
            }
            else
            {
                USB.SendCMD(Data.SCid, 0xE4, 0x0);
                btn_GNSS_B_Out.Text = "输出";
                btn_GNSS_B_Out.BackColor = Color.Aquamarine;
                comboBox_GNSSB_period.Enabled = true;
                comboBox_GNSSB_width.Enabled = true;
            }
        }

        private void btn_GNSS_AB_Out_Click(object sender, EventArgs e)
        {
            if (btn_GNSS_AB_Out.Text == "输出")
            {
                byte b1 = 0x0;

                string period = comboBox_GNSSAB_period.Text;
                string width = comboBox_GNSSAB_width.Text;

                switch (period)
                {
                    case "0.8":
                        b1 = (byte)(b1 | 0x0);
                        break;
                    case "0.9":
                        b1 = (byte)(b1 | 0x1);
                        break;
                    case "1":
                        b1 = (byte)(b1 | 0x2);
                        break;
                    case "1.1":
                        b1 = (byte)(b1 | 0x3);
                        break;
                    case "1.2":
                        b1 = (byte)(b1 | 0x4);
                        break;
                    default:
                        b1 = (byte)(b1 | 0x2);
                        break;
                }

                switch (width)
                {
                    case "0.8":
                        b1 = (byte)(b1 | 0x0);
                        break;
                    case "0.9":
                        b1 = (byte)(b1 | 0x8);
                        break;
                    case "1":
                        b1 = (byte)(b1 | 0x10);
                        break;
                    case "1.1":
                        b1 = (byte)(b1 | 0x18);
                        break;
                    case "1.2":
                        b1 = (byte)(b1 | 0x20);
                        break;
                    default:
                        b1 = (byte)(b1 | 0x10);
                        break;
                }

                double delay = 0;
                double.TryParse(textBox_GNSSAB_delay.Text, out delay);
                int tdelay = (int)(delay * 10);

                byte t1 = (byte)(tdelay & 0x3f);
                byte t2 = (byte)((tdelay & 0x1fC0) >> 6);

                USB.SendCMD(Data.SCid, 0xE5, t1);
                USB.SendCMD(Data.SCid, 0xE6, t2);
                USB.SendCMD(Data.SCid, 0xE5, (byte)(t1 | 0x40));

                USB.SendCMD(Data.SCid, 0xE4, b1);
                USB.SendCMD(Data.SCid, 0xE3, b1);
                USB.SendCMD(Data.SCid, 0xE3, (byte)(b1 | 0x40));//Enable置1

                comboBox_GNSSAB_period.Enabled = false;
                comboBox_GNSSAB_width.Enabled = false;
                textBox_GNSSAB_delay.Enabled = false;

                btn_GNSS_AB_Out.Text = "停止";
                btn_GNSS_AB_Out.BackColor = Color.Gray;
            }
            else
            {
                USB.SendCMD(Data.SCid, 0xE5, 0x0);
                //通道1一直有
                USB.SendCMD(Data.SCid, 0xE3, 0x0);
                USB.SendCMD(Data.SCid, 0xE4, 0x0);

                comboBox_GNSSAB_period.Enabled = true;
                comboBox_GNSSAB_width.Enabled = true;
                textBox_GNSSAB_delay.Enabled = true;
                btn_GNSS_AB_Out.Text = "输出";
                btn_GNSS_AB_Out.BackColor = Color.Aquamarine;
            }
        }

        private void btn_Log_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = Program.GetStartupPath() + @"LogData\";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Process Pnoted = new Process();
                try
                {
                    Pnoted.StartInfo.FileName = openFileDialog1.FileName;
                    Pnoted.Start();
                }
                catch
                {
                    MyLog.Error("日志未能成功打开！");
                }
            }
        }

        private void btn_YuanMa_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("Explorer", Program.GetStartupPath());
        }


        private void button4_Click(object sender, EventArgs e)
        {
            if (myRModifyForm != null)
            {
                myRModifyForm.Activate();
            }
            else
            {
                myRModifyForm = new RModifyForm(this);
            }
            myRModifyForm.ShowDialog();
        }


        private void dataGridView_R1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView dgv = (DataGridView)sender;
            string SenderName = dgv.Name;
            int LastNameValue = 0;

            int.TryParse(SenderName.Substring(14), out LastNameValue);     //SenderName = dataGridView_V4
            int t = LastNameValue - 1;

            if (e.RowIndex >= 0)
            {
                if (e.ColumnIndex == 2)
                {

                    double temp = (double)dtRList[t].Rows[e.RowIndex]["电阻"];
                    clcRValue(t, e.RowIndex, temp);


                    byte[] RSend = new byte[8 + 160 + 32];//帧头4B+160有效40个通道+32无效8通道+帧尾4B
                    RSend[0] = 0x1D;
                    RSend[1] = (byte)(0x24 + (t / 2));
                    RSend[2] = 0x00;
                    RSend[3] = 0x30;//0x0030 = 40
                                    //      for (int i = 0; i < 32; i++) RSend[164 + i] = 0x0;
                    RSend[196] = 0xC0;
                    RSend[197] = 0xDE;
                    RSend[198] = 0xC0;
                    RSend[199] = 0xDE;


                    switch (t)
                    {
                        case 0:
                            Array.Copy(RByteA, 0, RSend, 4, 80);
                            break;
                        case 1:
                            Array.Copy(RByteB, 0, RSend, 84, 80);
                            break;
                        case 2:
                            Array.Copy(RByteC, 0, RSend, 4, 80);
                            break;
                        case 3:
                            Array.Copy(RByteD, 0, RSend, 84, 80);
                            break;
                        default:
                            break;
                    }


                    if (t == 0 || t == 1)
                    {
                        USB.SendCMD(Data.DARid, 0x81, 0x10);
                        USB.SendCMD(Data.DARid, 0x81, 0x00);
                        USB.SendData(Data.DARid, RSend);
                    }
                    else
                    {
                        USB.SendCMD(Data.DARid, 0x81, 0x20);
                        USB.SendCMD(Data.DARid, 0x81, 0x00);
                        USB.SendData(Data.DARid, RSend);
                    }
                }


            }
        }

        private void btn_RLoad_Click(object sender, EventArgs e)
        {
            String Path = Program.GetStartupPath() + @"码本\电阻码本\";
            if (!Directory.Exists(Path))
                Directory.CreateDirectory(Path);

            openFileDialog_da.InitialDirectory = Path;

            if (openFileDialog_da.ShowDialog() == DialogResult.OK)
            {
                MyLog.Info("载入电阻码本成功！");

                string[] content = File.ReadAllLines(openFileDialog_da.FileName);
                string[] temp = new string[5];

                if (content.Length >= 96)
                {
                    for (int i = 0; i < 96; i++)
                    {
                        temp = content[i].Split(',');
                        if (i >= 0 && i < 24)
                        {
                            dtR1.Rows[i]["名称"] = temp[1].Trim();
                            dtR1.Rows[i]["电阻"] = double.Parse(temp[2].Trim());
                            //  dataGridView_V1.Rows[i].Cells[1].Value = double.Parse(temp[1].Trim());
                        }
                        else if (i >= 24 && i < 48)
                        {
                            dtR2.Rows[i - 24]["名称"] = temp[1].Trim();
                            dtR2.Rows[i - 24]["电阻"] = double.Parse(temp[2].Trim());
                            //    dataGridView_V2.Rows[i - 32].Cells[1].Value = double.Parse(temp[1].Trim());
                        }
                        else if (i >= 48 && i < 72)
                        {
                            dtR3.Rows[i - 48]["名称"] = temp[1].Trim();
                            dtR3.Rows[i - 48]["电阻"] = double.Parse(temp[2].Trim());
                            //                            dataGridView_V3.Rows[i - 64].Cells[1].Value = double.Parse(temp[1].Trim());
                        }
                        else if (i >= 72 && i < 96)
                        {
                            dtR4.Rows[i - 72]["名称"] = temp[1].Trim();
                            dtR4.Rows[i - 72]["电阻"] = double.Parse(temp[2].Trim());
                            //                            dataGridView_V4.Rows[i - 96].Cells[1].Value = double.Parse(temp[1].Trim());
                        }
                        else
                        {
                            //Nothing happens
                        }
                    }
                }
            }
        }

        private void btn_RSave_Click(object sender, EventArgs e)
        {
            String Path = Program.GetStartupPath() + @"码本\电阻码本\";
            if (!Directory.Exists(Path))
                Directory.CreateDirectory(Path);

            saveFileDialog_da.InitialDirectory = Path;

            saveFileDialog_da.Filter = "文本文件(*.txt)|*.txt|All files(*.*)|*.*";
            saveFileDialog_da.FilterIndex = 1;
            saveFileDialog_da.RestoreDirectory = true;

            if (saveFileDialog_da.ShowDialog() == DialogResult.OK)
            {
                String ModifyStr1 = "";
                String ModifyStr2 = "";
                String ModifyStr3 = "";
                String ModifyStr4 = "";
                for (int i = 0; i < 24; i++)
                {
                    ModifyStr1 += dtR1.Rows[i]["ID"] + "," + dtR1.Rows[i]["名称"] + "," + dtR1.Rows[i]["电阻"] + "\r\n";
                    ModifyStr2 += dtR2.Rows[i]["ID"] + "," + dtR2.Rows[i]["名称"] + "," + dtR2.Rows[i]["电阻"] + "\r\n";
                    ModifyStr3 += dtR3.Rows[i]["ID"] + "," + dtR3.Rows[i]["名称"] + "," + dtR3.Rows[i]["电阻"] + "\r\n";
                    ModifyStr4 += dtR4.Rows[i]["ID"] + "," + dtR4.Rows[i]["名称"] + "," + dtR4.Rows[i]["电阻"] + "\r\n";
                }

                string localFilePath = saveFileDialog_da.FileName.ToString(); //获得文件路径 

                FileStream file0 = new FileStream(localFilePath, FileMode.Create);
                StreamWriter sw = new StreamWriter(file0);
                sw.WriteLine(ModifyStr1 + ModifyStr2 + ModifyStr3 + ModifyStr4);
                sw.Flush();
                sw.Close();
                file0.Close();
                MessageBox.Show("存储文件成功！", "保存文件");
            }
        }

        private void numericUpDown4_ValueChanged(object sender, EventArgs e)
        {
            foreach (var dt in dtRList)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    dr["电阻"] = numericUpDown4.Value;
                }
            }
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            foreach (var dt in dtDAList)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    dr["电压"] = numericUpDown2.Value;
                }
            }


            //string str = numericUpDown2.Value.ToString();
            //double value = 0;
            //bool ret = double.TryParse(str, out value);
            //if (ret == false || value < 0 || value > 4.8)
            //{
            //    MessageBox.Show("Error:DA范围为0~4.8V，请正确输入", "Error");
            //}
            //else
            //{
            //    string ValueToSet = value.ToString("0.000");
            //    for (int j = 0; j < 4; j++)
            //    {
            //        for (int i = 0; i < 32; i++)
            //        {
            //            dtDAList[j].Rows[i]["电压"] = ValueToSet;
            //        }
            //    }
            //}
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            numericUpDown4.Increment = numericUpDown3.Value;
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            numericUpDown2.Increment = numericUpDown1.Value;
        }

        private void DebugBtn_Click(object sender, EventArgs e)
        {
            if (this.splitContainer2.Panel2Collapsed)
            {
                this.splitContainer2.Panel2Collapsed = false;
            }
            else
            {
                this.splitContainer2.Panel2Collapsed = true;
            }
        }

        private void dataGridView_R1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {

            if (e.Exception != null)
            {

                MessageBox.Show("请输入正确的电阻值，输入无效数值将自动设置为最大电阻256K");

                DataGridView dgv = (DataGridView)sender;
                string SenderName = dgv.Name;
                int LastNameValue = 0;

                int.TryParse(SenderName.Substring(14), out LastNameValue);     //SenderName = dataGridView_V4

                if (e.RowIndex >= 0)
                {
                    if (e.ColumnIndex == 2)
                    {
                        dtRList[LastNameValue - 1].Rows[e.RowIndex]["电阻"] = 256;
                    }
                }
            }
        }

        private void dataGridView_V1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            if (e.Exception != null)
            {
                MessageBox.Show("请输入正确的电压值，输入无效数值将自动设置为最大电压4.8V");

                DataGridView dgv = (DataGridView)sender;
                string SenderName = dgv.Name;
                int LastNameValue = 0;
                int.TryParse(SenderName.Substring(14), out LastNameValue);     //SenderName = dataGridView_V4

                if (e.RowIndex >= 0)
                {
                    if (e.ColumnIndex == 2)
                    {
                        dtDAList[LastNameValue - 1].Rows[e.RowIndex]["电压"] = 4.8;

                    }
                }
            }
        }

        private void btn_ClrOC_Click(object sender, EventArgs e)
        {
            foreach (var dt in dtOCList)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    dr["计数"] = 0;
                    dr["脉宽"] = 0;
                }
            }
        }

        private void dataGridView7_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                string temp = dataGridView7.Rows[e.RowIndex].Cells[1].FormattedValue.ToString();
                bool ret = false;
                int value = 0;
                ret = int.TryParse(temp, out value);
                if (ret)
                {
                    if (value <= 0 || value > 2000000)
                    {
                        dataGridView7.Rows[e.RowIndex].Cells[1].Value = "80000";
                        MessageBox.Show("脉宽必须设置在0-2000000us之间");
                    }
                }
                else
                {
                    dataGridView7.Rows[e.RowIndex].Cells[1].Value = "80000";
                    MessageBox.Show("输入有效的脉宽值");
                }
            }
        }

        private void btn_RSet_Click(object sender, EventArgs e)
        {
            SetRBoard(0x08, 0x00);
            Thread.Sleep(5000);
            SetRBoard(0x0C, 0x00);
        }

        private void btn_MdfyEnable_Click(object sender, EventArgs e)
        {
            if (btn_MdfyEnable.Text == "参数校准-开启")
            {
                btn_MdfyEnable.Text = "参数校准-关闭";
                btn_modify.Visible = true;
                button4.Visible = true;
                //btn_RInit.Visible = true;
                //btn_RSet.Visible = true;

            }
            else
            {
                btn_MdfyEnable.Text = "参数校准-开启";
                btn_modify.Visible = false;
                button4.Visible = false;
                btn_RInit.Visible = false;
                btn_RSet.Visible = false;
            }
        }

        private void btn_ClearLog_Click(object sender, EventArgs e)
        {
            this.richTextBox1.Clear();
        }

        private void dataGridView_sc1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                if (e.ColumnIndex == 0)//为何是0？前面2个被datatable绑定了，这个就变成0了
                {
                    dtSCVcid1.Rows[e.RowIndex][1] = 0;

                    for (int i = 0; i < VcidList1.Count; i++)
                    {
                        if (VcidList1[i].Name == (string)dtSCVcid1.Rows[e.RowIndex][0])
                        {
                            VCID_STRUCT temp = new VCID_STRUCT();
                            temp.Name = VcidList1[i].Name;
                            temp.RecvNums = 0;
                            VcidList1.Remove(VcidList1[i]);
                            VcidList1.Add(temp);
                            break;
                        }
                    }
                }
            }
        }

        private void dataGridView_sc2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                if (e.ColumnIndex == 0)//为何是0？前面2个被datatable绑定了，这个就变成0了
                {
                    dtSCVcid2.Rows[e.RowIndex][1] = 0;

                    for (int i = 0; i < VcidList2.Count; i++)
                    {
                        if (VcidList2[i].Name == (string)dtSCVcid2.Rows[e.RowIndex][0])
                        {
                            VCID_STRUCT temp = new VCID_STRUCT();
                            temp.Name = VcidList2[i].Name;
                            temp.RecvNums = 0;
                            VcidList2.Remove(VcidList2[i]);
                            VcidList2.Add(temp);
                            break;
                        }
                    }
                }
            }
        }
    }
}
