using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using ServerCore;

public enum PacketID
{
    C_TurnStartReady = 1, 
	S_TurnStart = 2, 
	C_CardSelect = 3, 
	S_CardSelectResult = 4, 
	C_TurnEnd = 5, 
	S_TurnEnd = 6, 
	C_UnitPlacement = 7, 
	S_UnitPlacementResult = 8, 
	S_UnitSpawn = 9, 
	S_UnitMove = 10, 
	S_UnitAttack = 11, 
	S_UnitDestroy = 12, 
	S_LifeUpdate = 13, 
	S_GameResult = 14, 
	C_PlayerMatchingReq = 15, 
	C_PlayerMatchingReqCancel = 16, 
	S_PlayerMatchingReqOk = 17, 
	S_MatchingSuccess = 18, 
	C_PlayerDeckInfo = 19, 
	S_PlayerDeckInfo = 20, 
	
}

public interface IPacket
{
	ushort Protocol { get; }
	void Deserialize(ArraySegment<byte> segment);
	ArraySegment<byte> Serialize();
}

public class C_TurnStartReady : IPacket
{
    public bool ready;

    public ushort Protocol { get { return (ushort)PacketID.C_TurnStartReady; } }

    public void Deserialize(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        this.ready = BitConverter.ToBoolean(s.Slice(count));
		count += sizeof(bool);
		
    }

    public ArraySegment<byte> Serialize()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)PacketID.C_TurnStartReady);
        count += sizeof(ushort);

        success &= BitConverter.TryWriteBytes(s.Slice(count), this.ready);
		count += sizeof(bool);
		

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false)
            return null;

        return SendBufferHelper.Close(count);
    }
}

public class S_TurnStart : IPacket
{
    public int turnNumber;
	public int turnTime;

    public ushort Protocol { get { return (ushort)PacketID.S_TurnStart; } }

    public void Deserialize(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        this.turnNumber = BitConverter.ToInt32(s.Slice(count));
		count += sizeof(int);
		this.turnTime = BitConverter.ToInt32(s.Slice(count));
		count += sizeof(int);
		
    }

    public ArraySegment<byte> Serialize()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)PacketID.S_TurnStart);
        count += sizeof(ushort);

        success &= BitConverter.TryWriteBytes(s.Slice(count), this.turnNumber);
		count += sizeof(int);
		success &= BitConverter.TryWriteBytes(s.Slice(count), this.turnTime);
		count += sizeof(int);
		

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false)
            return null;

        return SendBufferHelper.Close(count);
    }
}

public class C_CardSelect : IPacket
{
    public List<SelectedCardIds> selectedCardIdss = new List<SelectedCardIds>();
	
	public class SelectedCardIds
	{
	    public short cardId;
	
	    public void Deserialize(ReadOnlySpan<byte> s, ref ushort count)
	    {
	        this.cardId = BitConverter.ToInt16(s.Slice(count));
			count += sizeof(short);
			
	    }
	
	    public bool Serialize(Span<byte> s, ref ushort count)
	    {
	        bool success = true;
	        success &= BitConverter.TryWriteBytes(s.Slice(count), this.cardId);
			count += sizeof(short);
			
	        return success;
	    }
	}

    public ushort Protocol { get { return (ushort)PacketID.C_CardSelect; } }

    public void Deserialize(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        this.selectedCardIdss.Clear();
		ushort selectedCardIdsLen = BitConverter.ToUInt16(s.Slice(count));
		count += sizeof(ushort);
		for (int i = 0; i < selectedCardIdsLen; i++)
		{
		    SelectedCardIds selectedCardIds = new SelectedCardIds();
		    selectedCardIds.Deserialize(s, ref count);
		    selectedCardIdss.Add(selectedCardIds);
		}
		
    }

    public ArraySegment<byte> Serialize()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)PacketID.C_CardSelect);
        count += sizeof(ushort);

        success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)this.selectedCardIdss.Count);
		count += sizeof(ushort);
		
		foreach (SelectedCardIds selectedCardIds in selectedCardIdss)
		    success &= selectedCardIds.Serialize(s, ref count);
		

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false)
            return null;

        return SendBufferHelper.Close(count);
    }
}

public class S_CardSelectResult : IPacket
{
    public int playerId;
	public List<SelectedCardIds> selectedCardIdss = new List<SelectedCardIds>();
	
	public class SelectedCardIds
	{
	    public short cardId;
	
	    public void Deserialize(ReadOnlySpan<byte> s, ref ushort count)
	    {
	        this.cardId = BitConverter.ToInt16(s.Slice(count));
			count += sizeof(short);
			
	    }
	
