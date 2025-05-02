using UnityEngine;
using Unity.Sentis;
using System.Collections.Generic;
using radar.utils;
using System;

namespace radar.Yolov8
{
    // Attention: The class is used to run inference on the YOLOv8 model using the Sentis library. 
    // It is NOT suitable for yolov5 or yoloV7 models.
    public class Yolov8Inferencer : IDisposable
    {
        Worker worker_;
        private bool inferencePending_ = false;
        private Tensor<float> outputTensor_;
        public int classCount_; // Number of classes in model
        public Yolov8Inferencer(ModelAsset inferenceModel)
        {
            worker_ = new Worker(ModelLoader.Load(inferenceModel), BackendType.GPUCompute);
        }

        // Asynchronously run inference on the input texture,if the inference is done, return the result,or return null.
        public Dictionary<int, List<BoundingBox>> inference(Texture2D inputTexture, float confidenceThreshold, float nmsThreshold)
        {
            if (!inferencePending_)
            {
                using Tensor<float> inputTensor = TextureConverter.ToTensor(inputTexture, width: 640, height: 640);
                worker_.Schedule(inputTensor);
                outputTensor_ = worker_.PeekOutput("output0") as Tensor<float>;
                outputTensor_.ReadbackRequest();

                inferencePending_ = true;
                inputTensor.Dispose();
            }
            else if (inferencePending_ && outputTensor_.IsReadbackRequestDone())
            {
                inferencePending_ = false;
                var cpuTensorArray = outputTensor_.DownloadToArray();

                Dictionary<int, List<BoundingBox>> results = postProcess(cpuTensorArray, classCount_, confidenceThreshold, nmsThreshold);

                outputTensor_.Dispose();
                return results;
            }
            return null;
        }



        Dictionary<int, List<BoundingBox>> postProcess(float[] cpuTensorArray, int classCount, float confidenceThreshold, float nmsThreshold)
        {
            // cpuTensor[i,j,k] (which is [1, classCount + 4 ,8400]) = cpuTensorArray[i * (classCount + 4) * 8400 + j * 8400 + k]
            Dictionary<int, List<BoundingBox>> finalResults = new Dictionary<int, List<BoundingBox>>();
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
                        float xMin = cpuTensorArray[0 * 8400 + i] - cpuTensorArray[2 * 8400 + i] / 2;
                        float yMax = 640f - (cpuTensorArray[1 * 8400 + i] - cpuTensorArray[3 * 8400 + i] / 2);
                        float xMax = cpuTensorArray[0 * 8400 + i] + cpuTensorArray[2 * 8400 + i] / 2;
                        float yMin = 640f - (cpuTensorArray[1 * 8400 + i] + cpuTensorArray[3 * 8400 + i] / 2);

                        //  the output tensor is 4->classCount+4, so the classIndex should be minus 4
                        if (!classBoundingBoxes.ContainsKey(classIndex - 4))
                        {
                            classBoundingBoxes[classIndex - 4] = new List<BoundingBox>();
                        }

                        classBoundingBoxes[classIndex - 4].Add(new BoundingBox
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
                if (suppressedBoxes.Count == 0) continue;
                finalResults.Add(kvp.Key, suppressedBoxes);
            }
            return finalResults;
        }
        ~Yolov8Inferencer()
        {
            Dispose();
        }
        public void Dispose()
        {
            worker_.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}


