@ECHO OFF
SETLOCAL

SET "PACKET_DIR=%~dp0"
SET "GENERATOR=%PACKET_DIR%..\..\PacketGenerator\bin\PacketGenerator.exe"
SET "PDL_DIR=%PACKET_DIR%..\..\PacketGenerator\PDL"

"%GENERATOR%" "%PDL_DIR%" "%PACKET_DIR%."
IF NOT "%ERRORLEVEL%"=="0" EXIT /B %ERRORLEVEL%

XCOPY /Y "%PACKET_DIR%GenPackets.cs" "%PACKET_DIR%..\..\DummyClient\Packet\"
XCOPY /Y "%PACKET_DIR%GenPackets.cs" "%PACKET_DIR%..\..\..\Assets\Scripts\Packet\"
XCOPY /Y "%PACKET_DIR%GenPackets.cs" "%PACKET_DIR%..\..\Server\Packet\"
XCOPY /Y "%PACKET_DIR%ClientPacketManager.cs" "%PACKET_DIR%..\..\DummyClient\Packet\"
XCOPY /Y "%PACKET_DIR%ClientPacketManager.cs" "%PACKET_DIR%..\..\..\Assets\Scripts\Packet\"
XCOPY /Y "%PACKET_DIR%ServerPacketManager.cs" "%PACKET_DIR%..\..\Server\Packet\"

ENDLOCAL