	    public bool Serialize(Span<byte> s, ref ushort count)
	    {
	        bool success = true;
	        success &= BitConverter.TryWriteBytes(s.Slice(count), this.cardId);
			count += sizeof(short);
			
	        return success;
	    }
	}

    public ushort Protocol { get { return (ushort)PacketID.S_CardSelectResult; } }

    public void Deserialize(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        this.playerId = BitConverter.ToInt32(s.Slice(count));
		count += sizeof(int);
		this.selectedCardIdss.Clear();
		ushort selectedCardIdsLen = BitConverter.ToUInt16(s.Slice(count));
		count += sizeof(ushort);
		for (int i = 0; i < selectedCardIdsLen; i++)
		{
		    SelectedCardIds selectedCardIds = new SelectedCardIds();
		    selectedCardIds.Deserialize(s, ref count);
		    selectedCardIdss.Add(selectedCardIds);
		}
		
    }

    public ArraySegment<byte> Serialize()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)PacketID.S_CardSelectResult);
        count += sizeof(ushort);

        success &= BitConverter.TryWriteBytes(s.Slice(count), this.playerId);
		count += sizeof(int);
		success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)this.selectedCardIdss.Count);
		count += sizeof(ushort);
		
		foreach (SelectedCardIds selectedCardIds in selectedCardIdss)
		    success &= selectedCardIds.Serialize(s, ref count);
		

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false)
            return null;

        return SendBufferHelper.Close(count);
    }
}

public class C_TurnEnd : IPacket
{
    

    public ushort Protocol { get { return (ushort)PacketID.C_TurnEnd; } }

    public void Deserialize(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        
    }

    public ArraySegment<byte> Serialize()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)PacketID.C_TurnEnd);
        count += sizeof(ushort);

        

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false)
            return null;

        return SendBufferHelper.Close(count);
    }
}

public class S_TurnEnd : IPacket
{
    

    public ushort Protocol { get { return (ushort)PacketID.S_TurnEnd; } }

    public void Deserialize(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        
    }

    public ArraySegment<byte> Serialize()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)PacketID.S_TurnEnd);
        count += sizeof(ushort);

        

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false)
            return null;

        return SendBufferHelper.Close(count);
    }
}

public class C_UnitPlacement : IPacket
{
    public short cardId;
	public float x;
	public float y;

    public ushort Protocol { get { return (ushort)PacketID.C_UnitPlacement; } }

    public void Deserialize(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        this.cardId = BitConverter.ToInt16(s.Slice(count));
		count += sizeof(short);
		this.x = BitConverter.ToSingle(s.Slice(count));
		count += sizeof(float);
		this.y = BitConverter.ToSingle(s.Slice(count));
		count += sizeof(float);
		
    }

    public ArraySegment<byte> Serialize()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)PacketID.C_UnitPlacement);
        count += sizeof(ushort);

        success &= BitConverter.TryWriteBytes(s.Slice(count), this.cardId);
		count += sizeof(short);
		success &= BitConverter.TryWriteBytes(s.Slice(count), this.x);
		count += sizeof(float);
		success &= BitConverter.TryWriteBytes(s.Slice(count), this.y);
		count += sizeof(float);
		

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false)
            return null;

        return SendBufferHelper.Close(count);
    }
}

public class S_UnitPlacementResult : IPacket
{
    public int playerId;
	public short cardId;
	public float x;
	public float y;
	public bool isSuccess;
	public string errorMessage;

    public ushort Protocol { get { return (ushort)PacketID.S_UnitPlacementResult; } }

    public void Deserialize(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        this.playerId = BitConverter.ToInt32(s.Slice(count));
		count += sizeof(int);
		this.cardId = BitConverter.ToInt16(s.Slice(count));
		count += sizeof(short);
		this.x = BitConverter.ToSingle(s.Slice(count));
		count += sizeof(float);
		this.y = BitConverter.ToSingle(s.Slice(count));
		count += sizeof(float);
		this.isSuccess = BitConverter.ToBoolean(s.Slice(count));
		count += sizeof(bool);
		ushort errorMessageLen = BitConverter.ToUInt16(s.Slice(count));
		count += sizeof(ushort);
		this.errorMessage = Encoding.Unicode.GetString(s.Slice(count, errorMessageLen));
		count += errorMessageLen;
		
    }

