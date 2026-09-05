using System;
using System.Collections.Generic;
using System.Net;
using ServerCore;
using System.Text;

namespace DummyClient
{
    class ServerSession : PacketSession
    {
        private readonly Action<EndPoint> _onConnected;
        private readonly Action<EndPoint> _onDisconnected;

        public ServerSession(
            Action<EndPoint> onConnected = null,
            Action<EndPoint> onDisconnected = null)
        {
            _onConnected = onConnected;
            _onDisconnected = onDisconnected;
        }

        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected: {endPoint}");
            _onConnected?.Invoke(endPoint);
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisconnected: {endPoint}");
            _onDisconnected?.Invoke(endPoint);
        }

        public override void OnRecvPacket(ArraySegment<byte> buffer)
        {
            PacketManager.Instance.OnRecvPacket(this, buffer, (s, p) => PacketQueue.Instance.Push(p));
        }

        public override void OnSend(int numOfBytes)
        {
            //Console.WriteLine($"Transferred bytes: {numOfBytes}");
        }
    }
}
