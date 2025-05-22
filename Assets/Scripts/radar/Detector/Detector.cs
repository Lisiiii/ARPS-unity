using UnityEngine;

using UnityEngine.UI;
using System.Collections.Generic;
using radar.Yolov8;
using radar.utils;
using radar.data;
using System;
using NUnit.Framework;
using System.Collections;
using UnityEngine.Rendering;
using radar.webcamera;

namespace radar.detector
{
    public class RobotCoordinatePair
    {
        public Vector3 robotCoordinate_;
        public RobotType robotType_;
        public RobotCoordinatePair(Vector2 robotCoordinate, RobotType robotType)
        {
            robotCoordinate_ = robotCoordinate;
            robotType_ = robotType;
        }
    }
    public class Detector : MonoBehaviour
    {
        public static Detector Instance
        {
            get
            {
                if (instance_ == null)
                {
                    instance_ = FindAnyObjectByType<Detector>();
                    if (instance_ == null)
                    {
                        GameObject obj = new GameObject("Detector");
                        instance_ = obj.AddComponent<Detector>();
                    }
                }
                return instance_;
            }
        }
        private static Detector instance_;
        public Unity.InferenceEngine.ModelAsset robotModelAsset_;
        public Unity.InferenceEngine.ModelAsset armorModelAsset_;
        public int robotClassCount_ = 1;
        public int armorClassCount_ = 12;
        public bool ifInference_ = false;
        private Yolov8Inferencer robotInferencer_;

        /*
            Create multiple armor inferencers for inference multiple robots at the same time,avoid the armor inferencer
            to be blocked by last armor inferencing.
        */
        private List<Yolov8Inferencer> armorInferencerList_;
        private Dictionary<int, List<BoundingBox>> inferenceResults_;
        void Start()
        {
            Initialize();
        }
        void Initialize()
        {
            LogManager.Instance.log("[Detector]Robot Inferencer initializing...");

            if (robotInferencer_ == null)
                robotInferencer_ = new Yolov8Inferencer(robotModelAsset_, robotClassCount_);
            if (armorInferencerList_ == null || armorInferencerList_.Count == 0)
            {
                armorInferencerList_ = new List<Yolov8Inferencer>();
                for (int i = 0; i < armorClassCount_; i++)
                    armorInferencerList_.Add(new Yolov8Inferencer(armorModelAsset_, armorClassCount_));
            }

            inferenceResults_ = new Dictionary<int, List<BoundingBox>>();

            LogManager.Instance.log("[Detector]initialized.");
        }

        void Update()
        {

        }


        public void SwitchInference()
        {
            ifInference_ = !ifInference_;
            if (ifInference_)
            {
                Initialize();
                StartCoroutine(nameof(DetectionLoop));

                LogManager.Instance.log("[Detector]Detection started.");
            }
            else
            {
                StopAllCoroutines();
                robotInferencer_.Dispose();
                robotInferencer_ = null;
                foreach (var armorInferencer in armorInferencerList_)
                    armorInferencer.Dispose();
                armorInferencerList_.Clear();
                armorInferencerList_ = null;

                LogManager.Instance.log("[Detector]Detection stopped.");
            }
        }


