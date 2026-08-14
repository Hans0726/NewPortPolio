using Server.Session;
using ServerCore;

namespace Server
{
    class GameRoom : IJobQueue
    {
        ClientSession[] _sessions = new ClientSession[2]; 
        JobQueue _jobQueue = new JobQueue();
        List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();
        HashSet<int> _turnStartReadySessions = new HashSet<int>();
        HashSet<int> _turnEndSessions = new HashSet<int>();
        Dictionary<int, int> _lifeUpdateSessions = new Dictionary<int,int>();
        int _currentTurn;
        const int PreparationTimeSeconds = 30;
        const int PlayerLife = 10;
        public bool IsFull => _sessions[0] != null && _sessions[1] != null;
        public bool IsEmpty => _sessions[0] == null && _sessions[1] == null;
        bool _gameEnded = false;


        public enum GameResult
        {
            Victory,
            Defeat
        }

        public void Push(Action job)
        {
            _jobQueue.Push(job);
        }

        public void Flush()
        {
            foreach (ClientSession s in _sessions)
            {
                if (s == null)
                    continue;
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
            _turnStartReadySessions.Remove(session.SessionId);
            _turnEndSessions.Remove(session.SessionId);
            _lifeUpdateSessions.Remove(session.SessionId);

            // 플레이어 제거
            if (session.Equals(_sessions[0]) == true)
                _sessions[0] = null;
            else
                _sessions[1] = null;
            
            if(IsEmpty)
            {
                Program.ActiveRooms.Remove(this);
            }

            // 모두에게 알림
            S_BroadcastLeaveGame leave = new S_BroadcastLeaveGame();
            leave.playerId = session.SessionId;
            BroadCast(leave.Serialize());
        }

        public void RequestMatch(ClientSession session)
        {
            session.Room = this;
            if (_sessions[0] == null)
                _sessions[0] = session;
            else
                _sessions[1] = session;

            S_PlayerMatchingReqOk sok = new S_PlayerMatchingReqOk();
            session.Send(sok.Serialize());

            if (IsFull)
            {
                Program.ActiveRooms.Add(Program.MatchingRooms.Dequeue());
                S_MatchingSuccess s = new S_MatchingSuccess();
                BroadCast(s.Serialize());
                _lifeUpdateSessions.Add(_sessions[0].SessionId, PlayerLife);
                _lifeUpdateSessions.Add(_sessions[1].SessionId, PlayerLife);
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

        public void ReadyForTurn(ClientSession session, bool ready)
        {
            if (session == null || !ready || !ContainsSession(session))
                return;

            _turnStartReadySessions.Add(session.SessionId);
            if (_turnStartReadySessions.Count < 2)
                return;

            _turnStartReadySessions.Clear();
            _turnEndSessions.Clear();
            _currentTurn++;

            S_TurnStart packet = new S_TurnStart
            {
                turnNumber = _currentTurn,
                turnTime = PreparationTimeSeconds
            };
            BroadCast(packet.Serialize());
        }

        public void EndTurnPreparation(ClientSession session)
        {
            if (session == null || !ContainsSession(session))
                return;

            _turnEndSessions.Add(session.SessionId);
            if (_turnEndSessions.Count < 2)
                return;

            _turnEndSessions.Clear();
            BroadCast(new S_TurnEnd().Serialize());
        }

        public void RelayCardSelection(ClientSession sender, C_CardSelect packet)
        {
            if (sender == null || packet == null || !ContainsSession(sender))
                return;

            S_CardSelectResult result = new S_CardSelectResult
            {
                playerId = sender.SessionId
            };

            foreach (C_CardSelect.SelectedCardIds selectedCard in packet.selectedCardIdss)
            {
                result.selectedCardIdss.Add(new S_CardSelectResult.SelectedCardIds
                {
                    cardId = selectedCard.cardId
                });
            }

            SendToOpponent(sender, result.Serialize());
        }

        public void RelayUnitPlacement(ClientSession sender, C_UnitPlacement packet)
        {
            if (sender == null || packet == null || !ContainsSession(sender))
                return;

            S_UnitPlacementResult result = new S_UnitPlacementResult
            {
                playerId = sender.SessionId,
                cardId = packet.cardId,
                x = packet.x,
                y = packet.y,
                isSuccess = true,
                errorMessage = string.Empty
            };
            SendToOpponent(sender, result.Serialize());
        }

        public void RelayLifeUpdate(ClientSession sender, C_LifeUpdate packet)
        {
            if (sender == null || packet == null || !ContainsSession(sender) || _gameEnded)
                return;

            // 게임 룸은 각 세션에 해당하는 라이프를 가지고 있어야하고 둘 중에 하나라도 라이프가 0이 되면 게임 종료를 알리는 패킷을 보내야 한다.
            if (_lifeUpdateSessions.ContainsKey(sender.SessionId))
                _lifeUpdateSessions[sender.SessionId] = packet.life;

            if (_lifeUpdateSessions.Values.Any(life => life <= 0))
            {
                _gameEnded = true;
                GameResult result = _lifeUpdateSessions[sender.SessionId] <= 0 ? GameResult.Defeat : GameResult.Victory;
                S_GameResult gameEndPacket = new S_GameResult
                {
                    winnerId = _lifeUpdateSessions[sender.SessionId] > 0 ? sender.SessionId : _sessions.First(s => s != null && s.SessionId != sender.SessionId).SessionId,
                    reason = result == GameResult.Victory ? "승리!" : "패배!"
                };
                BroadCast(gameEndPacket.Serialize());
            }
        }

        private bool ContainsSession(ClientSession session)
        {
            return (_sessions[0] != null && _sessions[0].SessionId == session.SessionId)
                || (_sessions[1] != null && _sessions[1].SessionId == session.SessionId);
        }

        private void SendToOpponent(ClientSession sender, ArraySegment<byte> packet)
        {
            foreach (ClientSession session in _sessions)
            {
                if (session != null && session.SessionId != sender.SessionId)
                {
                    session.Send(packet);
                }
            }
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
