using Server;
using Server.Session;
using ServerCore;


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
        _logger.Log("Called", nameof(C_LeaveGameHandler));
        C_LeaveGame leavePacket = packet as C_LeaveGame;
        ClientSession clientSession = session as ClientSession;
        clientSession.Dispose();
    }

    public static void C_PlayerInfoHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("Called", nameof(C_PlayerInfoHandler));
        C_PlayerInfo deckPacket = packet as C_PlayerInfo;
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

        lock (Program.RoomsLock)
        {
            if (Program.MatchingRooms.Count == 0)
            {
                Program.MatchingRooms.Enqueue(new GameRoom());
            }

            room = Program.MatchingRooms.Peek();
        }

        room.Push(() => room.RequestMatch(clientSession));
    }

    public static void C_PlayerMatchingReqCancelHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("Called", nameof(C_PlayerMatchingReqCancelHandler));
        ClientSession clientSession = session as ClientSession;

        GameRoom room = clientSession.Room;
        if (room == null)
            return;

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

    public static void C_LifeUpdateHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("Called", nameof(C_LifeUpdateHandler));
        ClientSession clientSession = session as ClientSession;
        C_LifeUpdate lifeUpdatePacket = packet as C_LifeUpdate;

        if (clientSession?.Room == null || lifeUpdatePacket == null)
            return;

        GameRoom room = clientSession.Room;
        room.Push(() => room.RelayLifeUpdate(clientSession, lifeUpdatePacket));
    }

    public static void C_UnitDestroyHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("Called", nameof(C_UnitDestroyHandler));
        ClientSession clientSession = session as ClientSession;
        C_UnitDestroy destroyPacket = packet as C_UnitDestroy;

        if (clientSession?.Room == null || destroyPacket == null)
            return;

        GameRoom room = clientSession.Room;
        room.Push(() => room.RelayUnitDestroy(clientSession, destroyPacket));
    }

    public static void C_DefenseTargetHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("Called", nameof(C_DefenseTargetHandler));
        ClientSession clientSession = session as ClientSession;
        C_DefenseTarget targetPacket = packet as C_DefenseTarget;

        if (clientSession?.Room == null || targetPacket == null)
            return;

        GameRoom room = clientSession.Room;
        room.Push(() => room.RelayDefenseTarget(clientSession, targetPacket));
    }

    public static void C_UnitHealthHandler(PacketSession session, IPacket packet)
    {
        _logger.Log("Called", nameof(C_UnitHealthHandler));
        ClientSession clientSession = session as ClientSession;
        C_UnitHealth healthPacket = packet as C_UnitHealth;

        if (clientSession?.Room == null || healthPacket == null)
            return;

        GameRoom room = clientSession.Room;
        room.Push(() => room.RelayUnitHealth(clientSession, healthPacket));
    }

    public static void C_UnitMoveHandler(PacketSession session, IPacket packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_UnitMove movePacket = packet as C_UnitMove;
        if (clientSession?.Room == null || movePacket == null) return;

        GameRoom room = clientSession.Room;
        room.Push(() => room.RelayUnitMovement(clientSession, movePacket));
    }

    public static void C_UnitAttackHandler(PacketSession session, IPacket packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_UnitAttack attackPacket = packet as C_UnitAttack;
        if (clientSession?.Room == null || attackPacket == null) return;

        GameRoom room = clientSession.Room;
        room.Push(() => room.RelayUnitAttack(clientSession, attackPacket));
    }

    public static void C_UnitReachedDestinationHandler(PacketSession session, IPacket packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_UnitReachedDestination destinationPacket = packet as C_UnitReachedDestination;
        if (clientSession?.Room == null || destinationPacket == null) return;

        GameRoom room = clientSession.Room;
        room.Push(() => room.RelayUnitReachedDestination(clientSession, destinationPacket));
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
