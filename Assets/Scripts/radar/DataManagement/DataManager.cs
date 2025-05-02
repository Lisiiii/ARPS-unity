using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using radar.data;
using System;
using Unity.VisualScripting;
using System.Collections.Concurrent;

public class DataManager : MonoBehaviour
{
    private static DataManager _instance;
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
    public ConcurrentQueue<StateDatas> updatedStateQueue_ = new ConcurrentQueue<StateDatas>();
    public StateDatas stateData_ = new StateDatas();
    public event Action<StateDatas> OnDataUpdated;
    public bool isDataUpdated_ = false;
    public void Start()
    {
        OnDataUpdated += DEBUG_logOnDataUpdated;
    }

    public void Update()
    {
        UpdateData();
    }

    private void UpdateData()
    {
        if (updatedStateQueue_.TryDequeue(out StateDatas updatedState))
        {
            stateData_ = updatedState;
            isDataUpdated_ = true;
        }
        // Clear the queue if it exceeds 2 items
        while (updatedStateQueue_.Count > 2) { updatedStateQueue_.TryDequeue(out _); }

        if (isDataUpdated_)
        {
            OnDataUpdated?.Invoke(stateData_);
            isDataUpdated_ = false;
        }
    }

    void DEBUG_logOnDataUpdated(StateDatas stateData)
    {
        Debug.Log("Data Updated: " + stateData.gameState_.GameStage.ToString() + " " + stateData.gameState_.GameTimeSeconds.ToString() + " " + stateData.gameState_.GameCount.ToString() + " " + stateData.gameState_.EnemySide.ToString());
        foreach (var robot in stateData.enemyRobotStates_)
        {
            Debug.Log(robot.Key.ToString() + ": " + robot.Value.Position.ToString() + " " + robot.Value.IsTracked.ToString() + " " + robot.Value.LastUpdateTime.ToString() + " " + robot.Value.HP.ToString());
        }
    }
}
