using Server;
using Server.Session;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

    public static void C_LeaveGameHandler(PacketSession session, IPacket packet)
    {
        //_logger.Log("Called", nameof(C_LeaveGameHandler));
        //C_LeaveGame leavePacket = packet as C_LeaveGame;
        //ClientSession clientSession = session as ClientSession;

        //if (clientSession.Room == null)
        //    return;

        //GameRoom room = clientSession.Room;
        //room.Push(() => room.Leave(clientSession));
    }

    public static void C_PlayerDeckInfoHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("Called", nameof(C_PlayerDeckInfoHandler));
        C_PlayerDeckInfo deckPacket = packet as C_PlayerDeckInfo;
        ClientSession clientSession = session as ClientSession;

        UserData userData = clientSession.UserData;

        for (int i = 0; i < deckPacket.cards.Count; i++)
        {
            userData.cards[i].isInDeck = deckPacket.cards[i].isInDeck;
        }

        UserDatas.Instance.SaveData(clientSession.SessionIP, userData, "");
    }

    public static void C_PlayerMatchingReqHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("Called", nameof(C_PlayerMatchingReqHandler));
        ClientSession clientSession = session as ClientSession;
        GameRoom room;
 
        if (Program.MatchingRoom.Count < 1)
            Program.MatchingRoom.Enqueue(new GameRoom());

        room = Program.MatchingRoom.Peek();
        room.Push(() => room.RequestMatch(clientSession));
    }

    public static void C_PlayerMatchingReqCancelHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("Called", nameof(C_PlayerMatchingReqCancelHandler));
        ClientSession clientSession = session as ClientSession;

        GameRoom room = Program.MatchingRoom.Peek();
        room.Push(() => room.CancelMatch(clientSession));
    }

    public static void C_TurnStartReadyHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("Called", nameof(C_TurnStartReadyHandler));
        ClientSession clientSession = session as ClientSession;
        C_TurnStartReady readyPacket = packet as C_TurnStartReady;

        if (clientSession?.Room == null || readyPacket == null)
            return;

        GameRoom room = clientSession.Room;
        room.Push(() => room.ReadyForTurn(clientSession, readyPacket.ready));
    }

    public static void C_CardSelectHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("Called", nameof(C_CardSelectHandler));
        ClientSession clientSession = session as ClientSession;
        C_CardSelect cardPacket = packet as C_CardSelect;

        if (clientSession?.Room == null || cardPacket == null)
            return;

        GameRoom room = clientSession.Room;
        room.Push(() => room.RelayCardSelection(clientSession, cardPacket));
    }

    public static void C_TurnEndHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("Called", nameof(C_TurnEndHandler));
        ClientSession clientSession = session as ClientSession;

        if (clientSession?.Room == null)
            return;

        GameRoom room = clientSession.Room;
        room.Push(() => room.EndTurnPreparation(clientSession));
    }

    public static void C_UnitPlacementHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("Called", nameof(C_UnitPlacementHandler));
        ClientSession clientSession = session as ClientSession;
        C_UnitPlacement placementPacket = packet as C_UnitPlacement;

        if (clientSession?.Room == null || placementPacket == null)
            return;

        GameRoom room = clientSession.Room;
        room.Push(() => room.RelayUnitPlacement(clientSession, placementPacket));
    }


    //public static void C_MoveHandler(PacketSession session, IPacket packet)
    //{
    //    C_Move movePacket = packet as C_Move;
    //    ClientSession clientSession = session as ClientSession;

    //    if (clientSession.Room == null)
    //        return;

    //    //Console.WriteLine($"{movePacket.posX}, {mov
    //    GameRoom room = clientSession.Room;ePacket.posY}, {movePacket.posZ}");

    //    room.Push(() => room.Move(clientSession, movePacket));
    //}
}
