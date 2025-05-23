using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using radar.data;
using System;
using Unity.VisualScripting;
using System.Collections.Concurrent;
using radar.serial.package;
using System.Runtime.InteropServices;
using radar.serial;
using radar.ui.panel;

namespace radar.data
{
    public class DataManager : MonoBehaviour
    {
        public static DataManager Instance
        {
            get
            {
                if (instance_ == null)
                {
                    instance_ = FindAnyObjectByType<DataManager>();
                    if (instance_ == null)
                    {
                        GameObject obj = new("DataManager");
                        instance_ = obj.AddComponent<DataManager>();
                    }
                }
                return instance_;
            }
        }
        private static DataManager instance_;

        public ConcurrentQueue<StateDatas> updatedStateQueue = new();
        public event Action<StateDatas> OnDataUpdated;
        public event Action<int> OnDoubleDebuffChancesEnabled;
        private StateDatas stateData_ = new();
        public ref StateDatas stateData => ref stateData_;    // Read-only property to access the state data

        public float sendFrequencyHz = 10f;
        private bool isDataUpdated_ = false;
        private int doubleDebuffActivedTimes = 0;
        public DateTime lastRecordTime = DateTime.Now;
        public int lastRecordTimeSeconds = 0;
        public void Start()
        {
        }

        public void Update()
        {
            DequeueData();
            UpdateData();

            if (isDataUpdated_)
            {
                OnDataUpdated?.Invoke(stateData_);
                isDataUpdated_ = false;
            }

            // Send data at the specified frequency
            if (Time.frameCount % Mathf.RoundToInt(60f / sendFrequencyHz) == 0)
                SendRobotPositionData();

            if (stateData_.radarInfo.DoubleDebuffChances > 0)
                SendDoubleDebuffCmd();
        }

        public void UploadData<T>(T data, Action<T> updateAction)
        {
            updateAction?.Invoke(data);
            isDataUpdated_ = true;
        }

        // Get data from the queue
        private void DequeueData()
        {
            // Clear the queue if it exceeds 2 items
            while (updatedStateQueue.Count > 2) { updatedStateQueue.TryDequeue(out _); }
            if (updatedStateQueue.TryDequeue(out StateDatas updatedState))
            {
                stateData_ = updatedState;
                isDataUpdated_ = true;
            }
        }

        private void UpdateData()
        {
            TimeSpan timeSinceLastUpdate = DateTime.Now - lastRecordTime;
            stateData_.gameState.GameTimeSeconds = lastRecordTimeSeconds - (int)timeSinceLastUpdate.TotalSeconds;
            if (stateData_.gameState.GameTimeSeconds < 0)
                stateData_.gameState.GameTimeSeconds = 0;

            foreach (var robotState in stateData_.enemyRobots.Data)
            {
                if (!robotState.Value.IsTracked) continue;

                if (DateTime.Now - robotState.Value.LastUpdateTime > TimeSpan.FromSeconds(2))
                {
                    robotState.Value.IsTracked = false;
                    robotState.Value.Position = new Vector2(11.25f, 5.3f);
                    isDataUpdated_ = true;
                    continue;
                }

                // TODO: Predict or flilter
            }

        }

        private void SendDoubleDebuffCmd()
        {
            if (!SerialHandler.Instance.isConnected) return;
            if (stateData_.radarInfo.IsDoubleDebuffAble) return;
            if (doubleDebuffActivedTimes >= 2)
            {
                doubleDebuffActivedTimes = 0;
                return;
            }
            doubleDebuffActivedTimes++;

            RobotInteraction_Radar robotInteractionData = new()
            {
                dataCmdId = 0x0121,
                senderId = (ushort)(Instance.stateData.gameState.EnemySide == Team.Blue ? 9 : 109),
                receiverId = 0x8080,
                data = (byte)doubleDebuffActivedTimes
            };


            byte[] dataToSend = new byte[Marshal.SizeOf(typeof(RobotInteraction_Radar))];
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(RobotInteraction_Radar)));
            Marshal.StructureToPtr(robotInteractionData, ptr, true);
            Marshal.Copy(ptr, dataToSend, 0, Marshal.SizeOf(typeof(RobotInteraction_Radar)));
            Marshal.FreeHGlobal(ptr);

