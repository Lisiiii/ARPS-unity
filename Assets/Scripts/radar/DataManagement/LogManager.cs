using System;
using System.Collections.Generic;
using UnityEngine;

namespace radar.data
{
    public class LogManager : MonoBehaviour
    {
        public enum Level { DEBUG, WARNING, ERROR }
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
        private Queue<Log> logQueue_ = new();
        // public event Action<string> onLogUpdated_;
        public string logFileDir = "/Logs/";
        private string basePath_;
        private string logFilePath;
        public void log(string log) => logQueue_.Enqueue(new Log(Level.DEBUG, log));
        public void warning(string log) => logQueue_.Enqueue(new Log(Level.WARNING, log));
        public void error(string log) => logQueue_.Enqueue(new Log(Level.ERROR, log));

        void Start()
        {
            basePath_ = Application.streamingAssetsPath + logFileDir;
            logFilePath = basePath_ + "log_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
            if (!System.IO.Directory.Exists(basePath_))
                System.IO.Directory.CreateDirectory(basePath_);
            if (!System.IO.File.Exists(logFilePath))
                System.IO.File.Create(logFilePath).Dispose();
            System.IO.File.WriteAllText(logFilePath, "--- Log File Created: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " ---\n");
            Debug.Log("Log file created at: " + logFilePath);
            logQueue_.Clear();
        }

        void Update()
        {
            outputToFile();
        }

        private void outputToFile()
        {
            while (logQueue_.Count > 0)
            {
                Log log = logQueue_.Dequeue();
                string logMessage = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "]<" + log.logLevel_.ToString() + "> " + log.logContent_ + "\n";
                System.IO.File.AppendAllText(logFilePath, logMessage);
                // onLogUpdated_(logMessage);
            }

        }

        void OnDestroy()
        {
            log("--- Log File Closed: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " ---\n");
            outputToFile();
            logQueue_.Clear();
            logQueue_ = null;
        }
    }
}