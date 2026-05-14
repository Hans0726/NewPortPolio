using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Session;
using ServerCore;

namespace Server
{
    class GameRoom : IJobQueue
    {
        ClientSession[] _sessions = new ClientSession[2]; 
        object _lock = new object();
        JobQueue _jobQueue = new JobQueue();
        List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();

        public void Push(Action job)
        {
            _jobQueue.Push(job);
        }

        public void Flush()
        {
            foreach (ClientSession s in _sessions)
            {
                if (s == null)
                    return;
                s.Send(_pendingList);
            }
                

            _pendingList.Clear();
        }

        public void BroadCast(ArraySegment<byte> segment)
        {
            _pendingList.Add(segment);
        }

        //public void Enter(ClientSession session)
        //{
        //    // 입장 플레이어한테 상대 플레이어 목록 전송
        //    S_PlayerList players = new S_PlayerList();
        //    foreach (ClientSession s in _sessions)
        //    {
        //        players.players.Add(new S_PlayerList.Player()
        //        {
        //            isSelf = (s == session),
        //            playerId = s.SessionId,
        //        });
        //    }

        //    session.Send(players.Serialize());

        //    // 입장 플레이어를 모두에게 알림
        //    S_BroadcastEnterGame enter = new S_BroadcastEnterGame();
        //    enter.playerId = session.SessionId;
        //    enter.posX = 0;
        //    enter.posY = 0;
        //    enter.posZ = 0;

        //    BroadCast(enter.Serialize());
        //}

        public void Leave(ClientSession session)
        {
            // 플레이어 제거
            if (session.Equals(_sessions[0]) == true)
                _sessions[0] = null;
            else
                _sessions[1] = null;

            // 모두에게 알림
            //S_BroadcastLeaveGame leave = new S_BroadcastLeaveGame();
            //leave.playerId = session.SessionId;
            //BroadCast(leave.Serialize());
        }

        public void RequestMatch(ClientSession session)
        {
            session.Room = this;
            if (_sessions[0] == null)
                _sessions[0] = session;
            else
                _sessions[1] = session;

            S_MatchingSuccess s = new S_MatchingSuccess();
            S_PlayerMatchingReqOk sok = new S_PlayerMatchingReqOk();

            session.Send(sok.Serialize());
            if (_sessions[0] != null && _sessions[1] != null)
            {
                foreach(ClientSession bothsession in _sessions)
                {
                    bothsession.Send(s.Serialize());
                }
            }
        }

        public void CancelMatch(ClientSession session)
        {
            session.Room = null;
            if (_sessions[0].SessionId == session.SessionId)
                _sessions[0] = null;
            else
                _sessions[1] = null;
        }

        //public void Move(ClientSession session, C_Move packet)
        //{
        //    // 좌표 바꾸기
        //    session.PosX = packet.posX; 
        //    session.PosY = packet.posY; 
        //    session.PosZ = packet.posZ;

        //    // 모두에게 알리기
        //    S_BroadcastMove move = new S_BroadcastMove();
        //    move.playerId = session.SessionId;
        //    move.posX = session.PosX;
        //    move.posY = session.PosY;
        //    move.posZ = session.PosZ;
        //    BroadCast(move.Serialize());
        //}
    }
}
