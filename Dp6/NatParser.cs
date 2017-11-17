using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;

namespace Dp6
{
    class NatParser
    {
        //private readonly Regex _rxTcp = new Regex(@"^\s+(?<id>\d+)\s+TCP\s+(?<ext>\d+)\s+(?<int>\d+)\s+(?<ip>\d+\.\d+\.\d+\.\d+)\s*$");
        private readonly Regex _rxTcp = new Regex(@"\s+(?<ip>\d+\.\d+\.\d+\.\d+)\:(?<ext>\d+)\-\>(?<int>\d+)\/tcp", RegexOptions.IgnoreCase);

        public IList<NatMapping> NatMappings { get; }

        public NatParser()
        {
            NatMappings = new List<NatMapping>();
        }

        public int GetMappings()
        {
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                LoadUserProfile = false,
                RedirectStandardOutput = true,
                FileName = "Docker.exe",
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = "ps"
            };

            var process = new Process {StartInfo = startInfo};
            process.OutputDataReceived += Process_OutputDataReceived;

            process.Start();
            process.BeginOutputReadLine();

            process.WaitForExit(2000);

            return NatMappings.Count;
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if ( string.IsNullOrWhiteSpace(e.Data) ) return;

            Match m = _rxTcp.Match(e.Data);
            if (m.Success)
            {
                NatMappings.Add(new NatMapping
                {
                    Protocol = "TCP",
                    ExternalPort = int.Parse(m.Groups["ext"].Value),
                    InternalPort = Int32.Parse(m.Groups["int"].Value),
                    InternalIpAddress = IPAddress.Parse(m.Groups["ip"].Value)
                });
            }
        }
    }

    class NatMapping : IEquatable<NatMapping>
    {
        public string Protocol { get; set; }
        public int ExternalPort { get; set; }
        public int InternalPort { get; set; }
        public IPAddress InternalIpAddress { get; set; }

        public IPEndPoint InternalEndpoint() => new IPEndPoint(InternalIpAddress, InternalPort);

        public override string ToString()
        {
            return $"'[*]:{ExternalPort}'=>'[{InternalIpAddress}]:{InternalPort}'";
        }

        public bool Equals(NatMapping other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return ExternalPort == other.ExternalPort && InternalPort == other.InternalPort && InternalIpAddress.Equals(other.InternalIpAddress);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((NatMapping) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ExternalPort;
                hashCode = (hashCode * 397) ^ InternalPort;
                hashCode = (hashCode * 397) ^ InternalIpAddress.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(NatMapping left, NatMapping right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(NatMapping left, NatMapping right)
        {
            return !Equals(left, right);
        }
    }
}
