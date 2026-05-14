using ServerCore;
using System;
using System.Collections.Generic;

public class PacketManager
{
    #region Singleton
    static PacketManager _instance = new PacketManager();

    public static PacketManager Instance{ get { return _instance;} }
    #endregion

    PacketManager() { Register(); }

    Dictionary<ushort, Func<PacketSession, ArraySegment<byte>, IPacket>> _makeFunc = new Dictionary<ushort, Func<PacketSession, ArraySegment<byte>, IPacket>>();
    Dictionary<ushort, Action<PacketSession, IPacket>> _handler = new Dictionary<ushort, Action<PacketSession, IPacket>>();
    
    public void Register()
    {

        _makeFunc.Add((ushort)PacketID.C_TurnStartReady, MakePacket<C_TurnStartReady>);
        _handler.Add((ushort)PacketID.C_TurnStartReady, PacketHandler.C_TurnStartReadyHandler);

        _makeFunc.Add((ushort)PacketID.C_CardSelect, MakePacket<C_CardSelect>);
        _handler.Add((ushort)PacketID.C_CardSelect, PacketHandler.C_CardSelectHandler);

        _makeFunc.Add((ushort)PacketID.C_TurnEnd, MakePacket<C_TurnEnd>);
        _handler.Add((ushort)PacketID.C_TurnEnd, PacketHandler.C_TurnEndHandler);

        _makeFunc.Add((ushort)PacketID.C_UnitPlacement, MakePacket<C_UnitPlacement>);
        _handler.Add((ushort)PacketID.C_UnitPlacement, PacketHandler.C_UnitPlacementHandler);

        _makeFunc.Add((ushort)PacketID.C_PlayerMatchingReq, MakePacket<C_PlayerMatchingReq>);
        _handler.Add((ushort)PacketID.C_PlayerMatchingReq, PacketHandler.C_PlayerMatchingReqHandler);

        _makeFunc.Add((ushort)PacketID.C_PlayerMatchingReqCancel, MakePacket<C_PlayerMatchingReqCancel>);
        _handler.Add((ushort)PacketID.C_PlayerMatchingReqCancel, PacketHandler.C_PlayerMatchingReqCancelHandler);

        _makeFunc.Add((ushort)PacketID.C_PlayerDeckInfo, MakePacket<C_PlayerDeckInfo>);
        _handler.Add((ushort)PacketID.C_PlayerDeckInfo, PacketHandler.C_PlayerDeckInfoHandler);

    }

    public void OnRecvPacket(PacketSession session, ArraySegment<byte> buffer, Action <PacketSession, IPacket> onRecvCallback = null)
    {
        ushort count = 0;

        ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
        count += 2;
        ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
        count += 2;

        Func<PacketSession, ArraySegment<byte>, IPacket> func = null;
        if (_makeFunc.TryGetValue(id, out func))
        {
            IPacket packet = func.Invoke(session, buffer);
            if (onRecvCallback != null)
                onRecvCallback.Invoke(session, packet);
            else
                HandlePacket(session, packet);
        }
    }

    T MakePacket<T>(PacketSession session, ArraySegment<byte> buffer) where T : IPacket, new()
    {
        T packet = new T();
        packet.Deserialize(buffer);
        return packet;
    }

    public void HandlePacket(PacketSession session, IPacket packet)
    {
        Action<PacketSession, IPacket> action = null;
        if (_handler.TryGetValue(packet.Protocol, out action) == true)
            action.Invoke(session, packet);
    }
}
