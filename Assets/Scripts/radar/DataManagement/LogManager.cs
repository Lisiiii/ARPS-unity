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
                        GameObject obj = new("LogManager");
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
        string mainLogFilePath;
        public void log(string log) => logQueue_.Enqueue(new Log(Level.DEBUG, log));
        public void warning(string log) => logQueue_.Enqueue(new Log(Level.WARNING, log));
        public void error(string log) => logQueue_.Enqueue(new Log(Level.ERROR, log));

        void Start()
        {
            basePath_ = Application.streamingAssetsPath + logFileDir + "log_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "/";
            mainLogFilePath = basePath_ + "main_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";

            if (!System.IO.Directory.Exists(basePath_))
                System.IO.Directory.CreateDirectory(basePath_);
            if (!System.IO.File.Exists(mainLogFilePath))
            {
                System.IO.File.Create(mainLogFilePath).Dispose();
                System.IO.File.WriteAllText(mainLogFilePath, $"--- Log File Created: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " ---\n");
                Debug.Log("Log file created at: " + mainLogFilePath);
            }
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

                // log content is like: "[DataManager]Send data: {Hero: (1400, 750), E.....",split by []
                string moduleName = log.logContent_.Split(new char[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                string moduleLogFilePath = basePath_ + moduleName + ".log";

                if (!System.IO.File.Exists(moduleLogFilePath))
                {
                    System.IO.File.Create(moduleLogFilePath).Dispose();
                    System.IO.File.WriteAllText(moduleLogFilePath, $"---{moduleName} Log File Created: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " ---\n");
                    Debug.Log("Log file created at: " + moduleLogFilePath);
                }

                System.IO.File.AppendAllText(moduleLogFilePath, logMessage);
                System.IO.File.AppendAllText(mainLogFilePath, logMessage);

                // onLogUpdated_(logMessage);
            }

        }

        void OnDestroy()
        {
            logQueue_.Clear();
            logQueue_ = null;
        }
    }
}