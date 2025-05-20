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
        public Unity.InferenceEngine.ModelAsset robotModelAsset_;
        public Unity.InferenceEngine.ModelAsset armorModelAsset_;
        public int robotClassCount_ = 1;
        public int armorClassCount_ = 12;
        public RenderTexture inputTexture_;
        public Camera rayCastCamera_;
        public bool ifInference_ = false;
        private Yolov8Inferencer robotInferencer_;

        /*
            Create multiple armor inferencers for inference multiple robots at the same time,avoid the armor inferencer
            to be blocked by last armor inferencing.
        */
        private List<Yolov8Inferencer> armorInferencerList_;
        private Texture2D rawTexture_;
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

            rawTexture_ = new Texture2D(inputTexture_.width, inputTexture_.height, TextureFormat.RGBA32, false);
            inferenceResults_ = new Dictionary<int, List<BoundingBox>>();

            LogManager.Instance.log("[Detector]initialized.");
        }

        void Update()
        {
            RenderTexture.active = inputTexture_;
            AsyncGPUReadback.Request(inputTexture_, 0, TextureFormat.RGBA32, request =>
            {
                if (!request.hasError)
                {
                    var data = request.GetData<byte>();
                    if (data == null || data.Length == 0 || rawTexture_ == null)
                        return;
                    rawTexture_.LoadRawTextureData(data);
                    rawTexture_.Apply();
                }
            });
        }


        public void SwitchInference()
        {
            ifInference_ = !ifInference_;
            if (ifInference_)
            {
                Initialize();
                StartCoroutine(nameof(Detection));

                LogManager.Instance.log("[Detector]Detection started.");
            }
            else
            {
                StopCoroutine(nameof(Detection));
                robotInferencer_.Dispose();
                robotInferencer_ = null;
                foreach (var armorInferencer in armorInferencerList_)
                    armorInferencer.Dispose();
                armorInferencerList_.Clear();
                armorInferencerList_ = null;

                LogManager.Instance.log("[Detector]Detection stopped.");
            }
        }

        IEnumerator Detection()
        {
            Dictionary<int, List<BoundingBox>> robotResults;
            while (true)
            {
                inferenceResults_.Clear();
                robotResults = null;
                yield return new WaitUntil(() =>
                {
                    robotResults = robotInferencer_.inference(rawTexture_, 0.1f, 0.2f);
                    return robotResults != null;
                });

                if (robotResults.Count == 0)
                {
                    yield return new WaitForFixedUpdate();
                    continue;
                }

                bool[] isFinished = new bool[robotResults[0].Count];

                for (int i = 0; i < robotResults[0].Count; i++)
                {
                    var robotBox = robotResults[0][i];
                    StartCoroutine(ArmorDetectCoroutine(robotBox, i, isFinished));
                }

                yield return new WaitUntil(() => System.Array.TrueForAll(isFinished, x => x));

                RayCast(inferenceResults_);
            }
        }

        IEnumerator ArmorDetectCoroutine(BoundingBox robotBox, int idx, bool[] finished)
        {
            Vector2Int boxOrigin = new Vector2Int((int)robotBox.XMin, (int)robotBox.YMin);
            Vector2Int boxSize = new Vector2Int((int)(robotBox.XMax - robotBox.XMin), (int)(robotBox.YMax - robotBox.YMin));
            boxOrigin.x = Mathf.Clamp(boxOrigin.x, 0, rawTexture_.width - 1);
            boxOrigin.y = Mathf.Clamp(boxOrigin.y, 0, rawTexture_.height - 1);
            boxSize.x = Mathf.Clamp(boxSize.x, 0, rawTexture_.width - boxOrigin.x - 1);
            boxSize.y = Mathf.Clamp(boxSize.y, 0, rawTexture_.height - boxOrigin.y - 1);

            Color[] m_Colors = rawTexture_.GetPixels(boxOrigin.x, boxOrigin.y, boxSize.x, boxSize.y);
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

        private void RayCast(Dictionary<int, List<BoundingBox>> inferenceResults)
        {
            if (inferenceResults.Count == 0) return;
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

                    Ray ray = rayCastCamera_.ScreenPointToRay(centerPoint);
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