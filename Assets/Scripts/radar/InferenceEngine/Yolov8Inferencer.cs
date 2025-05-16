using UnityEngine;

using System.Collections.Generic;
using radar.utils;
using System;
using UnityEngine.UI;
using Unity.Mathematics;

namespace radar.Yolov8
{
    // Attention: The class is used to run inference on the YOLOv8 model using the Sentis library. 
    // It is NOT suitable for yolov5 or yoloV7 models.
    public class Yolov8Inferencer : IDisposable
    {
        private Unity.InferenceEngine.Worker worker_;
        private bool inferencePending_ = false;
        private Unity.InferenceEngine.Tensor<float> outputTensor_ = null;
        private Unity.InferenceEngine.Tensor<float> inputTensor_ = null;
        private int classCount_; // Number of classes in model
        private Texture2D inputTexture_ = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        public Yolov8Inferencer(Unity.InferenceEngine.ModelAsset inferenceModel, int classCount = 1)
        {
            classCount_ = classCount;
            worker_ = new Unity.InferenceEngine.Worker(Unity.InferenceEngine.ModelLoader.Load(inferenceModel), Unity.InferenceEngine.BackendType.GPUCompute);
        }

        // Asynchronously run inference on the input texture,if the inference is done, return the result,or return null.
        public Dictionary<int, List<BoundingBox>> inference(Texture2D inputTexture, float confidenceThreshold, float nmsThreshold)
        {
            inputTexture_ = inputTexture;
            if (!inferencePending_)
            {
                inputTensor_ = Unity.InferenceEngine.TextureConverter.ToTensor(inputTexture, width: 640, height: 640);
                worker_.Schedule(inputTensor_);
                outputTensor_ = worker_.PeekOutput("output0") as Unity.InferenceEngine.Tensor<float>;
                outputTensor_.ReadbackRequest();

                inferencePending_ = true;
            }
            else if (inferencePending_ && outputTensor_.IsReadbackRequestDone())
            {
                inputTensor_.Dispose();

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
                for (int classIndex = 4; classIndex < classCount + 4; classIndex++)
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
                        xMin = (int)Mathf.Clamp(xMin * (inputTexture_.width / 640f), 0, inputTexture_.width - 1);
                        xMax = (int)Mathf.Clamp(xMax * (inputTexture_.width / 640f), 0, inputTexture_.width - 1);
                        yMin = (int)Mathf.Clamp(yMin * (inputTexture_.height / 640f), 0, inputTexture_.height - 1);
                        yMax = (int)Mathf.Clamp(yMax * (inputTexture_.height / 640f), 0, inputTexture_.height - 1);

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
                // Debug.Log($"{(classCount > 1 ? "Armor" : "Car")}| Class {kvp.Key} has {suppressedBoxes.Count} boxes after NMS");
                LogManager.Instance.log($"{kvp.Key}");
            }



            return finalResults;
        }
        ~Yolov8Inferencer()
        {
            Dispose();
        }
        public void OnDestroy()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (inputTensor_ != null)
                inputTensor_.Dispose();
            if (outputTensor_ != null)
                outputTensor_.Dispose();
            worker_.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}


