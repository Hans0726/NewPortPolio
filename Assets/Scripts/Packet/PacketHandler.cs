using DummyClient;
using ServerCore;
using System.Security.Cryptography;
using UnityEngine;


class PacketHandler
{
    //public static void S_BroadcastEnterGameHandler(PacketSession session, IPacket packet)
    //{
    //    S_BroadcastEnterGame pkt = packet as S_BroadcastEnterGame;
    //    ServerSession serverSession = session as ServerSession;

    //    PlayerManager.Instance.EnterGame(pkt);
    //}

    //public static void S_BroadcastLeaveGameHandler(PacketSession session, IPacket packet)
    //{
    //    S_BroadcastLeaveGame pkt = packet as S_BroadcastLeaveGame;
    //    ServerSession serverSession = session as ServerSession;
    //    PlayerManager.Instance.LeaveGame(pkt);
    //}

    public static void S_PlayerDeckInfoHandler(PacketSession session, IPacket packet)
    {
        S_PlayerDeckInfo pkt = packet as S_PlayerDeckInfo;
        ServerSession serverSession = session as ServerSession;

        NetworkMananger.Instance.Gateway.PlayerDeckInfoHandle(pkt);
    }

    public static void S_PlayerMatchingReqOkHandler(PacketSession session, IPacket packet)
    {
        S_PlayerMatchingReqOk pkt = packet as S_PlayerMatchingReqOk;
        ServerSession serverSession = session as ServerSession;

        NetworkMananger.Instance.Gateway.MatchingRequestAcceptedHandle(pkt);
    }

    public static void S_MatchingSuccessHandler(PacketSession session, IPacket packet)
    {
        S_MatchingSuccess pkt = packet as S_MatchingSuccess;
        ServerSession serverSession = session as ServerSession;

        NetworkMananger.Instance.Gateway.MatchSuccessHandle(pkt);
    }


    public static void S_TurnStartHandler(PacketSession session, IPacket packet)
    {
        S_TurnStart pkt = packet as S_TurnStart;
        ServerSession serverSession = session as ServerSession;
        NetworkMananger.Instance.Gateway.TurnStartHandle(pkt);
    }
    public static void S_CardSelectResultHandler(PacketSession session, IPacket packet)
    {
        S_CardSelectResult pkt = packet as S_CardSelectResult;
        ServerSession serverSession = session as ServerSession;

        NetworkMananger.Instance.Gateway.CardSelectResultHandle(pkt);
    }

    public static void S_TurnEndHandler(PacketSession session, IPacket packet)
    {
        S_TurnEnd pkt = packet as S_TurnEnd;
        ServerSession serverSession = session as ServerSession;

        NetworkMananger.Instance.Gateway.TurnEndHandle(pkt);
    }

    public static void S_UnitPlacementResultHandler(PacketSession session, IPacket packet)
    {
        S_UnitPlacementResult pkt = packet as S_UnitPlacementResult;
        ServerSession serverSession = session as ServerSession;

        NetworkMananger.Instance.Gateway.UnitPlacementResultHandle(pkt);
    }

    public static void S_UnitSpawnHandler(PacketSession session, IPacket packet)
    {
        S_UnitSpawn pkt = packet as S_UnitSpawn;
        ServerSession serverSession = session as ServerSession;

        // 필요한 로직 추가 가능
    }

    public static void S_UnitMoveHandler(PacketSession session, IPacket packet)
    {
        S_UnitMove pkt = packet as S_UnitMove;
        ServerSession serverSession = session as ServerSession;

        // 필요한 로직 추가 가능
    }

    public static void S_UnitAttackHandler(PacketSession session, IPacket packet)
    {
        S_UnitAttack pkt = packet as S_UnitAttack;
        ServerSession serverSession = session as ServerSession;

        // 필요한 로직 추가 가능
    }

    public static void S_UnitDestroyHandler(PacketSession session, IPacket packet)
    {
        S_UnitDestroy pkt = packet as S_UnitDestroy;
        ServerSession serverSession = session as ServerSession;

        // 필요한 로직 추가 가능
    }

    public static void S_LifeUpdateHandler(PacketSession session, IPacket packet)
    {
        S_LifeUpdate pkt = packet as S_LifeUpdate;
        ServerSession serverSession = session as ServerSession;

        NetworkMananger.Instance.Gateway.LifeUpdateHandle(pkt);
    }

    public static void S_GameResultHandler(PacketSession session, IPacket packet)
    {
        S_GameResult pkt = packet as S_GameResult;
        ServerSession serverSession = session as ServerSession;

        NetworkMananger.Instance.Gateway.GameResultHandle(pkt);
    }

    //public static void S_PlayerListHandler(PacketSession session, IPacket packet)
    //{
    //    S_PlayerList pkt = packet as S_PlayerList;
    //    ServerSession serverSession = session as ServerSession;

    //    PlayerManager.Instance.Add(pkt);
    //}

    //public static void S_BroadcastMoveHandler(PacketSession session, IPacket packet)
    //{
    //    S_BroadcastMove pkt = packet as S_BroadcastMove;
    //    ServerSession serverSession = session as ServerSession;

    //    PlayerManager.Instance.Move(pkt);
    //}
}
