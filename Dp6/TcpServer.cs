using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Dp6
{
    class TcpServer : IDisposable, IEquatable<TcpServer>
    {
        private readonly TcpListener _tcp;
        private readonly IPEndPoint _dest;

        private bool _stop;

        public TcpServer(NatMapping nat)
        {
            Nat = nat;
            _dest = nat.InternalEndpoint();

            _tcp = new TcpListener(IPAddress.IPv6Any, Nat.ExternalPort);
            _tcp.AllowNatTraversal(true);
            _tcp.Start();

            Listen();
        }

        public NatMapping Nat { get; }

        void Listen()
        {
            _tcp.AcceptSocketAsync()
                .ContinueWith(OnConnect);
        }

        private void OnConnect(Task<Socket> task)
        {
            try
            {
                if (_stop) return;

                Listen();

                Socket srcSocket = task.Result;

                Socket dstSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                dstSocket.Connect(_dest);

                TcpForwarder.Start2(srcSocket, dstSocket);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        public void Dispose()
        {
            _stop = true;
            _tcp.Stop();
        }

        public override string ToString() => Nat.ToString();

        public bool Equals(TcpServer other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Nat.Equals(other.Nat);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TcpServer) obj);
        }

        public override int GetHashCode()
        {
            return Nat.GetHashCode();
        }

        public static bool operator ==(TcpServer left, TcpServer right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TcpServer left, TcpServer right)
        {
            return !Equals(left, right);
        }
    }
}
