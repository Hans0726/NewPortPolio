using DummyClient;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


static class PacketHandler
{
    static PacketHandler()
    {
        _cwLog = new ConsoleLogger();
        SetLogger(_cwLog);
    }
    private static IPacketHandlerLogger _cwLog;
    private static IPacketHandlerLogger _logger;

    static void SetLogger(IPacketHandlerLogger logger) { _logger = logger; }

    //public static void S_BroadcastEnterGameHandler(PacketSession session, IPacket packet)
    //{
    //    S_BroadcastEnterGame chatPacket = packet as S_BroadcastEnterGame;
    //    ServerSession serverSession = session as ServerSession;
    //}

    public static void S_BroadcastLeaveGameHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("[Dummy] Called", nameof(S_BroadcastLeaveGameHandler));
        S_BroadcastLeaveGame pkt = packet as S_BroadcastLeaveGame;
        ServerSession serverSession = session as ServerSession;
    }

    public static void S_PlayerInfoHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("[Dummy] Called", nameof(S_PlayerInfoHandler));
        S_PlayerInfo pkt = packet as S_PlayerInfo;
        ServerSession serverSession = session as ServerSession;
    }  

    public static void S_PlayerMatchingReqOkHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("[Dummy] Called", nameof(S_PlayerMatchingReqOkHandler));
        S_PlayerMatchingReqOk pkt = packet as S_PlayerMatchingReqOk;
        ServerSession serverSession = session as ServerSession;
    }

    public static void S_MatchingSuccessHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("[Dummy] Called", nameof(S_MatchingSuccessHandler));
        S_MatchingSuccess pkt = packet as S_MatchingSuccess;
        ServerSession serverSession = session as ServerSession;
    }

    //public static void S_PlayerListHandler(PacketSession session, IPacket packet)
    //{
    //    S_PlayerList chatPacket = packet as S_PlayerList;
    //    ServerSession serverSession = session as ServerSession;
    //}

    //public static void S_BroadcastMoveHandler(PacketSession session, IPacket packet)
    //{
    //    S_BroadcastMove chatPacket = packet as S_BroadcastMove;
    //    ServerSession serverSession = session as ServerSession;
    //}

    public static void S_TurnStartHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("[Dummy] Called", nameof(S_TurnStartHandler));
        S_TurnStart pkt = packet as S_TurnStart;
        ServerSession serverSession = session as ServerSession;
    }

    public static void S_CardSelectResultHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("[Dummy] Called", nameof(S_CardSelectResultHandler));
        S_CardSelectResult pkt = packet as S_CardSelectResult;
        ServerSession serverSession = session as ServerSession;
    }

    public static void S_TurnEndHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("[Dummy] Called", nameof(S_TurnEndHandler));
        S_TurnEnd pkt = packet as S_TurnEnd;
        ServerSession serverSession = session as ServerSession;
    }

    public static void S_UnitPlacementResultHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("[Dummy] Called", nameof(S_UnitPlacementResultHandler));
        S_UnitPlacementResult pkt = packet as S_UnitPlacementResult;
        ServerSession serverSession = session as ServerSession;
    }

    public static void S_UnitSpawnHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("[Dummy] Called", nameof(S_UnitSpawnHandler));
        S_UnitSpawn pkt = packet as S_UnitSpawn;
        ServerSession serverSession = session as ServerSession;
    }

    public static void S_UnitMoveHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("[Dummy] Called", nameof(S_UnitMoveHandler));
        S_UnitMove pkt = packet as S_UnitMove;
        ServerSession serverSession = session as ServerSession;
    }

    public static void S_UnitAttackHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("[Dummy] Called", nameof(S_UnitAttackHandler));
        S_UnitAttack pkt = packet as S_UnitAttack;
        ServerSession serverSession = session as ServerSession;
    }

    public static void S_UnitDestroyHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("[Dummy] Called", nameof(S_UnitDestroyHandler));
        S_UnitDestroy pkt = packet as S_UnitDestroy;
        ServerSession serverSession = session as ServerSession;
    }

    public static void S_DefenseTargetHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("[Dummy] Called", nameof(S_DefenseTargetHandler));
        S_DefenseTarget pkt = packet as S_DefenseTarget;
    }

    public static void S_UnitHealthHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("[Dummy] Called", nameof(S_UnitHealthHandler));
        S_UnitHealth pkt = packet as S_UnitHealth;
    }

    public static void S_UnitReachedDestinationHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("[Dummy] Called", nameof(S_UnitReachedDestinationHandler));
        S_UnitReachedDestination pkt = packet as S_UnitReachedDestination;
    }

    public static void S_GameResultHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("[Dummy] Called", nameof(S_GameResultHandler));
        S_GameResult pkt = packet as S_GameResult;
        ServerSession serverSession = session as ServerSession;
    }
}
