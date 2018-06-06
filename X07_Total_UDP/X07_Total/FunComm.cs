using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;//加载C++下的动态链接库
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace H07_Total
{
    public class FunComm
    {
        public struct COMMDATA
        {
            public int Port;
            public int BaudRate, Parity, ByteSize, StopBits;
            //  public int ibaudrate, iparity, ibytesize, istopbits;
            //  public bool Hw;		/* RTS/CTS hardware flow control */
            //  public bool Sw;		/* XON/XOFF software flow control */
            // public bool Dtr, Rts;
            public string strPort, strBaudRate, strParity, strByteSize, strStopBits;
            public bool GbOpen;
            //public bool loading;//是否正在加载 
            public int sta_loading;//0x01加载完成 0x02加载失败
           // public bool running;//是否正在运行
            public int sta_running;//运行是否正常 0x01 正常 0x02不正常
            public int COMportpage;//串口对应的窗口号 
            public string filename;

        }
        public static string config_file_name;
        public static string config_Path;
        public static COMMDATA[] myCOMMDATA = new COMMDATA[9];
        public static int[] COMportpage = new int[9];//窗口对应的串口号 

        public static COMMDATA myCOMMDATA_Test;

        [DllImport("PCOMM.DLL", EntryPoint = "sio_open", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int sio_open(int port);
        [DllImport("PCOMM.DLL", EntryPoint = "sio_ioctl", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int sio_ioctl(int port, int baud, int mode);
        [DllImport("PCOMM.DLL", EntryPoint = "sio_close", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int sio_close(int port);
        [DllImport("PCOMM.DLL", EntryPoint = "sio_read", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int sio_read(int port, byte[] buf, int len);
        [DllImport("PCOMM.DLL", EntryPoint = "sio_write", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int sio_write(int port, byte[] buf, int len);
        [DllImport("PCOMM.DLL", EntryPoint = "sio_iqueue", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int sio_iqueque(int port);
        [DllImport("PCOMM.DLL", EntryPoint = "sio_oqueue", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int sio_oqueque(int port);
        [DllImport("PCOMM.DLL", EntryPoint = "sio_SetReadTimeouts", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int sio_SetReadTimeouts(int port, Int16 totaltimeouts, Int16 intervaltimeouts);
        [DllImport("PCOMM.DLL", EntryPoint = "sio_getch", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int sio_getch(int port);


        public const int B50 = 0x00;
        public const int B75 = 0x01;
        public const int B110 = 0x02;
        public const int B134 = 0x03;
        public const int B150 = 0x04;
        public const int B300 = 0x05;
        public const int B600 = 0x06;
        public const int B1200 = 0x07;
        public const int B1800 = 0x08;
        public const int B2400 = 0x09;
        public const int B4800 = 0x0A;
        public const int B7200 = 0x0B;
        public const int B9600 = 0x0C;
        public const int B19200 = 0x0D;
        public const int B38400 = 0x0E;
        public const int B57600 = 0x0F;
        public const int B115200 = 0x10;
        public const int B230400 = 0x11;
        public const int B460800 = 0x12;
        public const int B921600 = 0x13;

        ///*	MODE setting		*/
        public const int BIT_5 = 0x00;		/* Word length define	*/
        public const int BIT_6 = 0x01;
        public const int BIT_7 = 0x02;
        public const int BIT_8 = 0x03;

        public const int STOP_1 = 0x00;      /* Stop bits define	*/
        public const int STOP_2 = 0x04;
        public const int P_EVEN = 0x18;      /* Parity define	*/
        public const int P_ODD = 0x08;
        public const int P_SPC = 0x38;
        public const int P_MRK = 0x28;
        public const int P_NONE = 0x00;

        ///*	MODEM CONTROL setting	*/
        public const int C_DTR = 0x01;
        public const int C_RTS = 0x02;

        ///*	MODEM LINE STATUS	*/
        public const int S_CTS = 0x01;
        public const int S_DSR = 0x02;
        public const int S_RI = 0x04;
        public const int S_CD = 0x08;

        ///* error code */
        public const int SIO_OK = 0;
        public const int SIO_BADPORT = -1;/* no such port or port not opened */
        public const int SIO_OUTCONTROL = -2;/* can't control the board */
        public const int SIO_NODATA = -4;/* no data to read or no buffer to write */
        public const int SIO_OPENFAIL = -5;/* no such port or port has be opened */
        public const int SIO_RTS_BY_HW = -6;  /* RTS can't set because H/W flowctrl */
        public const int SIO_BADPARM = -7;/* bad parameter */
        public const int SIO_WIN32FAIL = -8;/* call win32 function fail, please call */
        /* GetLastError to get the error code */
        public const int SIO_BOARDNOTSUPPORT = -9;/* Does not support this board */
        public const int SIO_FAIL = -10;/* PComm function run result fail */
        public const int SIO_ABORTWRITE = -11;/* write has blocked, and user abort write */
        public const int SIO_WRITETIMEOUT = -12;/* write timeoue has happened */
        //* file transfer error code */
        public const int SIOFT_OK = 0;
        public const int SIOFT_BADPORT = -1;/* no such port or port not open */
        public const int SIOFT_TIMEOUT = -2;/* protocol timeout */
        public const int SIOFT_ABORT = -3;/* user key abort */
        public const int SIOFT_FUNC = -4;/* func return abort */
        public const int SIOFT_FOPEN = -5;/* can not open files */
        public const int SIOFT_CANABORT = -6;/* Ymodem CAN signal abort */
        public const int SIOFT_PROTOCOL = -7;/* Protocol checking error abort */
        public const int SIOFT_SKIP = -8;/* Zmodem remote skip this send file */
        public const int SIOFT_LACKRBUF = -9;/* Zmodem Recv-Buff size must >= 2K bytes */
        public const int SIOFT_WIN32FAIL = -10;/* OS fail */
        /* GetLastError to get the error code */
        public const int SIOFT_BOARDNOTSUPPORT = -11;   /* Does not support board */


        //串口错误标志
        public static string MxShowError(int errcode)
        {
            string str;
            if (errcode != FunComm.SIOFT_WIN32FAIL)
            {
                switch (errcode)
                {
                    case FunComm.SIO_BADPORT:
                        str = "Port number is invalid or port is not opened in advance";
                        break;
                    case FunComm.SIO_OUTCONTROL:
                        str = "The board does not support this function";
                        break;
                    case FunComm.SIO_NODATA:
                        str = "No data to read";
                        break;
                    case FunComm.SIO_OPENFAIL:
                        str = "No such port or port is occupied by other program";
                        break;
                    case FunComm.SIO_RTS_BY_HW:
                        str = "RTS can't be set because RTS/CTS Flowctrl";
                        break;
                    case FunComm.SIO_BADPARM:
                        str = "Bad parameter";
                        break;
                    case FunComm.SIO_BOARDNOTSUPPORT:
                        str = "The board does not support this function";
                        break;
                    case FunComm.SIO_ABORTWRITE:
                        str = "Write has blocked, and user abort write";
                        break;
                    case FunComm.SIO_WRITETIMEOUT:
                        str = "Write timeout has happened";
                        break;
                    default:
                        str = "Unknown Error";
                        break;
                }
            }
            else
            { str = "Unknown Error"; }

            return str;

        }


        //获取当前执行文件路径
        public static string GetStartupPath()
        {
            string path;
            path = System.Windows.Forms.Application.StartupPath;
            //Application.StartupPath;
            if (!path.EndsWith("\\"))
            {
                path += "\\";
            }
            return path;
        }

        //解析数据 将字符串转化为十六进制数据数组
        public static byte[] DataParse(string data_str)
        {
            byte[] err = new byte[0];
            data_str = data_str.Trim();   //去除末尾回车和空格
            if (data_str == "")
                return err;

            string[] byte_str = data_str.Split(" ".ToCharArray(), System.StringSplitOptions.RemoveEmptyEntries);  //以空格划分
            if (byte_str.Length == 1)  //无空格模式
            {
                if (byte_str[0].Length % 2 == 0)
                {
                    try
                    {
                        byte[] data = new byte[byte_str[0].Length / 2];
                        for (int i = 0; i < byte_str[0].Length / 2; i++)
                            data[i] = byte.Parse(data_str.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
                        return data;
                    }
                    catch
                    {
                        return err;
                    }
                }
                else
                {
                    return err;
                }
            }
            else  //空格划分模式
            {
                try
                {
                    byte[] data = new byte[byte_str.Length];
                    for (int i = 0; i < byte_str.Length; i++)
                        data[i] = byte.Parse(byte_str[i], System.Globalization.NumberStyles.HexNumber);
                    return data;
                }
                catch
                {
                    return err;
                }
            }
        }

        public static void Intconfig()
        {
            config_Path = FunComm.GetStartupPath() + "数据文件\\";
            config_file_name = "config.txt";
            if (!Directory.Exists(config_Path))  //不存在文件夹则创建
            {
                Directory.CreateDirectory(config_Path);
                FileStream configfile = new FileStream(config_Path + config_file_name, FileMode.Create);
                configfile.Close();
                //初始化config信息        
                string config_str = "串口号" + "," + "波特率" + "," + "数据位" + "," + "校验位" + "," + "停止位" + "," + "窗口号" + "," + "文件路径" + "\r\n";
                File.AppendAllText(config_Path + config_file_name, config_str);
                string config_str1 = "COM1" + "," + "B9600" + "," + "BIT_8" + "," + "None" + "," + "Stop_1" + "," + "0" + "," + "" + "\r\n";
                File.AppendAllText(config_Path + config_file_name, config_str1);
                string config_str2 = "COM2" + "," + "B9600" + "," + "BIT_8" + "," + "None" + "," + "Stop_1" + "," + "0" + "," + "" + "\r\n";
                File.AppendAllText(config_Path + config_file_name, config_str2);
                string config_str3 = "COM3" + "," + "B9600" + "," + "BIT_8" + "," + "None" + "," + "Stop_1" + "," + "0" + "," + "" + "\r\n";
                File.AppendAllText(config_Path + config_file_name, config_str3);
                string config_str4 = "COM4" + "," + "B9600" + "," + "BIT_8" + "," + "None" + "," + "Stop_1" + "," + "0" + "," + "" + "\r\n";
                File.AppendAllText(config_Path + config_file_name, config_str4);
                string config_str5 = "COM5" + "," + "B9600" + "," + "BIT_8" + "," + "None" + "," + "Stop_1" + "," + "0" + "," + "" + "\r\n";
                File.AppendAllText(config_Path + config_file_name, config_str5);
                string config_str6 = "COM6" + "," + "B9600" + "," + "BIT_8" + "," + "None" + "," + "Stop_1" + "," + "0" + "," + "" + "\r\n";
                File.AppendAllText(config_Path + config_file_name, config_str6);
                string config_str7 = "COM7" + "," + "B9600" + "," + "BIT_8" + "," + "None" + "," + "Stop_1" + "," + "0" + "," + "" + "\r\n";
                File.AppendAllText(config_Path + config_file_name, config_str7);
                string config_str8 = "COM8" + "," + "B9600" + "," + "BIT_8" + "," + "None" + "," + "Stop_1" + "," + "0" + "," + "" + "\r\n";
                File.AppendAllText(config_Path + config_file_name, config_str8);


                string config_str9 = "10.126.1.192" + "," + "9000" + "," + "qdsb422" + "," + "qdsb422" + "," + "127.0.0.1" + "," + "0x84" + "," + "0x64" + "\r\n";
                File.AppendAllText(config_Path + config_file_name, config_str9);  
            }
        
        }

        //读取初始化信息
        public static void IntCOM()
        {
           
            config_Path = FunComm.GetStartupPath() + "数据文件\\";
            config_file_name = "config.txt";
            if (Directory.Exists(config_Path))  //存在文件夹则初始化串口
            {
                string[] config = File.ReadAllLines(FunComm.config_Path + FunComm.config_file_name);
                for (int i = 1; i < config.Length-1; i++)
                {                   
                    string[] config_i = config[i].Split(',');
                    
                    if (config_i.Length > 0)//有内容
                    {
                       Readconfig(config_i,i);                    
                    }
                    //if (config_i.Length ==1)//有内容
                    //{
                    //    Readcom(config_i,i);                       
                    //}
                }
                string[] config_tcp = config[config.Length - 1].Split(',');
                Readtcp(config_tcp, config.Length - 1);
            }
           
        }


        public static void Readtcp(string[] str1, int num)
        {
            FunComm.myTCP.ip = str1[0];
            FunComm.myTCP.port = str1[1];
            FunComm.myTCP.user = str1[2];
            FunComm.myTCP.password = str1[3];
            FunComm.myTCP.terminal = str1[4];
            FunComm.myTCP.mark = Convert.ToByte( str1[5], 16);
            FunComm.myTCP.equ = Convert.ToByte( str1[6], 16);
            FunComm.myTCP.strmark =str1[5];
            FunComm.myTCP.strequ =str1[6];
        }


        //读取初始化配置信息
        public static void Readconfig(string[] str1, int num)
        {
            int COMport = 0;
            string[] str = str1;
            if (str[0] == "COM1")
            {
                COMport = 1;
                FunComm.myCOMMDATA[num].Port = 1;
                FunComm.myCOMMDATA[num].strPort = "COM1";
            }
            if (str[0] == "COM2")
            {
                COMport = 2;
                FunComm.myCOMMDATA[num].Port = 2;
                FunComm.myCOMMDATA[num].strPort = "COM2";

            }
            if (str[0] == "COM3")
            {
                COMport = 3;
                FunComm.myCOMMDATA[num].Port = 3;
                FunComm.myCOMMDATA[num].strPort = "COM3";

            }
            if (str[0] == "COM4")
            {
                COMport = 4; FunComm.myCOMMDATA[num].Port = 4;
                FunComm.myCOMMDATA[num].strPort = "COM4";

            }
            if (str[0] == "COM5")
            {
                COMport = 5; FunComm.myCOMMDATA[num].Port = 5;
                FunComm.myCOMMDATA[num].strPort = "COM5";

            }
            if (str[0] == "COM6")
            {
                COMport = 6; FunComm.myCOMMDATA[num].Port = 6;
                FunComm.myCOMMDATA[num].strPort = "COM6";

            }
            if (str[0] == "COM7")
            {
                COMport = 7; FunComm.myCOMMDATA[num].Port = 7;
                FunComm.myCOMMDATA[num].strPort = "COM7";

            }
            if (str[0] == "COM8")
            {
                COMport = 8;
                FunComm.myCOMMDATA[num].Port = 8;
                FunComm.myCOMMDATA[num].strPort = "COM8";

            }
            if (str[0] == "COM9")
            {
                COMport = 9;
                FunComm.myCOMMDATA[num].Port = 9;
                FunComm.myCOMMDATA[num].strPort = "COM9";
            }
            if (str[0] == "COM10")
            {
                COMport = 10;
                FunComm.myCOMMDATA[num].Port = 10;
                FunComm.myCOMMDATA[num].strPort = "COM10";

            }
            if (str[0] == "COM11")
            {
                COMport = 11;
                FunComm.myCOMMDATA[num].Port = 11;
                FunComm.myCOMMDATA[num].strPort = "COM11";

            }
            if (str[0] == "COM12")
            {
                COMport = 12; FunComm.myCOMMDATA[num].Port = 12;
                FunComm.myCOMMDATA[num].strPort = "COM12";

            }
            if (str[0] == "COM13")
            {
                COMport = 13; FunComm.myCOMMDATA[num].Port = 13;
                FunComm.myCOMMDATA[num].strPort = "COM13";

            }
            if (str[0] == "COM14")
            {
                COMport = 14; FunComm.myCOMMDATA[num].Port = 14;
                FunComm.myCOMMDATA[num].strPort = "COM14";

            }
            if (str[0] == "COM15")
            {
                COMport = 15; FunComm.myCOMMDATA[num].Port = 15;
                FunComm.myCOMMDATA[num].strPort = "COM15";

            }
            if (str[0] == "COM16")
            {
                COMport = 16;
                FunComm.myCOMMDATA[num].Port = 16;
                FunComm.myCOMMDATA[num].strPort = "COM16";

            }

            if (str[0] == "COM17")
            {
                COMport = 17; FunComm.myCOMMDATA[num].Port = 17;
                FunComm.myCOMMDATA[num].strPort = "COM17";

            }
            if (str[0] == "COM18")
            {
                COMport = 18; FunComm.myCOMMDATA[num].Port = 18;
                FunComm.myCOMMDATA[num].strPort = "COM18";

            }
            if (str[0] == "COM19")
            {
                COMport = 19; FunComm.myCOMMDATA[num].Port = 19;
                FunComm.myCOMMDATA[num].strPort = "COM19";

            }
            if (str[0] == "COM20")
            {
                COMport = 20;
                FunComm.myCOMMDATA[num].Port = 20;
                FunComm.myCOMMDATA[num].strPort = "COM20";

            }
            ///////////////////////////////////

            if (str[1] == "B1800")
            {
                FunComm.myCOMMDATA[num].BaudRate = FunComm.B1800;
                FunComm.myCOMMDATA[num].strBaudRate = "B1800";
            }
            if (str[1] == "B75")
            {
                FunComm.myCOMMDATA[num].BaudRate = FunComm.B75;
                FunComm.myCOMMDATA[num].strBaudRate = "B75";
            }
            if (str[1] == "B2400")
            {
                FunComm.myCOMMDATA[num].BaudRate = FunComm.B2400;
                FunComm.myCOMMDATA[num].strBaudRate = "B2400";
            }

            if (str[1] == "B4800")
            {
                FunComm.myCOMMDATA[num].BaudRate = FunComm.B4800;
                FunComm.myCOMMDATA[num].strBaudRate = "B4800";
            }
            if (str[1] == "B9600")
            {
                FunComm.myCOMMDATA[num].BaudRate = FunComm.B9600;
                FunComm.myCOMMDATA[num].strBaudRate = "B9600";
            }
            if (str[1] == "B19200")
            {
                FunComm.myCOMMDATA[num].BaudRate = FunComm.B19200;
                FunComm.myCOMMDATA[num].strBaudRate = "B19200";
            }
            if (str[1] == "B38400")
            {
                FunComm.myCOMMDATA[num].BaudRate = FunComm.B38400;
                FunComm.myCOMMDATA[num].strBaudRate = "B38400";
            }
            if (str[1] == "B57600")
            {
                FunComm.myCOMMDATA[num].BaudRate = FunComm.B57600;
                FunComm.myCOMMDATA[num].strBaudRate = "B57600";
            }
            if (str[1] == "B115200")
            {
                FunComm.myCOMMDATA[num].BaudRate = FunComm.B115200;
                FunComm.myCOMMDATA[num].strBaudRate = "B115200";
            }
            if (str[1] == "B230400")
            {
                FunComm.myCOMMDATA[num].BaudRate = FunComm.B230400;
                FunComm.myCOMMDATA[num].strBaudRate = "B230400";
            }
            if (str[1] == "B460800")
            {
                FunComm.myCOMMDATA[num].BaudRate = FunComm.B460800;
                FunComm.myCOMMDATA[num].strBaudRate = "B460800";
            }
            if (str[1] == "B921600")
            {
                FunComm.myCOMMDATA[num].BaudRate = FunComm.B921600;
                FunComm.myCOMMDATA[num].strBaudRate = "B921600";
            }
            /////////////////////////
            if (str[2] == "BIT_5")
            {
                FunComm.myCOMMDATA[num].ByteSize = FunComm.BIT_5;
                FunComm.myCOMMDATA[num].strByteSize = "BIT_5";
            }
            if (str[2] == "BIT_6")
            {
                FunComm.myCOMMDATA[num].ByteSize = FunComm.BIT_6;
                FunComm.myCOMMDATA[num].strByteSize = "BIT_6";
            }
            if (str[2] == "BIT_7")
            {
                FunComm.myCOMMDATA[num].ByteSize = FunComm.BIT_7;
                FunComm.myCOMMDATA[num].strByteSize = "BIT_7";
            }
            if (str[2] == "BIT_8")
            {
                FunComm.myCOMMDATA[num].ByteSize = FunComm.BIT_8;
                FunComm.myCOMMDATA[num].strByteSize = "BIT_8";
            }
            ////////////////////////////
            if (str[3] == "None")
            {
                FunComm.myCOMMDATA[num].Parity = FunComm.P_NONE;
                FunComm.myCOMMDATA[num].strParity = "None";
            }

            if (str[3] == "Even")
            {
                FunComm.myCOMMDATA[num].Parity = FunComm.P_EVEN;
                FunComm.myCOMMDATA[num].strParity = "Even";
            }
            if (str[3] == "Odd")
            {
                FunComm.myCOMMDATA[num].Parity = FunComm.P_ODD;
                FunComm.myCOMMDATA[num].strParity = "Odd";
            }
            if (str[3] == "Mark")
            {
                FunComm.myCOMMDATA[num].Parity = FunComm.P_MRK;
                FunComm.myCOMMDATA[num].strParity = "Mark";
            }
            if (str[3] == "Space")
            {
                FunComm.myCOMMDATA[num].Parity = FunComm.P_SPC;
                FunComm.myCOMMDATA[num].strParity = "Space";
            }
            /////////////////////////////////
            if (str[4] == "Stop_1")
            {
                FunComm.myCOMMDATA[num].StopBits = FunComm.STOP_1;
                FunComm.myCOMMDATA[num].strStopBits = "Stop_1";
            }
            if (str[4] == "Stop_2")
            {
                FunComm.myCOMMDATA[num].StopBits = FunComm.STOP_2;
                FunComm.myCOMMDATA[num].strStopBits = "Stop_2";
            }
            //////////////////////////////////
            if (str[5] == "1")
            {
                FunComm.myCOMMDATA[num].COMportpage = 1;
                
            }
            if (str[5] == "2")
            {
                FunComm.myCOMMDATA[num].COMportpage = 2;
            }
            if (str[5] == "3")
            {
                FunComm.myCOMMDATA[num].COMportpage = 3;
            }
            if (str[5] == "4")
            {
                FunComm.myCOMMDATA[num].COMportpage = 4;
            }
            if (str[5] == "5")
            {
                FunComm.myCOMMDATA[num].COMportpage = 5;

            }
            if (str[5] == "6")
            {
                FunComm.myCOMMDATA[num].COMportpage =6;
            }
            if (str[5] == "7")
            {
                FunComm.myCOMMDATA[num].COMportpage = 7;
            }
            if (str[5] == "8")
            {
                FunComm.myCOMMDATA[num].COMportpage = 8;
            }

            FunComm.myCOMMDATA[num].filename = str[6];
            

        }
        
      
         public static string SetPort(int num)//设置NUM行串口
        {
            ///////配置///
            int port = FunComm.myCOMMDATA_Test.Port;
            int mode = FunComm.myCOMMDATA_Test.ByteSize | FunComm.myCOMMDATA_Test.Parity | FunComm.myCOMMDATA_Test.StopBits;
            // int hw = FunComm.myCOMMDATA[COMport].Hw ? 3 : 0;	/* bit0 and bit1 */
            // int sw = FunComm.myCOMMDATA[COMport].Sw ? 12 : 0;     /* bit2 and bit3 */
            int ret;
            //DWORD tout;
            string str = null;
            //打开某个串口
            if ((ret = FunComm.sio_open(port)) != FunComm.SIO_OK)
            {
                str = FunComm.MxShowError(ret);
                // MessageBox.Show(str, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return str;
            }
            //设置串口参数
            if ((ret = FunComm.sio_ioctl(port, FunComm.myCOMMDATA_Test.BaudRate, mode)) != FunComm.SIO_OK)
            {  //设置         
                str = FunComm.MxShowError(ret);
                //  MessageBox.Show(str, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return str;
            }
            if (str == null)
            {
                FunComm.myCOMMDATA_Test.GbOpen = true;//串口请求成功

            }
            else
            {
                FunComm.myCOMMDATA_Test.GbOpen = false;//串口开启失败或者被关闭
            }
            return str;



            /////////设置 Port ///
            //int port = FunComm.myCOMMDATA[COMport].Port;
            //int mode = FunComm.myCOMMDATA[COMport].ByteSize | FunComm.myCOMMDATA[COMport].Parity | FunComm.myCOMMDATA[COMport].StopBits;
            //int hw = FunComm.myCOMMDATA[COMport].Hw ? 3 : 0;	/* bit0 and bit1 */
            //int sw = FunComm.myCOMMDATA[COMport].Sw ? 12 : 0;     /* bit2 and bit3 */
            //int ret;
            ////DWORD tout;
            //string str;
            ////打开某个串口
            //if ((ret = FunComm.sio_open(port)) != FunComm.SIO_OK)
            //{
            //    str = FunComm.MxShowError(ret);
            //    MessageBox.Show(str, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    return;
            //}
            ////设置串口参数
            //if ((ret = FunComm.sio_ioctl(port, FunComm.myCOMMDATA[COMport].BaudRate, mode)) != FunComm.SIO_OK)
            //{  //设置         
            //    str = FunComm.MxShowError(ret);
            //    MessageBox.Show(str, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    return;
            //}

            //if ((ret = FunComm.sio_flowctrl(port, hw | sw)) != FunComm.SIO_OK)
            //{//流控制 
            //    str = FunComm.MxShowError(ret);
            //    MessageBox.Show(str, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    return;
            //}

            //if ((ret = FunComm.sio_DTR(port, (FunComm.myCOMMDATA[COM].Dtr ? 1 : 0))) != FunComm.SIO_OK)
            //{//DTR控制
            //    str =FunComm. MxShowError(ret);
            //  MessageBox.Show(str, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    return;
            //}

            //if (!FunComm.myCOMMDATA[COM].Hw)
            //{
            //    if ((ret = FunComm.sio_RTS(port, (FunComm.myCOMMDATA[COM].Rts ? 1 : 0))) !=FunComm. SIO_OK)
            //    {//Hw控制
            //        str = FunComm.MxShowError(ret);
            //       MessageBox.Show(str, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //        return;
            //    }
            //}

            //     tout = 1000 / sio_getbaud(port);  /* ms/byte */
            //     if (tout < 1)
            //         tout = 1;
            //     tout = tout * 1 * 3;             /* 1 byte; '*3' is for delay */
            //     if(tout<100)
            //         tout = 100;
            //sio_SetWriteTimeouts(port, tout);

            //ShowStatus();         

            //return true;




        }

        
        public static string SetPort_old(int num)//设置NUM行串口
        {
            ///////配置///
            int port = FunComm.myCOMMDATA[num].Port;
            int mode = FunComm.myCOMMDATA[num].ByteSize | FunComm.myCOMMDATA[num].Parity | FunComm.myCOMMDATA[num].StopBits;
            // int hw = FunComm.myCOMMDATA[COMport].Hw ? 3 : 0;	/* bit0 and bit1 */
            // int sw = FunComm.myCOMMDATA[COMport].Sw ? 12 : 0;     /* bit2 and bit3 */
            int ret;
            //DWORD tout;
            string str = null;
            //打开某个串口
            if ((ret = FunComm.sio_open(port)) != FunComm.SIO_OK)
            {
                str = FunComm.MxShowError(ret);
                // MessageBox.Show(str, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return str;
            }
            //设置串口参数
            if ((ret = FunComm.sio_ioctl(port, FunComm.myCOMMDATA[num].BaudRate, mode)) != FunComm.SIO_OK)
            {  //设置         
                str = FunComm.MxShowError(ret);
                //  MessageBox.Show(str, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return str;
            }
            if (str == null)
            {
                FunComm.myCOMMDATA[num].GbOpen = true;//串口请求成功

            }
            else
            {
                FunComm.myCOMMDATA[num].GbOpen = false;//串口开启失败或者被关闭
            }
            return str;



            /////////设置 Port ///
            //int port = FunComm.myCOMMDATA[COMport].Port;
            //int mode = FunComm.myCOMMDATA[COMport].ByteSize | FunComm.myCOMMDATA[COMport].Parity | FunComm.myCOMMDATA[COMport].StopBits;
            //int hw = FunComm.myCOMMDATA[COMport].Hw ? 3 : 0;	/* bit0 and bit1 */
            //int sw = FunComm.myCOMMDATA[COMport].Sw ? 12 : 0;     /* bit2 and bit3 */
            //int ret;
            ////DWORD tout;
            //string str;
            ////打开某个串口
            //if ((ret = FunComm.sio_open(port)) != FunComm.SIO_OK)
            //{
            //    str = FunComm.MxShowError(ret);
            //    MessageBox.Show(str, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    return;
            //}
            ////设置串口参数
            //if ((ret = FunComm.sio_ioctl(port, FunComm.myCOMMDATA[COMport].BaudRate, mode)) != FunComm.SIO_OK)
            //{  //设置         
            //    str = FunComm.MxShowError(ret);
            //    MessageBox.Show(str, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    return;
            //}

            //if ((ret = FunComm.sio_flowctrl(port, hw | sw)) != FunComm.SIO_OK)
            //{//流控制 
            //    str = FunComm.MxShowError(ret);
            //    MessageBox.Show(str, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    return;
            //}

            //if ((ret = FunComm.sio_DTR(port, (FunComm.myCOMMDATA[COM].Dtr ? 1 : 0))) != FunComm.SIO_OK)
            //{//DTR控制
            //    str =FunComm. MxShowError(ret);
            //  MessageBox.Show(str, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    return;
            //}

            //if (!FunComm.myCOMMDATA[COM].Hw)
            //{
            //    if ((ret = FunComm.sio_RTS(port, (FunComm.myCOMMDATA[COM].Rts ? 1 : 0))) !=FunComm. SIO_OK)
            //    {//Hw控制
            //        str = FunComm.MxShowError(ret);
            //       MessageBox.Show(str, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //        return;
            //    }
            //}

            //     tout = 1000 / sio_getbaud(port);  /* ms/byte */
            //     if (tout < 1)
            //         tout = 1;
            //     tout = tout * 1 * 3;             /* 1 byte; '*3' is for delay */
            //     if(tout<100)
            //         tout = 100;
            //sio_SetWriteTimeouts(port, tout);

            //ShowStatus();         

            //return true;




        }

        //////...............TCP.................../////////

        public  struct TCP_IP
        {
            public string ip, port, user, password, terminal;
            public byte mark,equ;
            public string strmark, strequ;
        }
        public static TCP_IP myTCP;
        public static UInt32 serial_num = 1;
        public static Socket sck;
        public static IPEndPoint rep;
        public static bool TCP = false;
        public static bool TCP_recv = false;
        public static bool remote_loading = false;
        public static bool auto_loading = false;
        public static EventWaitHandle m_event;
        
     
        private static void CallBackMethod(IAsyncResult asyncresult)
        {
            TimeoutObjiect.Set();
        }

        private static readonly ManualResetEvent TimeoutObjiect = new ManualResetEvent(false);

        public static void Connect(string ip, string port,int timeoutMSec)
        {
            bool isConnects = false;
            TimeoutObjiect.Reset();
            FunComm.sck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            FunComm.rep = new IPEndPoint(IPAddress.Parse(ip), int.Parse(port));

           // var resault = FunComm.sck.BeginConnect(FunComm.rep, CallBackMethod, FunComm.sck);
            var resault = FunComm.sck.BeginConnect(FunComm.rep, null, null);
            bool success = resault.AsyncWaitHandle.WaitOne(timeoutMSec,true);

            if (success)
            {
                try
                {
                    FunComm.sck.EndConnect(resault);
                    FunComm.TCP = true;
                }

                catch
                {
                    FunComm.TCP = false;
                }
                
            }
            else
            {
                FunComm.sck.Close();
                FunComm.TCP = false;

            }


            //if (TimeoutObjiect.WaitOne(timeoutMSec, false))
            //{
            //    if (isConnect==null)
            //        FunComm.TCP = false;
            //    else
            //        FunComm.TCP = true;
            //    //FunComm.TCP = true;
                
            //}
            //else
            //{
            //    FunComm.TCP = false;
            //}

            //FunComm.sck.EndConnect();
            //FunComm.sck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //FunComm.rep = new IPEndPoint(IPAddress.Parse(ip), int.Parse(port));
            //try
            //{
            //    FunComm.sck.Connect(FunComm.rep);//建立连接  
            //    FunComm.TCP = true;

            //}
            //catch
            //{
            //    FunComm.TCP = false;
            //}

        }

        public static void DisConnect(string user, string password, string terminalstr)//断开连接
        {
            try
            {
                FunComm.TCP = false;
                FunComm.Logoff(user, password, terminalstr);
                FunComm.sck.Disconnect(true);
                FunComm.sck.Close();
            }
            catch
            {
                FunComm.TCP = false;
                FunComm.sck.Disconnect(true);
                FunComm.sck.Close();
            }

        }

        public static void DataSend(byte[] data)//发送数据
        {
            try
            {
                int len = FunComm.sck.Send(data);
            }
            catch
            {
                FunComm.TCP = false;
                FunComm.sck.Disconnect(true);
                FunComm.sck.Close();
            }
        }


        public static void Logoff(string user, string password, string terminalstr)//退出登录
        {
            byte[] login_info = new byte[40];

            login_info[0] = 0x55;//退出登录
            login_info[1] = 0x00;
            login_info[2] = 0xFF;
           // login_info[3] = 0x84;
            login_info[3]= FunComm.myTCP.mark;

            for (int i = 0; i < user.Length; i++)
            {
                login_info[4 + i] = (byte)user[i];
            }

            for (int i = 0; i < password.Length; i++)
            {
                login_info[20 + i] = (byte)password[i];
            }

            string[] bb = new string[10];
            bb = terminalstr.Split('.');
            login_info[36] = byte.Parse(bb[0]);
            login_info[37] = byte.Parse(bb[1]);
            login_info[38] = byte.Parse(bb[2]);
            login_info[39] = byte.Parse(bb[3]);

            DataSend(login_info);
        }
        public static void Login(string user, string password, string terminalstr)//登录
        {
            byte[] login_info = new byte[40];

            login_info[0] = 0xFF;//登录服务器
            login_info[1] = 0x00;//保留字段
            login_info[2] = 0xFF;
           // login_info[3] = 0x84;//卫星型号
            login_info[3] = FunComm.myTCP.mark;

            for (int i = 0; i < user.Length; i++)
            {
                login_info[4 + i] = (byte)user[i];
            }

            for (int i = 0; i < password.Length; i++)
            {
                login_info[20 + i] = (byte)password[i];
            }

            string[] bb = new string[10];
            bb = terminalstr.Split('.');
            login_info[36] = byte.Parse(bb[0]);
            login_info[37] = byte.Parse(bb[1]);
            login_info[38] = byte.Parse(bb[2]);
            login_info[39] = byte.Parse(bb[3]);

            DataSend(login_info);
        }


        public static void Send_info(byte[] data, string terminalstr)//发送状态数据
        {
            DateTime StartTime = new DateTime(1970, 1, 1, 00, 00, 00);
            byte[] info = new byte[22];

            //info[0] = 0x61;//数据源或属性定义字段
            info[0] = FunComm.myTCP.equ;
            info[1] = 0x00;
            info[2] = 0x00;
            info[3] = FunComm.myTCP.mark;
           // info[3] = 0x84;//卫星型号
            UInt32 Seconds = (UInt32)(DateTime.Now - StartTime).TotalSeconds;

            info[4] = (byte)((Seconds >> 24) & (0x000000FF));
            info[5] = (byte)((Seconds >> 16) & (0x000000FF));
            info[6] = (byte)((Seconds >> 8) & (0x000000FF));
            info[7] = (byte)(Seconds & 0x000000FF);

            string[] bb = new string[10];
            bb = terminalstr.Split('.');
            info[8] = byte.Parse(bb[0]);
            info[9] = byte.Parse(bb[1]);
            info[10] = byte.Parse(bb[2]);
            info[11] = byte.Parse(bb[3]);

            info[12] = (byte)((serial_num >> 24) & (0x000000FF));
            info[13] = (byte)((serial_num >> 16) & (0x000000FF));
            info[14] = (byte)((serial_num >> 8) & (0x000000FF));
            info[15] = (byte)(serial_num & 0x000000FF);

            info[16] = 0x04;
            info[17] = 0x00;//数据长度高低位反

            ///////////////////////////////////////////////数据/状态
            info[18] = data[3];
            info[19] = data[2];
            info[20] = data[1];
            info[21] = data[0];

            DataSend(info);
            serial_num++;
           
        }


        public static void Send_sta(byte []data,string terminalstr)//发送命令执行状态
        {
            DateTime StartTime = new DateTime(1970, 1, 1, 00, 00, 00);
            byte[] info = new byte[21];

           // info[0] = 0x61;//数据源或属性定义字段   11.....61
            info[0] = FunComm.myTCP.equ;
            info[1] = 0x00;
            info[2] = 0x55;
           // info[3] = 0x84;//卫星型号
            info[3] = FunComm.myTCP.mark;
            UInt32 Seconds = (UInt32)(DateTime.Now - StartTime).TotalSeconds;

            info[4] = (byte)((Seconds >> 24) & (0x000000FF));
            info[5] = (byte)((Seconds >> 16) & (0x000000FF));
            info[6] = (byte)((Seconds >> 8) & (0x000000FF));
            info[7] = (byte)(Seconds & 0x000000FF);

            string[] bb = new string[10];
            bb = terminalstr.Split('.');
            info[8] = byte.Parse(bb[0]);
            info[9] = byte.Parse(bb[1]);
            info[10] = byte.Parse(bb[2]);
            info[11] = byte.Parse(bb[3]);

            info[12] = data[0];
            info[13] = data[1];
         
            info[14] = (byte)((serial_num >> 24) & (0x000000FF));
            info[15] = (byte)((serial_num >> 16) & (0x000000FF));
            info[16] = (byte)((serial_num >> 8) & (0x000000FF));
            info[17] = (byte)(serial_num & 0x000000FF);

            info[18] = 0x01;
            info[19] = 0x00;        
            info[20] = data[2];
            FunComm.DataSend(info);
          
        }
        public static void Send_apply(string terminalstr)//申请命令
        {
            DateTime StartTime = new DateTime(1970, 1, 1, 00, 00, 00);
            byte[] info = new byte[21];

            info[0] = 0x11;//实时数据
            info[1] = 0x00;
            info[2] = 0xAA;
           // info[3] = 0x84;//卫星型号
            info[3] = FunComm.myTCP.mark;
            UInt32 Seconds = (UInt32)(DateTime.Now - StartTime).TotalSeconds;

            info[4] = (byte)((Seconds >> 24) & (0x000000FF));
            info[5] = (byte)((Seconds >> 16) & (0x000000FF));
            info[6] = (byte)((Seconds >> 8) & (0x000000FF));
            info[7] = (byte)(Seconds & 0x000000FF);

            string[] bb = new string[10];
            bb = terminalstr.Split('.');
            info[8] = byte.Parse(bb[0]);
            info[9] = byte.Parse(bb[1]);
            info[10] = byte.Parse(bb[2]);
            info[11] = byte.Parse(bb[3]);

           // info[12] = 0x61;//申请数据类型  11...61
            info[12] = FunComm.myTCP.equ;
            info[13] = 0x00;
            info[14] = 0x13;//地面设备
           // info[15] = 0x84;//卫星代号
            info[15] = FunComm.myTCP.mark;

            info[16] = 0x00;//原码或者物理量

            info[17] = (byte)((Seconds >> 24) & (0x000000FF));
            info[18] = (byte)((Seconds >> 16) & (0x000000FF));
            info[19] = (byte)((Seconds >> 8) & (0x000000FF));
            info[20] = (byte)(Seconds & 0x000000FF);
            FunComm.DataSend(info);
        }


    }
}
