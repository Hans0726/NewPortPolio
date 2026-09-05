using DummyClient;
using ServerCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class NetworkMananger : MonoBehaviour
{
    private static NetworkMananger _instance = null;
    public static NetworkMananger Instance { get { return _instance; } }
    public NetworkGateway Gateway { get; private set; }

    private ServerSession _session;
    public event Action ConnectionFailed;
    private int _connectionFailurePending;
    private int _isShuttingDown;


    public void Send(ArraySegment<byte> sendBuff)
    {
        _session.Send(sendBuff);
    }
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _session = new ServerSession(onDisconnected: HandleSessionDisconnected);
        Gateway = new NetworkGateway(this);

        NetworkFailurePresenter failurePresenter = GetComponent<NetworkFailurePresenter>();
        if (failurePresenter == null)
            failurePresenter = gameObject.AddComponent<NetworkFailurePresenter>();

        failurePresenter.Initialize(this);
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // DNS (Domain Name System)            
        string host = Dns.GetHostName();
        IPHostEntry ipHost = Dns.GetHostEntry(host);
        IPAddress ipAddr = ipHost.AddressList[0];
        IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

        Connector connector = new Connector();

        connector.Connect(endPoint,
            () => { return _session; },
            1,
            HandleConnectFailed);
    }

    // Update is called once per frame
    void Update()
    {
        if (Interlocked.Exchange(ref _connectionFailurePending, 0) == 1)
        {
            ConnectionFailed?.Invoke();
        }

        List<IPacket> list = PacketQueue.Instance.PopAll();
        foreach (IPacket packet in list)
            PacketManager.Instance.HandlePacket(_session, packet);
    }

    private void HandleConnectFailed(SocketError socketError)
    {
        NotifyConnectionFailure();
    }

    private void HandleSessionDisconnected(EndPoint endPoint)
    {
        if (Interlocked.CompareExchange(ref _isShuttingDown, 0, 0) == 1)
            return;

        NotifyConnectionFailure();
    }

    private void OnApplicationQuit()
    {
        Interlocked.Exchange(ref _isShuttingDown, 1);
        _session?.Disconnect();
    }

    public void NotifyConnectionFailure()
    {
        Interlocked.Exchange(ref _connectionFailurePending, 1);
    }
}
