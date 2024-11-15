using UnityEngine;
using Unity.Sentis;
using System.Collections.Generic;
using radar.utils;

namespace radar.Yolov8
{
    public class Yolov8Inferencer
    {
        float duration = 0f;
        Worker worker;
        float windowHeight = Screen.height;
        float windowWidth = Screen.width;
        public Yolov8Inferencer(ModelAsset inferenceModel)
        {
            worker = new Worker(ModelLoader.Load(inferenceModel), BackendType.GPUCompute);
        }
        public List<List<BoundingBox>> inference(Texture2D inputTexture, float confidenceThreshold, float nmsThreshold, bool ifDrawBoxes)
        {
            using Tensor<float> inputTensor = TextureConverter.ToTensor(inputTexture, width: 640, height: 640);
            worker.Schedule(inputTensor);
            using Tensor outputTensor = worker.PeekOutput("output0");
            using Tensor<float> cpuTensor = outputTensor.ReadbackAndClone() as Tensor<float>;

            List<List<BoundingBox>> results = postProcess(cpuTensor, cpuTensor.shape[1] - 4, confidenceThreshold, nmsThreshold);
            if (ifDrawBoxes) drawBoxes(results);

            return results;
        }

        List<List<BoundingBox>> postProcess(Tensor<float> cpuTensor, int classCount, float confidenceThreshold, float nmsThreshold)
        {
            List<List<BoundingBox>> finalResults = new List<List<BoundingBox>>();
            List<BoundingBox> singleResults = new List<BoundingBox>();
            for (int classIndex = 4; classIndex < classCount; classIndex++)
            {
                for (int i = 0; i < 8400; i++)
                {
                    if (cpuTensor[0, classIndex, i] > confidenceThreshold)
                    {
                        float xMin, xMax, yMin, yMax;
                        xMin = cpuTensor[0, 0, i] - cpuTensor[0, 2, i] / 2;
                        yMax = 640f - (cpuTensor[0, 1, i] - cpuTensor[0, 3, i] / 2);
                        xMax = cpuTensor[0, 0, i] + cpuTensor[0, 2, i] / 2;
                        yMin = 640f - (cpuTensor[0, 1, i] + cpuTensor[0, 3, i] / 2);
                        singleResults.Add(new BoundingBox
                        {
                            XMin = xMin,
                            XMax = xMax,
                            YMin = yMin,
                            YMax = yMax,
                            Confidence = cpuTensor[0, classIndex, i]
                        });
                    }
                }
                singleResults = NMS.NonMaxSuppression(singleResults, nmsThreshold);
                if (singleResults.Count > 0)
                {
                    finalResults.Add(singleResults);
                    singleResults = new();
                }
            }
            return finalResults;
        }

        void drawBoxes(List<List<BoundingBox>> boundingBoxes)
        {
            for (int i = 0; i < boundingBoxes.Count; i++)
            {
                for (int j = 0; j < boundingBoxes[i].Count; j++)
                {
                    float a = boundingBoxes[i][j].Confidence;
                    Color color = new Color(1 - a, a, 0, 0.8F);
                    Vector3 leftUp = new Vector3(boundingBoxes[i][j].XMin * (windowWidth / 640f), boundingBoxes[i][j].YMax * (windowHeight / 640f), -10);
                    Vector3 rightDown = new Vector3(boundingBoxes[i][j].XMax * (windowWidth / 640f), boundingBoxes[i][j].YMin * (windowHeight / 640f), -10);
                    Vector3 leftDown = new Vector3(boundingBoxes[i][j].XMin * (windowWidth / 640f), boundingBoxes[i][j].YMin * (windowHeight / 640f), -10);
                    Vector3 rightUp = new Vector3(boundingBoxes[i][j].XMax * (windowWidth / 640f), boundingBoxes[i][j].YMax * (windowHeight / 640f), -10);

                    Debug.DrawLine(leftUp, rightUp, color, duration);
                    Debug.DrawLine(rightUp, rightDown, color, duration);
                    Debug.DrawLine(rightDown, leftDown, color, duration);
                    Debug.DrawLine(leftDown, leftUp, color, duration);
                }
            }
        }

        void OnDisable()
        {
            worker.Dispose();
        }
    }
}