    public ArraySegment<byte> Serialize()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)PacketID.S_UnitPlacementResult);
        count += sizeof(ushort);

        success &= BitConverter.TryWriteBytes(s.Slice(count), this.playerId);
		count += sizeof(int);
		success &= BitConverter.TryWriteBytes(s.Slice(count), this.cardId);
		count += sizeof(short);
		success &= BitConverter.TryWriteBytes(s.Slice(count), this.x);
		count += sizeof(float);
		success &= BitConverter.TryWriteBytes(s.Slice(count), this.y);
		count += sizeof(float);
		success &= BitConverter.TryWriteBytes(s.Slice(count), this.isSuccess);
		count += sizeof(bool);
		
		ushort errorMessageLen =
		    (ushort)Encoding.Unicode.GetBytes(this.errorMessage, s.Slice(count + sizeof(ushort)));
		success &= BitConverter.TryWriteBytes(s.Slice(count), errorMessageLen);
		count += sizeof(ushort);
		count += errorMessageLen;
		

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false)
            return null;

        return SendBufferHelper.Close(count);
    }
}

public class S_UnitSpawn : IPacket
{
    public int unitId;
	public int playerId;
	public short cardId;
	public float x;
	public float y;
	public int health;
	public int attack;
	public float moveSpeed;
	public short defense;
	public string specialEffect;
	public float attackSpeed;

    public ushort Protocol { get { return (ushort)PacketID.S_UnitSpawn; } }

    public void Deserialize(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        this.unitId = BitConverter.ToInt32(s.Slice(count));
		count += sizeof(int);
		this.playerId = BitConverter.ToInt32(s.Slice(count));
		count += sizeof(int);
		this.cardId = BitConverter.ToInt16(s.Slice(count));
		count += sizeof(short);
		this.x = BitConverter.ToSingle(s.Slice(count));
		count += sizeof(float);
		this.y = BitConverter.ToSingle(s.Slice(count));
		count += sizeof(float);
		this.health = BitConverter.ToInt32(s.Slice(count));
		count += sizeof(int);
		this.attack = BitConverter.ToInt32(s.Slice(count));
		count += sizeof(int);
		this.moveSpeed = BitConverter.ToSingle(s.Slice(count));
		count += sizeof(float);
		this.defense = BitConverter.ToInt16(s.Slice(count));
		count += sizeof(short);
		ushort specialEffectLen = BitConverter.ToUInt16(s.Slice(count));
		count += sizeof(ushort);
		this.specialEffect = Encoding.Unicode.GetString(s.Slice(count, specialEffectLen));
		count += specialEffectLen;
		this.attackSpeed = BitConverter.ToSingle(s.Slice(count));
		count += sizeof(float);
		
    }

    public ArraySegment<byte> Serialize()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)PacketID.S_UnitSpawn);
        count += sizeof(ushort);

        success &= BitConverter.TryWriteBytes(s.Slice(count), this.unitId);
		count += sizeof(int);
		success &= BitConverter.TryWriteBytes(s.Slice(count), this.playerId);
		count += sizeof(int);
		success &= BitConverter.TryWriteBytes(s.Slice(count), this.cardId);
		count += sizeof(short);
		success &= BitConverter.TryWriteBytes(s.Slice(count), this.x);
		count += sizeof(float);
		success &= BitConverter.TryWriteBytes(s.Slice(count), this.y);
		count += sizeof(float);
		success &= BitConverter.TryWriteBytes(s.Slice(count), this.health);
		count += sizeof(int);
		success &= BitConverter.TryWriteBytes(s.Slice(count), this.attack);
		count += sizeof(int);
		success &= BitConverter.TryWriteBytes(s.Slice(count), this.moveSpeed);
		count += sizeof(float);
		success &= BitConverter.TryWriteBytes(s.Slice(count), this.defense);
		count += sizeof(short);
		
		ushort specialEffectLen =
		    (ushort)Encoding.Unicode.GetBytes(this.specialEffect, s.Slice(count + sizeof(ushort)));
		success &= BitConverter.TryWriteBytes(s.Slice(count), specialEffectLen);
		count += sizeof(ushort);
		count += specialEffectLen;
		success &= BitConverter.TryWriteBytes(s.Slice(count), this.attackSpeed);
		count += sizeof(float);
		

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false)
            return null;

        return SendBufferHelper.Close(count);
    }
}

public class S_UnitMove : IPacket
{
    public int unitId;
	public float x;
	public float y;

    public ushort Protocol { get { return (ushort)PacketID.S_UnitMove; } }

    public void Deserialize(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        this.unitId = BitConverter.ToInt32(s.Slice(count));
		count += sizeof(int);
		this.x = BitConverter.ToSingle(s.Slice(count));
		count += sizeof(float);
		this.y = BitConverter.ToSingle(s.Slice(count));
		count += sizeof(float);
		
    }

