using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Time_Syncer
{
    public partial class Form1 : Form
    {
        DateTime Sync_Time;
        public Thread thread_Time_Sync;
        public bool flag = false;

        public Form1()
        {
            InitializeComponent();
            init();
        }

        public void init()
        {
            thread_Time_Sync = new Thread(new ThreadStart(thread_func));
            
        }

        public void thread_func()
        {
            try
            {
                
                
                while (true)
                {
                    if(flag)
                    {

                        if(DateTime.Now.Second == 0)
                        {
                            setNowTime();
                        }
                        else
                        {
                            TimeSpan span_temp;

                            span_temp = DateTime.Now - Sync_Time;

                            if (label1.InvokeRequired)
                            {
                                label1.Invoke(new MethodInvoker(delegate
                                {
                                    label1.Text = string.Format("실행 : {0:MM-dd HH:mm:ss}({1}초전)", Sync_Time, span_temp.Seconds);
                                }));
                            }
                            else
                            {
                                label1.Text = string.Format("실행 : {0:MM-dd HH:mm:ss}({1}초전)", Sync_Time, span_temp.Seconds);
                            }

                            
                        }

                        

                        Thread.Sleep(1000 * 1 * 1);


                    }
                    else
                    {
                        Thread.Sleep(1000 * 1);
                    }
                    GC.Collect();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR!!");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            setNowTime();
        }

        [DllImport("kernel32")]
        public static extern int SetSystemTime(ref SYSTEMTIME lpSystemTime);


        public struct SYSTEMTIME
        {
            public short wYear; //년도
            public short wMonth; //월
            public short wDayOfWeek; //요일
            public short wDay; //일
            public short wHour; //시
            public short wMinute; //분
            public short wSecond; //초
            public short wMilliseconds; //1/100초
        }

        private void setNowTime()
        {
            SYSTEMTIME sTime = new SYSTEMTIME();

            DateTime temp = GetNetworkTime();

            /*
            sTime.wYear = Convert.ToInt16(GetNetworkTime().Year);
            sTime.wMonth = Convert.ToInt16(GetNetworkTime().Month);
            sTime.wDayOfWeek = Convert.ToInt16(GetNetworkTime().DayOfWeek);
            sTime.wDay = Convert.ToInt16(GetNetworkTime().Day);
            sTime.wHour = Convert.ToInt16(GetNetworkTime().Hour);
            sTime.wMinute = Convert.ToInt16(GetNetworkTime().Minute);
            sTime.wSecond = Convert.ToInt16(GetNetworkTime().Second);
            sTime.wMilliseconds = Convert.ToInt16(GetNetworkTime().Millisecond);
            */

            sTime.wYear = Convert.ToInt16(temp.Year);
            sTime.wMonth = Convert.ToInt16(temp.Month);
            sTime.wDayOfWeek = Convert.ToInt16(temp.DayOfWeek);
            sTime.wDay = Convert.ToInt16(temp.Day);
            sTime.wHour = Convert.ToInt16(temp.Hour);
            sTime.wMinute = Convert.ToInt16(temp.Minute);
            sTime.wSecond = Convert.ToInt16(temp.Second);
            sTime.wMilliseconds = Convert.ToInt16(temp.Millisecond);

            SetSystemTime(ref sTime);

            Thread.Sleep(100);
            if(label1.InvokeRequired)
            {
                label1.Invoke(new MethodInvoker(delegate
                {
                    label1.Text = string.Format("실행 : {0:MM-dd HH:mm:ss}", temp);
                }));
            }
            else
            {
                label1.Text = string.Format("실행 : {0:MM-dd HH:mm:ss}", temp);
            }

            Sync_Time = temp;
        }


        /// <summary>
        /// Gets the current DateTime from time-a.nist.gov.
        /// </summary>
        /// <returns>A DateTime containing the current time.</returns>
        public static DateTime GetNetworkTime()
        {
            //return GetNetworkTime("time.windows.com"); // time-a.nist.gov
            //return GetNetworkTime("52.231.114.183"); // time-a.nist.gov
            return GetNetworkTime("time.microsoft.akadns.net"); // time-a.nist.gov
        }

        /// <summary>
        /// Gets the current DateTime from <paramref name="ntpServer"/>.
        /// </summary>
        /// <param name="ntpServer">The hostname of the NTP server.</param>
        /// <returns>A DateTime containing the current time.</returns>
        public static DateTime GetNetworkTime(string ntpServer)
        {
            IPAddress[] address = Dns.GetHostEntry(ntpServer).AddressList;

            if (address == null || address.Length == 0)
                throw new ArgumentException("Could not resolve ip address from '" + ntpServer + "'.", "ntpServer");


            IPAddress final_IP = null;
            for(int i = 0; i < address.Length; i++)
            {
                string IP = string.Format("{0}", address[i]);

                double temp = 0;
                try
                {
                    bool is_num = double.TryParse(string.Format("{0}", address[i].Address), out temp);
                    if (is_num)
                    {
                        final_IP = address[i];
                        break;
                    }
                }
                catch(Exception ex)
                {

                }
            }


            //IPEndPoint ep = new IPEndPoint(address[0], 123);
            IPEndPoint ep = new IPEndPoint(final_IP, 123);
            return GetNetworkTime(ep);
        }

        /// <summary>
        /// Gets the current DateTime form <paramref name="ep"/> IPEndPoint.
        /// </summary>
        /// <param name="ep">The IPEndPoint to connect to.</param>
        /// <returns>A DateTime containing the current time.</returns>
        public static DateTime GetNetworkTime(IPEndPoint ep)
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            s.Connect(ep);

            byte[] ntpData = new byte[48]; // RFC 2030 
            ntpData[0] = 0x1B;
            for (int i = 1; i < 48; i++)
                ntpData[i] = 0;

            s.Send(ntpData);
            s.Receive(ntpData);

            byte offsetTransmitTime = 40;
            ulong intpart = 0;
            ulong fractpart = 0;

            for (int i = 0; i <= 3; i++)
                intpart = 256 * intpart + ntpData[offsetTransmitTime + i];

            for (int i = 4; i <= 7; i++)
                fractpart = 256 * fractpart + ntpData[offsetTransmitTime + i];

            ulong milliseconds = (intpart * 1000 + (fractpart * 1000) / 0x100000000L);
            s.Close();

            TimeSpan timeSpan = TimeSpan.FromTicks((long)milliseconds * TimeSpan.TicksPerMillisecond);

            DateTime dateTime = new DateTime(1900, 1, 1);
            dateTime += timeSpan;

            TimeSpan offsetAmount = TimeZone.CurrentTimeZone.GetUtcOffset(dateTime);
            DateTime networkDateTime = (dateTime + offsetAmount);

            return networkDateTime.AddHours(-9);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(!thread_Time_Sync.IsAlive)
                thread_Time_Sync.Start();
            flag = true;

            label2.Invoke(new MethodInvoker(delegate
            {
                label2.Text = string.Format("FLAG : TRUE");
            }));
        }

        private void button3_Click(object sender, EventArgs e)
        {
            flag = false;
            label2.Invoke(new MethodInvoker(delegate
            {
                label2.Text = string.Format("FLAG : FALSE");
            }));
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if(thread_Time_Sync.IsAlive)
                thread_Time_Sync.Abort();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            /*
            if (!IsRunningAsAdministrator())
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo(Assembly.GetEntryAssembly().CodeBase);
                {
                    var withBlock = processStartInfo;
                    withBlock.UseShellExecute = true;
                    withBlock.Verb = "runas";
                    Process.Start(processStartInfo);
                    Application.Exit();
                }
            }
            else
                this.Text += " " + "(Administrator)";
            */
        }

        public bool IsRunningAsAdministrator()
        {
            WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent();
            WindowsPrincipal windowsPrincipal = new WindowsPrincipal(windowsIdentity);
            return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
