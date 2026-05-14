using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacketGenerator
{
    class PacketFormat
    {
        // {0} 패킷 등록
        public static string managerFormat =
@"using ServerCore;
using System;
using System.Collections.Generic;

public class PacketManager
{{
    #region Singleton
    static PacketManager _instance = new PacketManager();

    public static PacketManager Instance{{ get {{ return _instance;}} }}
    #endregion

    PacketManager() {{ Register(); }}

    Dictionary<ushort, Func<PacketSession, ArraySegment<byte>, IPacket>> _makeFunc = new Dictionary<ushort, Func<PacketSession, ArraySegment<byte>, IPacket>>();
    Dictionary<ushort, Action<PacketSession, IPacket>> _handler = new Dictionary<ushort, Action<PacketSession, IPacket>>();
    
    public void Register()
    {{
{0}
    }}

    public void OnRecvPacket(PacketSession session, ArraySegment<byte> buffer, Action <PacketSession, IPacket> onRecvCallback = null)
    {{
        ushort count = 0;

        ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
        count += 2;
        ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
        count += 2;

        Func<PacketSession, ArraySegment<byte>, IPacket> func = null;
        if (_makeFunc.TryGetValue(id, out func))
        {{
            IPacket packet = func.Invoke(session, buffer);
            if (onRecvCallback != null)
                onRecvCallback.Invoke(session, packet);
            else
                HandlePacket(session, packet);
        }}
    }}

    T MakePacket<T>(PacketSession session, ArraySegment<byte> buffer) where T : IPacket, new()
    {{
        T packet = new T();
        packet.Deserialize(buffer);
        return packet;
    }}

    public void HandlePacket(PacketSession session, IPacket packet)
    {{
        Action<PacketSession, IPacket> action = null;
        if (_handler.TryGetValue(packet.Protocol, out action) == true)
            action.Invoke(session, packet);
    }}
}}
";

        // {0} 패킷 이름
        public static string managerRegisterFormat =
@"
        _makeFunc.Add((ushort)PacketID.{0}, MakePacket<{0}>);
        _handler.Add((ushort)PacketID.{0}, PacketHandler.{0}Handler);";

        // {0} 패킷 이름/번호 목록
        // {1} 패킷 목록
        public static string fileFormat =
@"using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using ServerCore;

public enum PacketID
{{
    {0}
}}

public interface IPacket
{{
	ushort Protocol {{ get; }}
	void Deserialize(ArraySegment<byte> segment);
	ArraySegment<byte> Serialize();
}}

{1}
";
        // {0} 패킷 이름
        // {1} 패킷 번호
        public static string packetEnumFormat =
@"{0} = {1}, ";


        // {0} 패킷 이름
        // {1} 멤버 변수
        // {2} 멤버 변수 역직렬화
        // {3} 멤버 변수 직렬화
        public static string packetFormat =
@"public class {0} : IPacket
{{
    {1}

    public ushort Protocol {{ get {{ return (ushort)PacketID.{0}; }} }}

    public void Deserialize(ArraySegment<byte> segment)
    {{
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        {2}
    }}

    public ArraySegment<byte> Serialize()
    {{
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)PacketID.{0});
        count += sizeof(ushort);

        {3}

        success &= BitConverter.TryWriteBytes(s, count);

        if (success == false)
            return null;

        return SendBufferHelper.Close(count);
    }}
}}

";

        // {0} 변수 타입
        // {1} 변수 이름
        public static string memberFormat =
@"public {0} {1};";

        // {0} 리스트 이름 [대문자]
        // {1} 리스트 이름 [소문자]
        // {2} 멤버 변수들
        // {3} 멤버 변수 역직렬화
        // {4} 멤버 변수 직렬화
        public static string memberListFormat =
@"public List<{0}> {1}s = new List<{0}>();

public class {0}
{{
    {2}

    public void Deserialize(ReadOnlySpan<byte> s, ref ushort count)
    {{
        {3}
    }}

    public bool Serialize(Span<byte> s, ref ushort count)
    {{
        bool success = true;
        {4}
        return success;
    }}
}}";

        // {0} 변수 이름
        // {1} To~ 변수 타입
        // {2} 변수 타입
        public static string deserializeFormat =
@"this.{0} = BitConverter.{1}(s.Slice(count));
count += sizeof({2});
";

        // {0} 변수 이름
        // {1} 변수 타입
        public static string deserializeByteFormat =
@"this.{0} = ({1})segment.Array[segment.Offset + count];
count += sizeof({1});";

        // {0} 변수 이름
        public static string deserializeStringFormat =
@"ushort {0}Len = BitConverter.ToUInt16(s.Slice(count));
count += sizeof(ushort);
this.{0} = Encoding.Unicode.GetString(s.Slice(count, {0}Len));
count += {0}Len;
";

        // {0} 리스트 이름 [대문자]
        // {1} 리스트 이름 [소문자]
        public static string deserializeListFormat =
@"this.{1}s.Clear();
ushort {1}Len = BitConverter.ToUInt16(s.Slice(count));
count += sizeof(ushort);
for (int i = 0; i < {1}Len; i++)
{{
    {0} {1} = new {0}();
    {1}.Deserialize(s, ref count);
    {1}s.Add({1});
}}
";

        // {0} 변수 이름
        // {1} 변수 타입
        public static string serializeFormat =
@"success &= BitConverter.TryWriteBytes(s.Slice(count), this.{0});
count += sizeof({1});
";

        // {0} 변수 이름
        // {1} 변수 타입
        public static string serializeByteFormat =
@"segment.Array[segment.Offset + count] = (byte)this.{0};
count += sizeof({1});
";

        // {0} 변수 이름
        public static string serializeStringFormat =
@"
ushort {0}Len =
    (ushort)Encoding.Unicode.GetBytes(this.{0}, s.Slice(count + sizeof(ushort)));
success &= BitConverter.TryWriteBytes(s.Slice(count), {0}Len);
count += sizeof(ushort);
count += {0}Len;
";

        // {0} 리스트 이름 [대문자]
        // {1} 리스트 이름 [소문자]
        public static string serializeListFormat =
@"success &= BitConverter.TryWriteBytes(s.Slice(count), (ushort)this.{1}s.Count);
count += sizeof(ushort);

foreach ({0} {1} in {1}s)
    success &= {1}.Serialize(s, ref count);
";

        // {0} 패킷 이름
        public static string handlerFormat =
@"
using DummyClient;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


static class PacketHandler
{{
    static PacketHandler()
    {{
        _cwLog = new ConsoleLogger();
        SetLogger(_cwLog);
    }}
    private static IPacketHandlerLogger _cwLog;
    private static IPacketHandlerLogger _logger;

    static void SetLogger(IPacketHandlerLogger logger) {{ _logger = logger; }}

    public static void S_{0}Handler(PacketSession session, IPacket packet)
    {{
        _logger.Log(""[Dummy] Called"", nameof(S_{0}Handler));
        S_{0} pkt = packet as S_{0};
        ServerSession serverSession = session as ServerSession;
    }}
}}
";
    }
}
