using DummyClient;
using ServerCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class NetworkMananger : MonoBehaviour
{
    private static NetworkMananger _instance = null;
    public static NetworkMananger Instance { get { return _instance; } }

    ServerSession _session = new ServerSession();
    public void Send(ArraySegment<byte> sendBuff)
    {
        _session.Send(sendBuff);
    }

    void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
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
            () => { return _session; }
        , 1);
    }

    // Update is called once per frame
    void Update()
    {
        List<IPacket> list = PacketQueue.Instance.PopAll();
        foreach (IPacket packet in list)
            PacketManager.Instance.HandlePacket(_session, packet);
    }

    private void OnApplicationQuit()
    {
        if (_session != null)
            _session.Disconnect();
    }
}
