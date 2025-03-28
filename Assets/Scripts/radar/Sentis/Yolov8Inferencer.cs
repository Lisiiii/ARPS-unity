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
        private bool inferencePending = false;
        private Tensor<float> outputTensor;
        public Yolov8Inferencer(ModelAsset inferenceModel)
        {
            worker = new Worker(ModelLoader.Load(inferenceModel), BackendType.GPUCompute);
        }

        // Asynchronously run inference on the input texture,if the inference is done, return the result,or return null.
        public List<List<BoundingBox>> inference(Texture2D inputTexture, float confidenceThreshold, float nmsThreshold)
        {
            if (!inferencePending)
            {
                using Tensor<float> inputTensor = TextureConverter.ToTensor(inputTexture, width: 640, height: 640);
                worker.Schedule(inputTensor);
                outputTensor = worker.PeekOutput("output0") as Tensor<float>;
                outputTensor.ReadbackRequest();

                inferencePending = true;
                inputTensor.Dispose();
            }
            else if (inferencePending && outputTensor.IsReadbackRequestDone())
            {
                inferencePending = false;
                var cpuTensorArray = outputTensor.DownloadToArray();

                List<List<BoundingBox>> results = postProcess(cpuTensorArray, 80, confidenceThreshold, nmsThreshold);

                outputTensor.Dispose();
                return results;
            }
            return null;
        }



        List<List<BoundingBox>> postProcess(float[] cpuTensorArray, int classCount, float confidenceThreshold, float nmsThreshold)
        {
            // cpuTensor[i,j,k] (which is [1,84,8400]) = cpuTensorArray[i * 84 * 8400 + j * 8400 + k]

            List<List<BoundingBox>> finalResults = new List<List<BoundingBox>>();
            Dictionary<int, List<BoundingBox>> classBoundingBoxes = new Dictionary<int, List<BoundingBox>>();

            for (int i = 0; i < 8400; i++)
            {
                for (int classIndex = 4; classIndex < classCount; classIndex++)
                {
                    // float confidence = cpuTensor[0, classIndex, i];
                    float confidence = cpuTensorArray[classIndex * 8400 + i];
                    if (confidence > confidenceThreshold)
                    {
                        // float xMin = cpuTensor[0, 0, i] - cpuTensor[0, 2, i] / 2;
                        // float xMax = cpuTensor[0, 0, i] + cpuTensor[0, 2, i] / 2;
                        // float yMin = 640f - (cpuTensor[0, 1, i] + cpuTensor[0, 3, i] / 2);
                        // float yMax = 640f - (cpuTensor[0, 1, i] - cpuTensor[0, 3, i] / 2);
                        float xMin = cpuTensorArray[0 * 84 * 8400 + 0 * 8400 + i] - cpuTensorArray[0 * 84 * 8400 + 2 * 8400 + i] / 2;
                        float yMax = 640f - (cpuTensorArray[1 * 84 * 8400 + 1 * 8400 + i] - cpuTensorArray[1 * 84 * 8400 + 3 * 8400 + i] / 2);
                        float xMax = cpuTensorArray[0 * 84 * 8400 + 0 * 8400 + i] + cpuTensorArray[0 * 84 * 8400 + 2 * 8400 + i] / 2;
                        float yMin = 640f - (cpuTensorArray[1 * 84 * 8400 + 1 * 8400 + i] + cpuTensorArray[1 * 84 * 8400 + 3 * 8400 + i] / 2);

                        if (!classBoundingBoxes.ContainsKey(classIndex))
                        {
                            classBoundingBoxes[classIndex] = new List<BoundingBox>();
                        }

                        classBoundingBoxes[classIndex].Add(new BoundingBox
                        {
                            XMin = xMin,
                            XMax = xMax,
                            YMin = yMin,
                            YMax = yMax,
                            Confidence = confidence
                        });
                    }
                }
            }

            foreach (var kvp in classBoundingBoxes)
            {
                List<BoundingBox> suppressedBoxes = NMS.NonMaxSuppression(kvp.Value, nmsThreshold);
                if (suppressedBoxes.Count > 0)
                {
                    finalResults.Add(suppressedBoxes);
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


