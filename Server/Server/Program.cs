using ServerCore;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Server;
using Server.Session;

namespace ServerCore
{
    class Program
    {
        static Listener _listener = new Listener();
        public static Queue<GameRoom> MatchingRooms = new();
        public static List<GameRoom> ActiveRooms = new();

        static void FlushRoom()
        {
            foreach (GameRoom room in ActiveRooms)
                room.Push(() => room.Flush());
            JobTimer.Instance.Push(FlushRoom, 250);
        }

        static void Main(string[] args)
        {
            // DNS (Domain Name System)
            string host = Dns.GetHostName();
            IPHostEntry iPHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = iPHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

            _listener.Init(endPoint, () => { return SessionManager.Instance.Generate(); });
            UserDatas.Instance.Init();
            Console.WriteLine("Listening...");

            JobTimer.Instance.Push(FlushRoom);

            while (true)
            {
                JobTimer.Instance.Flush();
            }
        }
    }
}
