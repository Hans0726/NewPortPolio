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

        _makeFunc.Add((ushort)PacketID.S_TurnStart, MakePacket<S_TurnStart>);
        _handler.Add((ushort)PacketID.S_TurnStart, PacketHandler.S_TurnStartHandler);

        _makeFunc.Add((ushort)PacketID.S_CardSelectResult, MakePacket<S_CardSelectResult>);
        _handler.Add((ushort)PacketID.S_CardSelectResult, PacketHandler.S_CardSelectResultHandler);

        _makeFunc.Add((ushort)PacketID.S_TurnEnd, MakePacket<S_TurnEnd>);
        _handler.Add((ushort)PacketID.S_TurnEnd, PacketHandler.S_TurnEndHandler);

        _makeFunc.Add((ushort)PacketID.S_UnitPlacementResult, MakePacket<S_UnitPlacementResult>);
        _handler.Add((ushort)PacketID.S_UnitPlacementResult, PacketHandler.S_UnitPlacementResultHandler);

        _makeFunc.Add((ushort)PacketID.S_UnitSpawn, MakePacket<S_UnitSpawn>);
        _handler.Add((ushort)PacketID.S_UnitSpawn, PacketHandler.S_UnitSpawnHandler);

        _makeFunc.Add((ushort)PacketID.S_UnitMove, MakePacket<S_UnitMove>);
        _handler.Add((ushort)PacketID.S_UnitMove, PacketHandler.S_UnitMoveHandler);

        _makeFunc.Add((ushort)PacketID.S_UnitAttack, MakePacket<S_UnitAttack>);
        _handler.Add((ushort)PacketID.S_UnitAttack, PacketHandler.S_UnitAttackHandler);

        _makeFunc.Add((ushort)PacketID.S_UnitDestroy, MakePacket<S_UnitDestroy>);
        _handler.Add((ushort)PacketID.S_UnitDestroy, PacketHandler.S_UnitDestroyHandler);

        _makeFunc.Add((ushort)PacketID.S_LifeUpdate, MakePacket<S_LifeUpdate>);
        _handler.Add((ushort)PacketID.S_LifeUpdate, PacketHandler.S_LifeUpdateHandler);

        _makeFunc.Add((ushort)PacketID.S_GameResult, MakePacket<S_GameResult>);
        _handler.Add((ushort)PacketID.S_GameResult, PacketHandler.S_GameResultHandler);

        _makeFunc.Add((ushort)PacketID.S_PlayerMatchingReqOk, MakePacket<S_PlayerMatchingReqOk>);
        _handler.Add((ushort)PacketID.S_PlayerMatchingReqOk, PacketHandler.S_PlayerMatchingReqOkHandler);

        _makeFunc.Add((ushort)PacketID.S_MatchingSuccess, MakePacket<S_MatchingSuccess>);
        _handler.Add((ushort)PacketID.S_MatchingSuccess, PacketHandler.S_MatchingSuccessHandler);

        _makeFunc.Add((ushort)PacketID.S_PlayerDeckInfo, MakePacket<S_PlayerDeckInfo>);
        _handler.Add((ushort)PacketID.S_PlayerDeckInfo, PacketHandler.S_PlayerDeckInfoHandler);

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
