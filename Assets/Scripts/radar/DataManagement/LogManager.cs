using System;
using System.Collections.Generic;
using UnityEngine;

public class LogManager : MonoBehaviour
{
    public static LogManager Instance
    {
        get
        {
            if (instance_ == null)
            {
                instance_ = FindAnyObjectByType<LogManager>();
                if (instance_ == null)
                {
                    GameObject obj = new GameObject("LogManager");
                    instance_ = obj.AddComponent<LogManager>();
                }
            }
            return instance_;
        }
    }
    private static LogManager instance_;
    public enum Level { Debug, Warning, Error }
    public class Log
    {
        public Level logLevel_;
        public string logContent_;
        public Log(Level logLevel, string logContent)
        {
            logLevel_ = logLevel;
            logContent_ = logContent;
        }
    }

    private Queue<Log> logQueue_ = new();
    public event Action<string> onLogUpdated_;
    public void log(string log) => logQueue_.Enqueue(new Log(Level.Debug, log));
    public void warning(string log) => logQueue_.Enqueue(new Log(Level.Warning, log));
    public void error(string log) => logQueue_.Enqueue(new Log(Level.Error, log));

    void Update()
    {
        while (logQueue_.Count > 7) logQueue_.Dequeue();
        string logText = "";
        foreach (var log in logQueue_)
        {
            logText += log.logLevel_.ToString() + "|" + log.logContent_ + "\n";
        }
        onLogUpdated_?.Invoke(logText);
    }

    void OnDestroy()
    {
        logQueue_.Clear();
        logQueue_ = null;
    }
}
