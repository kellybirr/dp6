using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Timers;

namespace Dp6
{
    class TcpForwarder : IDisposable
    {
        private readonly Socket _src, _dst;
        private volatile bool _disposed;

        private readonly byte[] _buffer = new byte[1<<16];
        private DateTime _lastActivity = DateTime.UtcNow;

        public static List<TcpForwarder> Active { get; } = new List<TcpForwarder>();

        private static Timer _checkTimer;
        private static readonly object _timerLock = new object();

        private static void StartMonitor()
        {
            lock (_timerLock)
            {
                if (_checkTimer != null) return;
                Debug.WriteLine("CREATING TCP FORWARD CHECK TIMER");

                _checkTimer = new Timer(8000) {AutoReset = false};
                _checkTimer.Elapsed += CheckTimerOnElapsed;
                _checkTimer.Start();
            }
        }

        private static void CheckTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            try
            {
                Debug.WriteLine("TCP FORWARDER CHECK");

                DateTime old = DateTime.UtcNow.AddMinutes(-1);
                TcpForwarder[] toCheck = Active.Where(f => f._lastActivity < old).ToArray();

                foreach (TcpForwarder f in toCheck)
                {
                    if (f._disposed) continue;

                    lock (f)
                    {
                        try
                        {
                            byte[] tmp = new byte[1];

                            f._src.Blocking = false;
                            f._src.Send(tmp, 0, 0);
                        }
                        catch (ObjectDisposedException)
                        {
                            using (f) continue;
                        }
                        catch (SocketException se)
                        {
                            // 10035 == WSAEWOULDBLOCK
                            if (! se.NativeErrorCode.Equals(10035))
                                using (f) continue;
                        }

                        if (! f._src.Connected)
                            f.Dispose();
                    }
                }
            }
            catch (Exception e1)
            {
                Debug.WriteLine(e1);
            }
            finally
            {
                _checkTimer.Start();
            }
        }

        public static void Start1(Socket src, Socket dst)
        {
            var f1 = new TcpForwarder(src, dst);

            lock (Active)
                Active.Add(f1);

            if (_checkTimer == null)
                StartMonitor();
        }

        public static void Start2(Socket s1, Socket s2)
        {
            var f2 = new[]
            {
                new TcpForwarder(s1, s2),
                new TcpForwarder(s2, s1)
            };

            lock (Active)
                Active.AddRange(f2);

            if (_checkTimer == null)
                StartMonitor();
        }

        private TcpForwarder(Socket src, Socket dst)
        {
            _src = src;
            _dst = dst;

            Read();
        }

        public void Dispose()
        {
            if (_disposed) return;
            lock (this)
            {
                if (_disposed) return;
                Debug.WriteLine("DISPOSING FORWARDER");

                using (_src)
                {
                    using (_dst)
                    {
                        _disposed = true;
                    }
                }

                lock (Active)
                    Active.Remove(this);
            }
        }

        private void Read()
        {
            _src.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, OnDataReceive, null);
        }

        private void OnDataReceive(IAsyncResult result)
        {
            try
            {
                _lastActivity = DateTime.UtcNow;

                var bytes = _src.EndReceive(result);
                if (bytes > 0 && !_disposed)
                    _dst.BeginSend(_buffer, 0, bytes, SocketFlags.None, OnDataSend, null);
            }
            catch
            {
                Dispose();
            }
        }

        private void OnDataSend(IAsyncResult result)
        {
            try
            {
                _dst.EndSend(result);
                Read();
            }
            catch
            {
                Dispose();
            }
        }
    }
}
