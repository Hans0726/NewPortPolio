using System;
using System.Xml;

namespace PacketGenerator
{
    class Program
    {
        static string genPackets;

        static ushort packetId;
        static string packetEnums;

        static string clientRegister;
        static string serverRegister;

        static void Main(string[] args)
        {
            string baseDirectory = AppContext.BaseDirectory;
            string pdlPath = Path.GetFullPath(Path.Combine(baseDirectory, "..", "PDL"));
            string outputPath = baseDirectory;

            // 명령줄 인자로 폴더 경로를 받을 수 있도록 함
            if (args.Length >= 1)
                pdlPath = Path.GetFullPath(args[0]);
            if (args.Length >= 2)
                outputPath = Path.GetFullPath(args[1]);

            if (!Directory.Exists(pdlPath))
            {
                Console.WriteLine($"Packet definition folder not found: {pdlPath}");
                return;
            }

            // 폴더 내 모든 .xml 파일 찾기
            string[] pdlFiles = Directory.GetFiles(pdlPath, "*.xml");

            XmlReaderSettings settings = new XmlReaderSettings()
            {
                IgnoreComments = true,
                IgnoreWhitespace = true
            };

            // 모든 XML 파일을 순회하며 파싱 결과 누적
            foreach (string pdlfile in pdlFiles)
            {
                Console.WriteLine($"Parsing {pdlfile}...");
                using (XmlReader r = XmlReader.Create(pdlfile, settings))
                {
                    r.MoveToContent();

                    while (r.Read())
                    {
                        if (r.Depth == 1 && r.NodeType == XmlNodeType.Element)
                            ParsePacket(r);
                    }
                }
            }

            // 모든 파싱 완료 후 파일 생성 (한 번만 실행)
            if (!string.IsNullOrEmpty(genPackets)) // 파싱된 내용이 있을 경우에만 파일 생성
            {
                string fileText = string.Format(PacketFormat.fileFormat, packetEnums, genPackets);
                Directory.CreateDirectory(outputPath);
                File.WriteAllText(Path.Combine(outputPath, "GenPackets.cs"), fileText);
                Console.WriteLine("Generated GenPackets.cs");

                string clientManagerText = string.Format(PacketFormat.managerFormat, clientRegister);
                File.WriteAllText(Path.Combine(outputPath, "ClientPacketManager.cs"), clientManagerText);
                Console.WriteLine("Generated ClientPacketManager.cs");

                string serverManagerText = string.Format(PacketFormat.managerFormat, serverRegister);
                File.WriteAllText(Path.Combine(outputPath, "ServerPacketManager.cs"), serverManagerText);
                Console.WriteLine("Generated ServerPacketManager.cs");
            }
        }

        public static void ParsePacket(XmlReader r)
        {
            if (r.NodeType == XmlNodeType.EndElement)
                return;

            if (r.Name.ToLower() != "packet")
            {
                Console.WriteLine("Invalid id packet node");
                return;
            }

            string packetName = r["name"];
            if (string.IsNullOrEmpty(packetName))
            {
                Console.WriteLine("Packet without name");
                return;
            }

            Tuple<string, string, string> t = ParseMembers(r);
            genPackets += string.Format(PacketFormat.packetFormat, packetName, t.Item1, t.Item2, t.Item3);
            packetEnums += string.Format(PacketFormat.packetEnumFormat, packetName, ++packetId) + Environment.NewLine + "\t";

            if (packetName.StartsWith("S_") || packetName.StartsWith("s_"))
                clientRegister += string.Format(PacketFormat.managerRegisterFormat, packetName) + Environment.NewLine;
            else
                serverRegister += string.Format(PacketFormat.managerRegisterFormat, packetName) + Environment.NewLine;
        }

        // {1} 멤버 변수
        // {2} 멤버 변수 직렬화
        // {3} 멤버 변수 역직렬화
        public static Tuple<string, string, string> ParseMembers(XmlReader r)
        {
            string memberCode = "";
            string serializeCode = "";
            string deserializeCode = "";

            int depth = r.Depth + 1;
            while (r.Read())
            {
                if (r.Depth != depth)
                    break;

                string memberName = r["name"];
                if (string.IsNullOrEmpty(memberName))
                {
                    Console.WriteLine("Member without name");
                    return null;
                }

                if (string.IsNullOrEmpty(memberCode) == false)
                    memberCode += Environment.NewLine;

                string memberType = r.Name.ToLower();
                switch (memberType)
                {
                    case "byte":
                    case "sbyte":
                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
                        deserializeCode += string.Format(PacketFormat.deserializeByteFormat, memberName, memberType);
                        serializeCode += string.Format(PacketFormat.serializeByteFormat, memberName, memberType);
                        break;
                    case "bool":
                    case "short":
                    case "ushort":
                    case "int":
                    case "long":
                    case "float":
                    case "double":
                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
                        deserializeCode += string.Format(PacketFormat.deserializeFormat, memberName, ToMemberType(memberType), memberType);
                        serializeCode += string.Format(PacketFormat.serializeFormat, memberName, memberType);
                        break;
                    case "string":
                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
                        deserializeCode += string.Format(PacketFormat.deserializeStringFormat, memberName);
                        serializeCode += string.Format(PacketFormat.serializeStringFormat, memberName);
                        break;
                    case "list":
                        Tuple<string, string, string> t = ParseList(r);
                        memberCode += t.Item1;
                        deserializeCode += t.Item2;
                        serializeCode += t.Item3;
                        break;
                    default:
                        break;
                }
            }
            memberCode = memberCode.Replace("\n", "\n\t");
            deserializeCode = deserializeCode.Replace("\n", "\n\t\t");
            serializeCode = serializeCode.Replace("\n", "\n\t\t");
            return new Tuple<string, string, string>(memberCode, deserializeCode, serializeCode);
        }

        public static Tuple<string, string, string> ParseList(XmlReader r)
        {
            string listName = r["name"];
            if (string.IsNullOrEmpty(listName))
            {
                Console.WriteLine("List without name");
                return null;
            }

            Tuple<string, string, string> t = ParseMembers(r);

            string memberCode = string.Format(
                PacketFormat.memberListFormat, 
                FirstCharToUpper(listName),
                FirstCharToLower(listName), 
                t.Item1,
                t.Item2,
                t.Item3);

            string serializeListCode = string.Format(PacketFormat.serializeListFormat,
                FirstCharToUpper(listName),
                FirstCharToLower(listName));

            string deserializeListCode = string.Format(PacketFormat.deserializeListFormat,
                FirstCharToUpper(listName),
                FirstCharToLower(listName));


            return new Tuple<string, string, string>(memberCode, deserializeListCode, serializeListCode);
        }

        public static string ToMemberType(string memberType)
        {
            switch (memberType)
            {
                case "bool":
                    return "ToBoolean";
                case "short":
                    return "ToInt16";
                case "ushort":
                    return "ToUInt16";
                case "int":
                    return "ToInt32";
                case "long":
                    return "ToInt64";
                case "float":
                    return "ToSingle";
                case "double":
                    return "ToDouble";
                default:
                    return "";
            }
        }

        public static string FirstCharToUpper(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "";
            return input[0].ToString().ToUpper() + input.Substring(1);
        }

        public static string FirstCharToLower(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "";
            return input[0].ToString().ToLower() + input.Substring(1);
        }
    }
}
