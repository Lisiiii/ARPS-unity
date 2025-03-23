using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace radar.state
{
    public enum RobotType { Hero, Engineer, Infantry1, Infantry2, Infantry3, Sentry, Dart, Drone, Outpost, Base }
    public enum Team { Blue, Red }
    public enum GameStage { NotStarted, Preparing, Countdown, Started, Finished }
    public class RobotState
    {
        public bool IsTracked;
        public Vector3 Position;
        public DateTime LastUpdateTime;
        public int Health;
    }
    public class GameState
    {
        public GameStage GameStage;
        public int GameTimeSeconds;
        public int GameCount;
        public Team Enemy;
    }
    public class StateManager
    {
        public Team _enemyTeam;
        private Dictionary<RobotType, RobotState> _enemyRobotStates;
        private GameState _gameState;

        public void setRobotPosition(Dictionary<RobotType, Vector3> newRobotPosition)
        {
            foreach (RobotType robotType in newRobotPosition.Keys)
            {
                _enemyRobotStates[robotType].IsTracked = true;
                _enemyRobotStates[robotType].Position = newRobotPosition[robotType];
                _enemyRobotStates[robotType].LastUpdateTime = DateTime.Now;
            }
        }
        public void update()
        {
            foreach (RobotType robotType in _enemyRobotStates.Keys)
            {
                TimeSpan ts = DateTime.Now - _enemyRobotStates[robotType].LastUpdateTime;
                if (ts.TotalSeconds > 2)
                    _enemyRobotStates[robotType].IsTracked = false;
            }
        }
        public GameState getGameState() => _gameState;
        public Dictionary<RobotType, RobotState> getEnemyRobotStates() => _enemyRobotStates;

        private static StateManager _instance;
        private static readonly object _instanceLock = new object();
        public static StateManager Instance()
        {
            if (_instance == null)
                lock (_instanceLock)
                    if (_instance == null) _instance = new StateManager();
            return _instance;
        }
        private StateManager()
        {
            _enemyRobotStates = new Dictionary<RobotType, RobotState>{
                { RobotType.Hero, new RobotState() { IsTracked = false,Position = Vector3.zero,LastUpdateTime = DateTime.Now,Health = 200}},
                { RobotType.Engineer, new RobotState() { IsTracked = false, Position = Vector3.zero, LastUpdateTime = DateTime.Now, Health = 200}},
                { RobotType.Infantry1, new RobotState() { IsTracked = false, Position = Vector3.zero, LastUpdateTime = DateTime.Now, Health = 200}},
                { RobotType.Infantry2, new RobotState() { IsTracked = false, Position = Vector3.zero, LastUpdateTime = DateTime.Now, Health = 200}},
                { RobotType.Infantry3, new RobotState() { IsTracked = false, Position = Vector3.zero, LastUpdateTime = DateTime.Now, Health = 200}},
                { RobotType.Sentry, new RobotState() { IsTracked = false, Position = Vector3.zero, LastUpdateTime = DateTime.Now, Health = 600}},
                { RobotType.Dart, new RobotState() { IsTracked = false, Position = Vector3.zero, LastUpdateTime = DateTime.Now, Health = -1}},
                { RobotType.Drone, new RobotState() { IsTracked = false, Position = Vector3.zero, LastUpdateTime = DateTime.Now, Health = -1}},
                { RobotType.Outpost, new RobotState() { IsTracked = false, Position = Vector3.zero, LastUpdateTime = DateTime.Now, Health = 1500}},
                { RobotType.Base, new RobotState() { IsTracked = false, Position = Vector3.zero, LastUpdateTime = DateTime.Now, Health = 2000}},
            };
            _gameState = new GameState { GameStage = GameStage.NotStarted, GameTimeSeconds = 0, GameCount = 0, Enemy = _enemyTeam };
        }
    }
}