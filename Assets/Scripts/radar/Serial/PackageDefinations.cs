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


    /*
        CMD: 0x020E 1 byte 雷达自主决策信息同步，固定以1Hz频率发送

        bit 0-1：雷达是否拥有触发双倍易伤的机会，开局为 0，数值为雷达拥有触发双倍易伤的机会，至多为 2
        bit 2：对方是否正在被触发双倍易伤
        - 0：对方未被触发双倍易伤
        - 1：对方正在被触发双倍易伤
        bit 3-7：保留
    */
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public struct RadarInfo
    {
        public byte RadarInfoData;

        public int DoubleDebuffChances => (RadarInfoData & 0x01) + (((RadarInfoData & 0x02) == 0x02) ? 2 : 0);
        public bool IsDoubleDebuffAble => (RadarInfoData & 0x03) == 0x03;
    }

    /* 
        CMD: 0x0101  4 bytes 场地事件数据，固定以 1Hz 频率发送
        
        0：未占领/未激活
        1：已占领/已激活
        bit 0-2：
        - bit 0：己方与兑换区不重叠的补给区占领状态，1 为已占领
        - bit 1：己方与兑换区重叠的补给区占领状态，1 为已占领
        - bit 2：己方补给区的占领状态，1 为已占领（仅 RMUL 适用）
        bit 3-5：己方能量机关状态
        - bit 3：己方小能量机关的激活状态，1 为已激活
        - bit 4：己方大能量机关的激活状态，1 为已激活
        - bit 5-6：己方中央高地的占领状态，1 为被己方占领，2 为被对方占领
        - bit 7-8：己方梯形高地的占领状态，1 为已占领
        - bit 9-17：对方飞镖最后一次击中己方前哨站或基地的时间（0-420，开
        局默认为 0）
        - bit 18-20：对方飞镖最后一次击中己方前哨站或基地的具体目标，开局
        默认为 0，1 为击中前哨站，2 为击中基地固定目标，3 为击中基地随机
        固定目标，4 为击中基地随机移动目标
        - bit 21-22：中心增益点的占领状态，0 为未被占领，1 为被己方占领，2
        为被对方占领，3 为被双方占领。（仅 RMUL 适用）
        - bit 23-31：保留位
    */
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public struct EventData
    {
        public uint EventDataValue;

        public bool IsSupplyAreaOccupied => (EventDataValue & 0x01) == 0x01;
        public bool IsSupplyAreaOccupied2 => (EventDataValue & 0x02) == 0x02;
        public bool IsSupplyAreaOccupied3 => (EventDataValue & 0x04) == 0x04;
        public bool IsLittleEnergyOrganActivated => (EventDataValue & 0x08) == 0x08;
        public bool IsBigEnergyOrganActivated => (EventDataValue & 0x10) == 0x10;
        public bool IsCentralHighlandOccupied => (EventDataValue & 0x20) == 0x20;
        public bool IsTrapezoidalHighlandOccupied => (EventDataValue & 0x40) == 0x40;
        public int EnemyDartHitTime => (int)((EventDataValue >> 9) & 0x1FF);
        public int EnemyDartHitTarget => (int)((EventDataValue >> 18) & 0x07);
        public int CenterGainPointStatus => (int)((EventDataValue >> 21) & 0x03);
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