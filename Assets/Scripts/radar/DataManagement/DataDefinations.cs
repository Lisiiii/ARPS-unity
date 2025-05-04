using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace radar.data
{
    public enum RobotType { Hero, Engineer, Infantry3, Infantry4, Infantry5, Sentry, Dart, Drone, Outpost, Base }
    public enum Team { Blue, Red }
    public enum GameStage { NotStarted, Preparing, Countdown, Started, Finished }

    public class RobotState
    {
        public bool IsTracked;
        public Vector2 Position;
        public DateTime LastUpdateTime;
        public int HP;
    }
    public class GameState
    {
        public GameStage GameStage;
        public int GameTimeSeconds;
        public int GameCount;
        public Team EnemySide;
    }
    public class StateDatas
    {
        public Dictionary<RobotType, RobotState> enemyRobotStates_;
        public Dictionary<RobotType, RobotState> allieRobotStates_;
        public GameState gameState_;
        public StateDatas()
        {
            enemyRobotStates_ = new Dictionary<RobotType, RobotState>{
                { RobotType.Hero, new RobotState() { IsTracked = false,Position = Vector2.zero,LastUpdateTime = DateTime.Now,HP = 200}},
                { RobotType.Engineer, new RobotState() { IsTracked = false, Position = Vector2.zero, LastUpdateTime = DateTime.Now, HP = 200}},
                { RobotType.Infantry3, new RobotState() { IsTracked = false, Position = Vector2.zero, LastUpdateTime = DateTime.Now, HP = 200}},
                { RobotType.Infantry4, new RobotState() { IsTracked = false, Position = Vector2.zero, LastUpdateTime = DateTime.Now, HP = 200}},
                { RobotType.Infantry5, new RobotState() { IsTracked = false, Position = Vector2.zero, LastUpdateTime = DateTime.Now, HP = 200}},
                { RobotType.Sentry, new RobotState() { IsTracked = false, Position = Vector2.zero, LastUpdateTime = DateTime.Now, HP = 600}},
                { RobotType.Dart, new RobotState() { IsTracked = false, Position = Vector2.zero, LastUpdateTime = DateTime.Now, HP = -1}},
                { RobotType.Drone, new RobotState() { IsTracked = false, Position = Vector2.zero, LastUpdateTime = DateTime.Now, HP = -1}},
                { RobotType.Outpost, new RobotState() { IsTracked = false, Position = Vector2.zero, LastUpdateTime = DateTime.Now, HP = 1500}},
                { RobotType.Base, new RobotState() { IsTracked = false, Position = Vector2.zero, LastUpdateTime = DateTime.Now, HP = 2000}},
            };
            allieRobotStates_ = enemyRobotStates_;
            gameState_ = new GameState { GameStage = GameStage.NotStarted, GameTimeSeconds = 0, GameCount = 0, EnemySide = Team.Red };
        }
    }
}