    public ArraySegment<byte> Serialize()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)PacketID.S_UnitMove);
        count += sizeof(ushort);

        success &= BitConverter.TryWriteBytes(s.Slice(count), this.unitId);
		count += sizeof(int);
		success &= BitConverter.TryWriteBytes(s.Slice(count), this.x);
		count += sizeof(float);
		success &= BitConverter.TryWriteBytes(s.Slice(count), this.y);
		count += sizeof(float);
		

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false)
            return null;

        return SendBufferHelper.Close(count);
    }
}

public class S_UnitAttack : IPacket
{
    public int attackerId;
	public int targetId;
	public int damage;

    public ushort Protocol { get { return (ushort)PacketID.S_UnitAttack; } }

    public void Deserialize(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        this.attackerId = BitConverter.ToInt32(s.Slice(count));
		count += sizeof(int);
		this.targetId = BitConverter.ToInt32(s.Slice(count));
		count += sizeof(int);
		this.damage = BitConverter.ToInt32(s.Slice(count));
		count += sizeof(int);
		
    }

    public ArraySegment<byte> Serialize()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)PacketID.S_UnitAttack);
        count += sizeof(ushort);

        success &= BitConverter.TryWriteBytes(s.Slice(count), this.attackerId);
		count += sizeof(int);
		success &= BitConverter.TryWriteBytes(s.Slice(count), this.targetId);
		count += sizeof(int);
		success &= BitConverter.TryWriteBytes(s.Slice(count), this.damage);
		count += sizeof(int);
		

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false)
            return null;

        return SendBufferHelper.Close(count);
    }
}

public class S_UnitDestroy : IPacket
{
    public int unitId;

    public ushort Protocol { get { return (ushort)PacketID.S_UnitDestroy; } }

    public void Deserialize(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        this.unitId = BitConverter.ToInt32(s.Slice(count));
		count += sizeof(int);
		
    }

    public ArraySegment<byte> Serialize()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)PacketID.S_UnitDestroy);
        count += sizeof(ushort);

        success &= BitConverter.TryWriteBytes(s.Slice(count), this.unitId);
		count += sizeof(int);
		

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false)
            return null;

        return SendBufferHelper.Close(count);
    }
}

public class S_LifeUpdate : IPacket
{
    public int playerId;
	public int life;

    public ushort Protocol { get { return (ushort)PacketID.S_LifeUpdate; } }

    public void Deserialize(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        this.playerId = BitConverter.ToInt32(s.Slice(count));
		count += sizeof(int);
		this.life = BitConverter.ToInt32(s.Slice(count));
		count += sizeof(int);
		
    }

    public ArraySegment<byte> Serialize()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)PacketID.S_LifeUpdate);
        count += sizeof(ushort);

        success &= BitConverter.TryWriteBytes(s.Slice(count), this.playerId);
		count += sizeof(int);
		success &= BitConverter.TryWriteBytes(s.Slice(count), this.life);
		count += sizeof(int);
		

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false)
            return null;

        return SendBufferHelper.Close(count);
    }
}

public class S_GameResult : IPacket
{
    public int winnerId;
	public string reason;

    public ushort Protocol { get { return (ushort)PacketID.S_GameResult; } }

    public void Deserialize(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        this.winnerId = BitConverter.ToInt32(s.Slice(count));
		count += sizeof(int);
		ushort reasonLen = BitConverter.ToUInt16(s.Slice(count));
		count += sizeof(ushort);
		this.reason = Encoding.Unicode.GetString(s.Slice(count, reasonLen));
		count += reasonLen;
		
    }

    public ArraySegment<byte> Serialize()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)PacketID.S_GameResult);
        count += sizeof(ushort);

        success &= BitConverter.TryWriteBytes(s.Slice(count), this.winnerId);
		count += sizeof(int);
		
		ushort reasonLen =
		    (ushort)Encoding.Unicode.GetBytes(this.reason, s.Slice(count + sizeof(ushort)));
		success &= BitConverter.TryWriteBytes(s.Slice(count), reasonLen);
		count += sizeof(ushort);
		count += reasonLen;
		

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false)
            return null;

        return SendBufferHelper.Close(count);
    }
}

public class C_PlayerMatchingReq : IPacket
{
    

    public ushort Protocol { get { return (ushort)PacketID.C_PlayerMatchingReq; } }

    public void Deserialize(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        
    }

    public ArraySegment<byte> Serialize()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)PacketID.C_PlayerMatchingReq);
        count += sizeof(ushort);

        

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false)
            return null;

        return SendBufferHelper.Close(count);
    }
}

