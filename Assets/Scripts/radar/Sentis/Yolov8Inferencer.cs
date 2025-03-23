using UnityEngine;
using Unity.Sentis;
using System.Collections.Generic;
using radar.utils;
using System;

namespace radar.Yolov8
{
    public class Yolov8Inferencer : IDisposable
    {
        Worker worker;
        public Yolov8Inferencer(ModelAsset inferenceModel)
        {
            worker = new Worker(ModelLoader.Load(inferenceModel), BackendType.GPUCompute);
        }
        public List<List<BoundingBox>> inference(Texture2D inputTexture, float confidenceThreshold, float nmsThreshold)
        {
            using Tensor<float> inputTensor = TextureConverter.ToTensor(inputTexture, width: 640, height: 640);
            worker.Schedule(inputTensor);
            using Tensor outputTensor = worker.PeekOutput("output0");
            using Tensor<float> cpuTensor = outputTensor.ReadbackAndClone() as Tensor<float>;

            List<List<BoundingBox>> results = postProcess(cpuTensor, cpuTensor.shape[1] - 4, confidenceThreshold, nmsThreshold);

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
        ~Yolov8Inferencer()
        {
            Dispose();
        }
        public void Dispose()
        {
            worker.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}