            SerialHandler.Instance.SendData(0x0301, dataToSend);

            LogManager.Instance.log($"[DataManager]Send double debuff command:{doubleDebuffActivedTimes}");

            OnDoubleDebuffChancesEnabled?.Invoke(doubleDebuffActivedTimes);
        }

        private void SendRobotPositionData()
        {
            if (!SerialHandler.Instance.isConnected) return;

            Dictionary<RobotType, Vector2> realLocationRobots = new();
            foreach (var robot in stateData_.enemyRobots.Data)
            {
                Vector2 location =
                          stateData.gameState.EnemySide == Team.Blue
                              ? new Vector2(robot.Value.Position.x + 14f, robot.Value.Position.y + 7.5f)
                              : new Vector2(28f - (robot.Value.Position.x + 14f), 15f - (robot.Value.Position.y + 7.5f));
                // 2025赛季更改了地图坐标单位,现为cm,这里 m -> cm
                Vector2 robotCoordinate = new(location.x * 100, location.y * 100);
                realLocationRobots.Add(robot.Key, robotCoordinate);
            }

            MapRobotData mapDataToSend = new()
            {
                HeroPositionX = (ushort)realLocationRobots[RobotType.Hero].x,
                HeroPositionY = (ushort)realLocationRobots[RobotType.Hero].y,
                EngineerPositionX = (ushort)realLocationRobots[RobotType.Engineer].x,
                EngineerPositionY = (ushort)realLocationRobots[RobotType.Engineer].y,
                Infantry3PositionX = (ushort)realLocationRobots[RobotType.Infantry3].x,
                Infantry3PositionY = (ushort)realLocationRobots[RobotType.Infantry3].y,
                Infantry4PositionX = (ushort)realLocationRobots[RobotType.Infantry4].x,
                Infantry4PositionY = (ushort)realLocationRobots[RobotType.Infantry4].y,
                Infantry5PositionX = (ushort)realLocationRobots[RobotType.Infantry5].x,
                Infantry5PositionY = (ushort)realLocationRobots[RobotType.Infantry5].y,
                SentryPositionX = (ushort)realLocationRobots[RobotType.Sentry].x,
                SentryPositionY = (ushort)realLocationRobots[RobotType.Sentry].y
            };

            byte[] dataToSend = new byte[Marshal.SizeOf(typeof(MapRobotData))];
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MapRobotData)));
            Marshal.StructureToPtr(mapDataToSend, ptr, true);
            Marshal.Copy(ptr, dataToSend, 0, Marshal.SizeOf(typeof(MapRobotData)));
            Marshal.FreeHGlobal(ptr);

            SerialHandler.Instance.SendData(0x0305, dataToSend);

            LogManager.Instance.log("[DataManager]Send data: {" +
                $"Hero: ({mapDataToSend.HeroPositionX}, {mapDataToSend.HeroPositionY}), " +
                $"Engineer: ({mapDataToSend.EngineerPositionX}, {mapDataToSend.EngineerPositionY}), " +
                $"Infantry3: ({mapDataToSend.Infantry3PositionX}, {mapDataToSend.Infantry3PositionY}), " +
                $"Infantry4: ({mapDataToSend.Infantry4PositionX}, {mapDataToSend.Infantry4PositionY}), " +
                $"Infantry5: ({mapDataToSend.Infantry5PositionX}, {mapDataToSend.Infantry5PositionY}), " +
                $"Sentry: ({mapDataToSend.SentryPositionX}, {mapDataToSend.SentryPositionY})" +
                "}");
        }
        private void OnDestroy()
        {
            instance_ = null;
        }
    }
}