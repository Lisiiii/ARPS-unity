namespace radar.serial.package
{
    public static class Constants
    {
        public const int FrameDataMaxLength = 512;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public struct FrameHeader
    {
        public byte SOF;
        public ushort DataLength;
        public byte Sequence;
        public byte Crc8;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public struct FrameBody
    {
        public ushort CommandId;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = Constants.FrameDataMaxLength)]
        public byte[] Data;
        public ushort Crc16;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public struct Frame
    {
        public FrameHeader Header;
        public FrameBody Body;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public struct GameStatus
    {
        public byte GameTypeAndStage; // Combine game_type (4 bits) and game_stage (4 bits)
        public ushort StageRemainTime;
        public ulong SyncTimestamp;

        public byte GameType => (byte)(GameTypeAndStage & 0x0F);
        public byte GameStage => (byte)((GameTypeAndStage >> 4) & 0x0F);
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public struct GameRobotHp
    {
        public ushort Red1;
        public ushort Red2;
        public ushort Red3;
        public ushort Red4;
        public ushort Red5;
        public ushort Red7;
        public ushort RedOutpost;
        public ushort RedBase;
        public ushort Blue1;
        public ushort Blue2;
        public ushort Blue3;
        public ushort Blue4;
        public ushort Blue5;
        public ushort Blue7;
        public ushort BlueOutpost;
        public ushort BlueBase;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public struct RadarInfo
    {
        public byte RadarInfoData;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public struct EventData
    {
        public uint EventDataValue;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public struct RadarMarkProgress
    {
        public byte MarkHeroProgress;
        public byte MarkEngineerProgress;
        public byte MarkStandard3Progress;
        public byte MarkStandard4Progress;
        public byte MarkStandard5Progress;
        public byte MarkSentryProgress;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public struct DartInfo
    {
        public byte DartRemainingTime;
        public ushort DartInfoData;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public struct MapRobotData
    {
        public ushort HeroPositionX;
        public ushort HeroPositionY;
        public ushort EngineerPositionX;
        public ushort EngineerPositionY;
        public ushort Infantry3PositionX;
        public ushort Infantry3PositionY;
        public ushort Infantry4PositionX;
        public ushort Infantry4PositionY;
        public ushort Infantry5PositionX;
        public ushort Infantry5PositionY;
        public ushort SentryPositionX;
        public ushort SentryPositionY;
    }

}