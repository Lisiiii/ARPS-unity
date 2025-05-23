using System;
using System.Collections;
using System.Collections.Generic;
using radar.ui.panel;
using UnityEngine;

namespace radar.data
{
    public enum RobotType { Unkown = -1, Hero = 0, Engineer = 1, Infantry3 = 2, Infantry4 = 3, Infantry5 = 4, Sentry = 5, Dart = 6, Drone = 7, Outpost = 8, Base = 9 }
    public static class RobotName
    {
        public static readonly Dictionary<RobotType, string> Chinese = new()
        {
            { RobotType.Hero, "英雄" },
            { RobotType.Engineer, "工程" },
            { RobotType.Infantry3, "步兵3" },
            { RobotType.Infantry4, "步兵4" },
            { RobotType.Infantry5, "步兵5" },
            { RobotType.Sentry, "哨兵" },
            { RobotType.Dart, "飞镖" },
            { RobotType.Drone, "无人机" },
            { RobotType.Outpost, "前哨站" },
            { RobotType.Base, "基地" },
            { RobotType.Unkown, "未知" }

        };

        public static readonly Dictionary<RobotType, string> English = new()
        {
            { RobotType.Hero, "Hero" },
            { RobotType.Engineer, "Engineer" },
            { RobotType.Infantry3, "Infantry3" },
            { RobotType.Infantry4, "Infantry4" },
            { RobotType.Infantry5, "Infantry5" },
            { RobotType.Sentry, "Sentry" },
            { RobotType.Dart, "Dart" },
            { RobotType.Drone, "Drone" },
            { RobotType.Outpost, "Outpost" },
            { RobotType.Base, "Base" },
            { RobotType.Unkown, "Unknown" }
        };
    }
    public enum Team { Blue, Red }
    public enum GameStage { NotStarted = 0, Preparing = 1, SelfCheck = 2, Countdown = 3, Started = 4, Finished = 5 }

    public static class StageName
    {
        public static readonly Dictionary<GameStage, string> Chinese = new()
        {
            { GameStage.NotStarted, "未开始" },
            { GameStage.Preparing, "准备中" },
            { GameStage.SelfCheck, "自检中" },
            { GameStage.Countdown, "倒计时" },
            { GameStage.Started, "进行中" },
            { GameStage.Finished, "已结束" }
        };
    }


    public class RobotSets
    {
        public class RobotState
        {
            public bool IsTracked;
            public Vector3 Position;
            public DateTime LastUpdateTime;
            public int HP;
        }
        public Dictionary<RobotType, RobotState> Data;
        public RobotSets(List<RobotType> robotTypes = null)
        {
            Data = new Dictionary<RobotType, RobotState>();
            if (robotTypes != null)
            {
                foreach (var robotType in robotTypes)
                {
                    Data.Add(robotType, new RobotState() { IsTracked = false, Position = Vector2.zero, LastUpdateTime = DateTime.Now, HP = 200 });
                    switch (robotType)
                    {
                        case RobotType.Hero:
                        case RobotType.Engineer:
                        case RobotType.Infantry3:
                        case RobotType.Infantry4:
                        case RobotType.Infantry5:
                        case RobotType.Sentry:
                            Data[robotType].HP = 200;
                            break;
                        case RobotType.Dart:
                            Data[robotType].HP = 10;
                            break;
                        case RobotType.Drone:
                            Data[robotType].HP = 10;
                            break;
                        case RobotType.Outpost:
                            Data[robotType].HP = 1500;
                            break;
                        case RobotType.Base:
                            Data[robotType].HP = 2000; // Base HP
                            break;
                        default:
                            Data[robotType].HP = 200; // Unknown type
                            break;
                    }
                }
            }
        }

    }
    public class GameState
    {
        public GameStage GameStage;
        public int GameTimeSeconds;
        public int GameCount;
        public Team EnemySide;
    }
    public class RadarInfo
    {
        public int DoubleDebuffChances;
        public bool IsDoubleDebuffAble;
    }
    public class StateDatas
    {
        public RobotSets enemyRobots;
        public RobotSets allieRobots;
        public RobotSets enemyFacilities;
        public RobotSets allieFacilities;
        public RadarInfo radarInfo;
        public GameState gameState;
        public StateDatas()
        {
            enemyRobots = new RobotSets(new List<RobotType> { RobotType.Hero, RobotType.Engineer, RobotType.Infantry3, RobotType.Infantry4, RobotType.Infantry5, RobotType.Sentry, RobotType.Unkown });
            allieRobots = new RobotSets(new List<RobotType> { RobotType.Hero, RobotType.Engineer, RobotType.Infantry3, RobotType.Infantry4, RobotType.Infantry5, RobotType.Sentry, RobotType.Unkown });
            enemyFacilities = new RobotSets(new List<RobotType> { RobotType.Dart, RobotType.Drone, RobotType.Outpost, RobotType.Base });
            allieFacilities = new RobotSets(new List<RobotType> { RobotType.Dart, RobotType.Drone, RobotType.Outpost, RobotType.Base });
            gameState = new GameState { GameStage = GameStage.NotStarted, GameTimeSeconds = 0, GameCount = 0, EnemySide = Team.Blue };
            radarInfo = new RadarInfo { DoubleDebuffChances = 0, IsDoubleDebuffAble = false };
        }
    }
}