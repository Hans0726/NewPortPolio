using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    public class Connector
    {
        private sealed class ConnectContext
        {
            public Socket Socket { get; }
            public Func<Session> SessionFactory { get; }
            public Action<SocketError> OnConnectFailed { get; }

            public ConnectContext(
                Socket socket,
                Func<Session> sessionFactory,
                Action<SocketError> onConnectFailed)
            {
                Socket = socket;
                SessionFactory = sessionFactory;
                OnConnectFailed = onConnectFailed;
            }
        }

        public void Connect(
            IPEndPoint endPoint,
            Func<Session> sessionFactory,
            int count = 1,
            Action<SocketError> onConnectFailed = null)
        {
            for (int i = 0; i < count; i++)
            {
                // 휴대폰 설정
                Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += OnConnectCompleted;
                args.RemoteEndPoint = endPoint;
                args.UserToken = new ConnectContext(socket, sessionFactory, onConnectFailed);

                RegisterConnect(args);
            }
        }

        void RegisterConnect(SocketAsyncEventArgs args)
        {
            ConnectContext context = args.UserToken as ConnectContext;
            if (context == null)
                return;

            bool pending = context.Socket.ConnectAsync(args);
            if (pending == false)
                OnConnectCompleted(null, args);
        }

        void OnConnectCompleted(object sender, SocketAsyncEventArgs args)
        {
            ConnectContext context = args.UserToken as ConnectContext;

            if (args.SocketError == SocketError.Success)
            {
                Session session = context.SessionFactory.Invoke();
                session.Start(args.ConnectSocket);
                session.OnConnected(args.RemoteEndPoint);
            }
            else
            {
                Console.WriteLine($"OnConnectCompleted Fail: {args.SocketError}");
                context?.OnConnectFailed?.Invoke(args.SocketError);
                context?.Socket.Dispose();
            }

            args.Completed -= OnConnectCompleted;
            args.Dispose();
        }
    }
}
