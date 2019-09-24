using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AVS.ProxyUtil;

namespace AVS.NetUtil
{
    public partial class PingForm : Form
    {
        public string Host => tbHost.Text;
        public string Host2 => tbHost2.Text;
        public int Port => (int)numericPort.Value;
        public int Port2 => (int)numericPort2.Value;
        public int Interval => (int)numericInterval.Value;
        readonly PingHelper _pingHelper = new PingHelper();
        public PingForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            timer1.Interval = Interval*1000;
            _pingHelper.Clear();
            timer1.Start();
            button1.Enabled = false;
            btnStop.Enabled = true;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            timer1.Stop();
            WriteToLog("\r\n\r\n");
            WriteToLog(_pingHelper.GetInfo());
            WriteToLog(_pingHelper.GetHostConnectionFails(Host));
            button1.Enabled = true;
            btnStop.Enabled = false;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (Ping())
            {
                if (timer1.Interval < Interval * 1000)
                {
                    WriteToLog($"Ping interval set to {Interval} sec.\r\n");
                }
                timer1.Interval = Interval * 1000;
            }
            else
            {
                if (timer1.Interval > 5000)
                {
                    WriteToLog($"Ping interval set to 5 sec.\r\n");
                    timer1.Interval = 5000;
                }
            }
        }

        private bool Ping()
        {
            if (_pingHelper.Ping(Host, Port))
            {
                WriteToLog($"{DateTime.Now:G}  > ping {Host}:{Port} - OK\r\n");
                return true;
            }
            else
            {
                if (_pingHelper.Ping(Host2, Port2))
                {
                    WriteToLog($"{DateTime.Now:G}  > ping {Host}:{Port} - Failed;   {Host2}:{Port2} - OK\r\n");
                    return true;
                }
                WriteToLog($"{DateTime.Now:G}  > ping {Host}:{Port} - Failed;   {Host2}:{Port2} - Failed\r\n");
                return false;
            }
        }

        private void WriteToLog(string message)
        {
            richTextBox1.AppendText(message);
            richTextBox1.ScrollToCaret();
        }
    }

    public class PingHelper
    {
        public Dictionary<DateTime, PingTest> Pings = new Dictionary<DateTime, PingTest>();
        public class PingTest
        {
            public DateTime Date { get; set; }
            public string Address { get; set; }
            public bool Success { get; set; }
        }

        public bool Ping(string host, int port)
        {
            var ping = new PingTest()
            {
                Address = $"{host}:{port}",
                Date = DateTime.Now,
                Success = TcpUtil.PingHost(host, port)
            };

            if(!Pings.ContainsKey(ping.Date))
                Pings.Add(ping.Date, ping);
            return ping.Success;
        }

        public string GetInfo()
        {
            var sb = new StringBuilder();
            var dates = Pings.Values.Select(v => v.Date.Date).Distinct().ToArray();
            foreach (var date in dates)
            {
                sb.AppendLine($"{date:D}\r\n");

                var arr = Pings.Values.Where(v => v.Date.Date == date).GroupBy(v => v.Address).ToArray();
                foreach (var grouping in arr)
                {
                    sb.AppendLine($" ping {grouping.Key}  >  {grouping.Count(g => g.Success)} - OK;    {grouping.Count(g => g.Success==false)} - Failed");
                }
                sb.AppendLine();
                
                
            }
            return sb.ToString();
        }

        public string GetHostConnectionFails(string host)
        {
            var sb = new StringBuilder();
            DateTime start = DateTime.MinValue;
            DateTime end = DateTime.MinValue;
            sb.AppendLine();
            sb.AppendLine($"Host {host} connection fails:");
            int shortConnectionFailures = 0;
            foreach (var pingTest in Pings.Values.Where(v => v.Address.StartsWith(host)))
            {
                if (pingTest.Success)
                {
                    if (end == DateTime.MinValue && start > DateTime.MinValue)
                    {
                        end = pingTest.Date;
                        var ts = end - start;
                        if (ts.TotalSeconds > 60)
                        {
                            sb.AppendLine($"{start:d}   from {start:T} till {end:T}          [{ts.TotalSeconds} sec.]");
                        }
                        else
                        {
                            shortConnectionFailures++;
                        }

                        start = DateTime.MinValue;
                        end = DateTime.MinValue;
                    }
                    continue;
                }

                if (start == DateTime.MinValue)
                    start = pingTest.Date;
            }

            if (start > DateTime.MinValue && end == DateTime.MinValue)
            {
                end = DateTime.Now;
                var ts = end - start;
                sb.AppendLine($"{start:d} connection failed:         from {start:T} till {end:T}          [{ts.TotalSeconds} sec.]");
            }
            if(shortConnectionFailures > 0)
            sb.AppendLine($"Short [less than 1 min..] connection troubles #{shortConnectionFailures}.]");

            return sb.ToString();
        }

        public void Clear()
        {
            Pings.Clear();
        }
    }

}
