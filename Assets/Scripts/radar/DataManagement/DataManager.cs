using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using radar.data;
using System;
using Unity.VisualScripting;
using System.Collections.Concurrent;
using radar.serial.package;
using System.Runtime.InteropServices;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<DataManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("DataManager");
                    _instance = obj.AddComponent<DataManager>();
                }
            }
            return _instance;
        }
    }
    public ConcurrentQueue<StateDatas> updatedStateQueue = new ConcurrentQueue<StateDatas>();
    public event Action<StateDatas> OnDataUpdated;
    public StateDatas stateData => stateData_;    // Read-only property to access the state data
    public float sendFrequencyHz = 10f;
    private static DataManager _instance;
    private StateDatas stateData_ = new();
    private bool isDataUpdated_ = false;
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
            SendData();
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
        foreach (var robotState in stateData_.enemyRobotStates_)
        {
            if (!robotState.Value.IsTracked) continue;

            if (DateTime.Now - robotState.Value.LastUpdateTime > TimeSpan.FromSeconds(3))
            {
                robotState.Value.IsTracked = false;
                robotState.Value.Position = Vector2.zero;
            }

            // TODO: Using Kalman filter to smooth the position data 
        }
    }

    private void SendData()
    {
        MapRobotData mapDataToSend = new MapRobotData();
        mapDataToSend.HeroPositionX = (ushort)stateData_.enemyRobotStates_[RobotType.Hero].Position.x;
        mapDataToSend.HeroPositionY = (ushort)stateData_.enemyRobotStates_[RobotType.Hero].Position.y;
        mapDataToSend.EngineerPositionX = (ushort)stateData_.enemyRobotStates_[RobotType.Engineer].Position.x;
        mapDataToSend.EngineerPositionY = (ushort)stateData_.enemyRobotStates_[RobotType.Engineer].Position.y;
        mapDataToSend.Infantry3PositionX = (ushort)stateData_.enemyRobotStates_[RobotType.Infantry5].Position.x;
        mapDataToSend.Infantry3PositionY = (ushort)stateData_.enemyRobotStates_[RobotType.Infantry5].Position.y;
        mapDataToSend.Infantry4PositionX = (ushort)stateData_.enemyRobotStates_[RobotType.Infantry4].Position.x;
        mapDataToSend.Infantry4PositionY = (ushort)stateData_.enemyRobotStates_[RobotType.Infantry4].Position.y;
        mapDataToSend.Infantry5PositionX = (ushort)stateData_.enemyRobotStates_[RobotType.Infantry5].Position.x;
        mapDataToSend.Infantry5PositionY = (ushort)stateData_.enemyRobotStates_[RobotType.Infantry5].Position.y;
        mapDataToSend.SentryPositionX = (ushort)stateData_.enemyRobotStates_[RobotType.Sentry].Position.x;
        mapDataToSend.SentryPositionY = (ushort)stateData_.enemyRobotStates_[RobotType.Sentry].Position.y;

        byte[] dataToSend = new byte[Marshal.SizeOf(typeof(MapRobotData))];
        IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MapRobotData)));
        Marshal.StructureToPtr(mapDataToSend, ptr, true);
        Marshal.Copy(ptr, dataToSend, 0, Marshal.SizeOf(typeof(MapRobotData)));
        Marshal.FreeHGlobal(ptr);

        radar.serial.SerialHandler.Instance.SendData(0x0305, dataToSend);
    }
    private void OnDestroy()
    {
        _instance = null;
    }
}
