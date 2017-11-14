using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Timers;

namespace Dp6
{
    public partial class DpService : ServiceBase
    {
        private List<TcpServer> _ports;
        private Timer _timer;

        public DpService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Startup(args);
        }

        internal void Startup(string[] args)
        {
            _ports = new List<TcpServer>();

            _timer = new Timer(1000) {AutoReset = false};
            _timer.Elapsed += TimerOnElapsed;
            _timer.Start();
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            try
            {
                // get mappings
                var nat = new NatParser();
                nat.GetMappings();

                // kill unneeded ports
                var toKill = (from p in _ports
                              where ! nat.NatMappings.Any(m => m.Equals(p.Nat))
                              select p
                              ).ToArray();

                foreach (TcpServer port in toKill)
                {
                    port.Dispose();
                    _ports.Remove(port);
                }

                // add new ports
                var toAdd = (from m in nat.NatMappings
                            where !_ports.Any(p => p.Nat.Equals(m))
                            select new TcpServer(m)
                            ).ToArray();

                _ports.AddRange(toAdd);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            finally
            {
                _timer.Interval = 7000;
                _timer.Start();
            }
        }

        protected override void OnStop()
        {
            _timer.Dispose();
            _timer = null;

            foreach (TcpServer server in _ports)
                server.Dispose();

            _ports.Clear();
            _ports = null;
        }
    }
}