        IEnumerator DetectionLoop()
        {
            while (true)
            {
                if (WebCameraHandler.Instance.raycastCameras_ == null || WebCameraHandler.Instance.raycastCameras_.Count == 0)
                {
                    StartCoroutine(nameof(Detection), WebCameraHandler.Instance.selectedCamera_);
                }
                else
                    foreach (var raycastCamera in WebCameraHandler.Instance.raycastCameras_)
                    {
                        StartCoroutine(nameof(Detection), raycastCamera);
                    }
                yield return new WaitForEndOfFrame();
            }
        }
        IEnumerator Detection(RaycastCameraType raycastCameraData)
        {
            Texture2D inputTexture = raycastCameraData.outputTexture2D_;
            Camera raycastCamera = raycastCameraData.raycastCamera_;
            if (raycastCamera == null)
                yield break;

            Dictionary<int, List<BoundingBox>> robotResults;

            if (inputTexture == null)
                yield break;

            inferenceResults_.Clear();
            robotResults = null;
            yield return new WaitUntil(() =>
            {
                robotResults = robotInferencer_.inference(inputTexture, 0.1f, 0.2f);
                return robotResults != null;
            });

            if (robotResults.Count == 0)
            {
                yield return new WaitForFixedUpdate();
                yield break;
            }

            bool[] isFinished = new bool[robotResults[0].Count];

            for (int i = 0; i < robotResults[0].Count; i++)
            {
                var robotBox = robotResults[0][i];
                StartCoroutine(ArmorDetection(robotBox, inputTexture, i, isFinished));
            }

            yield return new WaitUntil(() => System.Array.TrueForAll(isFinished, x => x));

            RayCast(inferenceResults_, raycastCamera);
        }
        IEnumerator ArmorDetection(BoundingBox robotBox, Texture2D inputTexture, int idx, bool[] finished)
        {
            Vector2Int boxOrigin = new Vector2Int((int)robotBox.XMin, (int)robotBox.YMin);
            Vector2Int boxSize = new Vector2Int((int)(robotBox.XMax - robotBox.XMin), (int)(robotBox.YMax - robotBox.YMin));
            boxOrigin.x = Mathf.Clamp(boxOrigin.x, 0, inputTexture.width - 1);
            boxOrigin.y = Mathf.Clamp(boxOrigin.y, 0, inputTexture.height - 1);
            boxSize.x = Mathf.Clamp(boxSize.x, 0, inputTexture.width - boxOrigin.x - 1);
            boxSize.y = Mathf.Clamp(boxSize.y, 0, inputTexture.height - boxOrigin.y - 1);

            Color[] m_Colors = inputTexture.GetPixels(boxOrigin.x, boxOrigin.y, boxSize.x, boxSize.y);
            Texture2D clipedTexture = new Texture2D(boxSize.x, boxSize.y, TextureFormat.RGB24, false);
            clipedTexture.SetPixels(m_Colors);
            clipedTexture.Apply();

            Dictionary<int, List<BoundingBox>> armorResults = null;
            yield return new WaitUntil(() =>
            {
                armorResults = armorInferencerList_[idx].inference(clipedTexture, 0.1f, 0.2f);
                return armorResults != null;
            });

            Vector2 robotType = new Vector2(-1, 0);
            if (armorResults.Count != 0)
                foreach (var kvp in armorResults)
                    foreach (var box in kvp.Value)
                        if (box.Confidence > robotType.y)
                            robotType = new Vector2(kvp.Key, box.Confidence);

            lock (inferenceResults_)
            {
                if (inferenceResults_.ContainsKey((int)robotType.x))
                    inferenceResults_[(int)robotType.x].Add(new BoundingBox()
                    {
                        XMin = robotBox.XMin,
                        YMin = robotBox.YMin,
                        XMax = robotBox.XMax,
                        YMax = robotBox.YMax,
                        Confidence = robotType.y
                    });
                else
                    inferenceResults_.Add((int)robotType.x, new List<BoundingBox>()
            {
                new BoundingBox()
                {
                    XMin = robotBox.XMin,
                    YMin = robotBox.YMin,
                    XMax = robotBox.XMax,
                    YMax = robotBox.YMax,
                    Confidence = robotType.y
                }
            });
            }
            finished[idx] = true;
        }

        private void RayCast(Dictionary<int, List<BoundingBox>> inferenceResults, Camera raycastCamera)
        {
            if (inferenceResults.Count == 0 || raycastCamera == null) return;
            // 0-5 for blue, 6-11 for red
            Vector2Int classScale =
                DataManager.Instance.stateData.gameState.EnemySide == Team.Blue ? new Vector2Int(0, 5) : new Vector2Int(6, 11);

            foreach (var kvp in inferenceResults)
            {
                int classIndex = kvp.Key;
                if (inferenceResults[classIndex].Count == 0) continue;
                for (int classCount = 0; classCount < inferenceResults[classIndex].Count; classCount++)
                {
                    Vector2 centerPoint = new(
                        (int)((inferenceResults[classIndex][classCount].XMin + inferenceResults[classIndex][classCount].XMax) / 2),
                        (int)(inferenceResults[classIndex][classCount].YMin +
                         ((inferenceResults[classIndex][classCount].YMax - inferenceResults[classIndex][classCount].YMin) / 4))
                    );

                    Ray ray = raycastCamera.ScreenPointToRay(centerPoint);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit))
                    {
                        Vector3 robotCoordinate = new Vector3(hit.point.x, hit.point.z, hit.point.y);
                        RobotType robotType = (RobotType)(classIndex > 5 ? classIndex - 6 : classIndex);
                        RobotCoordinatePair newPair = new(robotCoordinate, robotType);
                        if (classIndex >= classScale.x && classIndex <= classScale.y)
                            DataManager.Instance.UploadData(newPair, UpdateEnemyRobotPositions);
                        else
                            DataManager.Instance.UploadData(newPair, UpdateAllieRobotPositions);

                        Debug.DrawLine(ray.origin, hit.point, Color.red, 1f);
                    }
                }
            }
        }

        public void UpdateEnemyRobotPositions(RobotCoordinatePair updateRobotPositions)
        {
            RobotSets robot = DataManager.Instance.stateData.enemyRobots;
            robot.Data[updateRobotPositions.robotType_].Position = updateRobotPositions.robotCoordinate_;
            robot.Data[updateRobotPositions.robotType_].IsTracked = true;
            robot.Data[updateRobotPositions.robotType_].LastUpdateTime = DateTime.Now;
        }

        public void UpdateAllieRobotPositions(RobotCoordinatePair updateRobotPositions)
        {
            RobotSets robot = DataManager.Instance.stateData.allieRobots;
            robot.Data[updateRobotPositions.robotType_].Position = updateRobotPositions.robotCoordinate_;
            robot.Data[updateRobotPositions.robotType_].IsTracked = true;
            robot.Data[updateRobotPositions.robotType_].LastUpdateTime = DateTime.Now;
        }


        void OnDisable()
        {
            if (robotInferencer_ != null)
                robotInferencer_.Dispose();
            if (armorInferencerList_ != null)
            {
                foreach (var armorInferencer in armorInferencerList_)
                    if (armorInferencer != null)
                        armorInferencer.Dispose();
                armorInferencerList_.Clear();
                armorInferencerList_ = null;
            }

        }
    }


}