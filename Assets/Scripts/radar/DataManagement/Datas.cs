using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace radar.data
{
    public enum RobotType { Hero, Engineer, Infantry1, Infantry2, Infantry3, Sentry, Dart, Drone, Outpost, Base }
    public enum Team { Blue, Red }
    public enum GameStage { NotStarted, Preparing, Countdown, Started, Finished }
    public class RobotState
    {
        public bool IsTracked;
        public Vector3 Position;
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
        public GameState gameState_;

        // public void setRobotPosition(Dictionary<RobotType, Vector3> newRobotPosition)
        // {
        //     foreach (RobotType robotType in newRobotPosition.Keys)
        //     {
        //         enemyRobotStates_[robotType].IsTracked = true;
        //         enemyRobotStates_[robotType].Position = newRobotPosition[robotType];
        //         enemyRobotStates_[robotType].LastUpdateTime = DateTime.Now;
        //     }
        // }
        // public void update()
        // {
        //     foreach (RobotType robotType in enemyRobotStates_.Keys)
        //     {
        //         TimeSpan ts = DateTime.Now - enemyRobotStates_[robotType].LastUpdateTime;
        //         if (ts.TotalSeconds > 2)
        //             enemyRobotStates_[robotType].IsTracked = false;
        //     }
        // }

        public StateDatas()
        {
            enemyRobotStates_ = new Dictionary<RobotType, RobotState>{
                { RobotType.Hero, new RobotState() { IsTracked = false,Position = Vector3.zero,LastUpdateTime = DateTime.Now,HP = 200}},
                { RobotType.Engineer, new RobotState() { IsTracked = false, Position = Vector3.zero, LastUpdateTime = DateTime.Now, HP = 200}},
                { RobotType.Infantry1, new RobotState() { IsTracked = false, Position = Vector3.zero, LastUpdateTime = DateTime.Now, HP = 200}},
                { RobotType.Infantry2, new RobotState() { IsTracked = false, Position = Vector3.zero, LastUpdateTime = DateTime.Now, HP = 200}},
                { RobotType.Infantry3, new RobotState() { IsTracked = false, Position = Vector3.zero, LastUpdateTime = DateTime.Now, HP = 200}},
                { RobotType.Sentry, new RobotState() { IsTracked = false, Position = Vector3.zero, LastUpdateTime = DateTime.Now, HP = 600}},
                { RobotType.Dart, new RobotState() { IsTracked = false, Position = Vector3.zero, LastUpdateTime = DateTime.Now, HP = -1}},
                { RobotType.Drone, new RobotState() { IsTracked = false, Position = Vector3.zero, LastUpdateTime = DateTime.Now, HP = -1}},
                { RobotType.Outpost, new RobotState() { IsTracked = false, Position = Vector3.zero, LastUpdateTime = DateTime.Now, HP = 1500}},
                { RobotType.Base, new RobotState() { IsTracked = false, Position = Vector3.zero, LastUpdateTime = DateTime.Now, HP = 2000}},
            };
            gameState_ = new GameState { GameStage = GameStage.NotStarted, GameTimeSeconds = 0, GameCount = 0, EnemySide = Team.Blue };
        }
    }
}