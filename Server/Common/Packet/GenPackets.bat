START ../../PacketGenerator/bin/PacketGenerator.exe ../../PacketGenerator/PDL
XCOPY /Y GenPackets.cs "../../DummyClient/Packet"
XCOPY /Y GenPackets.cs "../../../../../NewPortPolio/Assets/Scripts/Packet"
XCOPY /Y GenPackets.cs "../../Server/Packet"
XCOPY /Y ClientPacketManager.cs "../../DummyClient/Packet"
XCOPY /Y ClientPacketManager.cs "../../../../../NewPortPolio/Assets/Scripts/Packet"
XCOPY /Y ServerPacketManager.cs "../../Server/Packet"