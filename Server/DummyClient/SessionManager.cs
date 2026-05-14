using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DummyClient
{
    class SessionManager
    {
        static SessionManager _instance = new SessionManager();
        public static SessionManager Instance { get { return _instance; } }

        List<ServerSession> _sessions = new List<ServerSession>();
        int _testCount = 0;
        object _lock = new object();
        Random _rand = new Random();

        public void SendForEach()
        {
            lock (_lock)
            {
                if (_testCount >= 2)
                    return;

                foreach (ServerSession session in _sessions)
                {
                    C_PlayerMatchingReq matchingReq = new C_PlayerMatchingReq();
                    session.Send(matchingReq.Serialize());
                    _testCount++;
                }
            }
        }

        public ServerSession Generate()
        {
            lock (_lock)
            {
                ServerSession session = new ServerSession();
                _sessions.Add(session);
                _testCount++;
                return session;
            }
        }

    }
}
