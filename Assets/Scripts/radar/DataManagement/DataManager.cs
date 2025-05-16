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
            if (instance_ == null)
            {
                instance_ = FindAnyObjectByType<DataManager>();
                if (instance_ == null)
                {
                    GameObject obj = new GameObject("DataManager");
                    instance_ = obj.AddComponent<DataManager>();
                }
            }
            return instance_;
        }
    }
    public ConcurrentQueue<StateDatas> updatedStateQueue = new ConcurrentQueue<StateDatas>();
    public event Action<StateDatas> OnDataUpdated;
    public ref StateDatas stateData => ref stateData_;    // Read-only property to access the state data
    public float sendFrequencyHz = 10f;
    private static DataManager instance_;
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
        foreach (var robotState in stateData_.enemyRobots.Data)
        {
            if (!robotState.Value.IsTracked) continue;

            if (DateTime.Now - robotState.Value.LastUpdateTime > TimeSpan.FromSeconds(2))
            {
                robotState.Value.IsTracked = false;
                robotState.Value.Position = new Vector2(11.25f, 5.3f);
                isDataUpdated_ = true;
            }

            // TODO: Using Kalman filter to smooth the position data 
        }
    }

    private void SendData()
    {
        MapRobotData mapDataToSend = new MapRobotData();
        mapDataToSend.HeroPositionX = (ushort)stateData_.enemyRobots.Data[RobotType.Hero].Position.x;
        mapDataToSend.HeroPositionY = (ushort)stateData_.enemyRobots.Data[RobotType.Hero].Position.y;
        mapDataToSend.EngineerPositionX = (ushort)stateData_.enemyRobots.Data[RobotType.Engineer].Position.x;
        mapDataToSend.EngineerPositionY = (ushort)stateData_.enemyRobots.Data[RobotType.Engineer].Position.y;
        mapDataToSend.Infantry3PositionX = (ushort)stateData_.enemyRobots.Data[RobotType.Infantry5].Position.x;
        mapDataToSend.Infantry3PositionY = (ushort)stateData_.enemyRobots.Data[RobotType.Infantry5].Position.y;
        mapDataToSend.Infantry4PositionX = (ushort)stateData_.enemyRobots.Data[RobotType.Infantry4].Position.x;
        mapDataToSend.Infantry4PositionY = (ushort)stateData_.enemyRobots.Data[RobotType.Infantry4].Position.y;
        mapDataToSend.Infantry5PositionX = (ushort)stateData_.enemyRobots.Data[RobotType.Infantry5].Position.x;
        mapDataToSend.Infantry5PositionY = (ushort)stateData_.enemyRobots.Data[RobotType.Infantry5].Position.y;
        mapDataToSend.SentryPositionX = (ushort)stateData_.enemyRobots.Data[RobotType.Sentry].Position.x;
        mapDataToSend.SentryPositionY = (ushort)stateData_.enemyRobots.Data[RobotType.Sentry].Position.y;

        byte[] dataToSend = new byte[Marshal.SizeOf(typeof(MapRobotData))];
        IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MapRobotData)));
        Marshal.StructureToPtr(mapDataToSend, ptr, true);
        Marshal.Copy(ptr, dataToSend, 0, Marshal.SizeOf(typeof(MapRobotData)));
        Marshal.FreeHGlobal(ptr);

        radar.serial.SerialHandler.Instance.SendData(0x0305, dataToSend);
    }
    private void OnDestroy()
    {
        instance_ = null;
    }
}
