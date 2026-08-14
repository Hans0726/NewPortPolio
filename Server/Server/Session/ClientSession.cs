using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server.Session
{
    class ClientSession : PacketSession
    {
        public int SessionId { get; set; }
        public string SessionIP { get; set; } 
        public GameRoom Room { get; set; }

        public UserData UserData { get; set; } 

        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected: {endPoint}");
            SessionIP = (endPoint as IPEndPoint).Address.ToString();
            UserData = UserDatas.Instance.GetUserData(SessionIP);
            if (UserData == null)
            {
                Console.WriteLine("UserData empty. Empty Data is creating");
                UserData = UserDatas.Instance.GetUserData("empty");
                UserDatas.Instance.SaveData(SessionIP, UserData, "");
            }
            UserDatas.Instance.SendDeckPacket(UserData, this);
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            SessionManager.Instance.Remove(this);
            Dispose();
            Console.WriteLine($"OnDisconnected: {endPoint}");
        }

        public override void OnRecvPacket(ArraySegment<byte> buffer)
        {
            PacketManager.Instance.OnRecvPacket(this, buffer);
        }

        public override void OnSend(int numOfBytes)
        {
            //Console.WriteLine($"Transferred bytes: {numOfBytes}");
        }

        public void Dispose()
        {
            if (Room != null)
            {
                GameRoom room = Room;
                room.Push(() => room.Leave(this));
                Room = null;
            }
        }

        //public void OnMatched()
        //{
        //    Room = Program.MatchingRoom.Peek();
        //    //Room.Push(() => Program.MatchingRoom.Enter(this));
        //}
    }
}