public class C_PlayerMatchingReqCancel : IPacket
{
    

    public ushort Protocol { get { return (ushort)PacketID.C_PlayerMatchingReqCancel; } }

    public void Deserialize(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        
    }

    public ArraySegment<byte> Serialize()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)PacketID.C_PlayerMatchingReqCancel);
        count += sizeof(ushort);

        

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false)
            return null;

        return SendBufferHelper.Close(count);
    }
}

public class S_PlayerMatchingReqOk : IPacket
{
    

    public ushort Protocol { get { return (ushort)PacketID.S_PlayerMatchingReqOk; } }

    public void Deserialize(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        
    }

    public ArraySegment<byte> Serialize()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)PacketID.S_PlayerMatchingReqOk);
        count += sizeof(ushort);

        

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false)
            return null;

        return SendBufferHelper.Close(count);
    }
}

public class S_MatchingSuccess : IPacket
{
    

    public ushort Protocol { get { return (ushort)PacketID.S_MatchingSuccess; } }

    public void Deserialize(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        
    }

    public ArraySegment<byte> Serialize()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)PacketID.S_MatchingSuccess);
        count += sizeof(ushort);

        

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false)
            return null;

        return SendBufferHelper.Close(count);
    }
}

public class C_PlayerDeckInfo : IPacket
{
    public List<Card> cards = new List<Card>();
	
	public class Card
	{
	    public short cardId;
		public bool isInDeck;
	
	    public void Deserialize(ReadOnlySpan<byte> s, ref ushort count)
	    {
	        this.cardId = BitConverter.ToInt16(s.Slice(count));
			count += sizeof(short);
			this.isInDeck = BitConverter.ToBoolean(s.Slice(count));
			count += sizeof(bool);
			
	    }
	
	    public bool Serialize(Span<byte> s, ref ushort count)
	    {
	        bool success = true;
	        success &= BitConverter.TryWriteBytes(s.Slice(count), this.cardId);
			count += sizeof(short);
			success &= BitConverter.TryWriteBytes(s.Slice(count), this.isInDeck);
			count += sizeof(bool);
			
	        return success;
	    }
	}

    public ushort Protocol { get { return (ushort)PacketID.C_PlayerDeckInfo; } }

    public void Deserialize(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        this.cards.Clear();
		ushort cardLen = BitConverter.ToUInt16(s.Slice(count));
		count += sizeof(ushort);
		for (int i = 0; i < cardLen; i++)
		{
		    Card card = new Card();
		    card.Deserialize(s, ref count);
		    cards.Add(card);
		}
		
    }

    public ArraySegment<byte> Serialize()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)PacketID.C_PlayerDeckInfo);
        count += sizeof(ushort);

        success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)this.cards.Count);
		count += sizeof(ushort);
		
		foreach (Card card in cards)
		    success &= card.Serialize(s, ref count);
		

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false)
            return null;

        return SendBufferHelper.Close(count);
    }
}

public class S_PlayerDeckInfo : IPacket
{
    public List<Card> cards = new List<Card>();
	
	public class Card
	{
	    public short cardId;
		public bool isInDeck;
	
	    public void Deserialize(ReadOnlySpan<byte> s, ref ushort count)
	    {
	        this.cardId = BitConverter.ToInt16(s.Slice(count));
			count += sizeof(short);
			this.isInDeck = BitConverter.ToBoolean(s.Slice(count));
			count += sizeof(bool);
			
	    }
	
	    public bool Serialize(Span<byte> s, ref ushort count)
	    {
	        bool success = true;
	        success &= BitConverter.TryWriteBytes(s.Slice(count), this.cardId);
			count += sizeof(short);
			success &= BitConverter.TryWriteBytes(s.Slice(count), this.isInDeck);
			count += sizeof(bool);
			
	        return success;
	    }
	}

    public ushort Protocol { get { return (ushort)PacketID.S_PlayerDeckInfo; } }

    public void Deserialize(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        this.cards.Clear();
		ushort cardLen = BitConverter.ToUInt16(s.Slice(count));
		count += sizeof(ushort);
		for (int i = 0; i < cardLen; i++)
		{
		    Card card = new Card();
		    card.Deserialize(s, ref count);
		    cards.Add(card);
		}
		
    }

    public ArraySegment<byte> Serialize()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)PacketID.S_PlayerDeckInfo);
        count += sizeof(ushort);

        success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)this.cards.Count);
		count += sizeof(ushort);
		
		foreach (Card card in cards)
		    success &= card.Serialize(s, ref count);
		

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false)
            return null;

        return SendBufferHelper.Close(count);
    }
}


