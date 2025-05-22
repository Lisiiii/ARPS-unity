using UnityEngine;

using System.Collections.Generic;
using radar.utils;
using System;
using UnityEngine.UI;
using Unity.Mathematics;
using radar.data;

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
        private int outputTensorSize_ = 8400; // The size of the output tensor
        private int classCount_; // Number of classes in model
        private Vector2Int inputSize_ = new Vector2Int(640, 640); // Input size of the model
        private Texture2D inputTexture_;
        public Yolov8Inferencer(Unity.InferenceEngine.ModelAsset inferenceModel, int classCount = 1, Vector2Int inputSize = default)
        {
            inputSize_ = inputSize == default ? new Vector2Int(640, 640) : inputSize;
            classCount_ = classCount;
            outputTensorSize_ = (int)(8400f * (inputSize_.x / 640f) * (inputSize_.y / 640f));
            worker_ = new Unity.InferenceEngine.Worker(Unity.InferenceEngine.ModelLoader.Load(inferenceModel), Unity.InferenceEngine.BackendType.GPUCompute);

            LogManager.Instance.log($"[Yolov8Inferencer]Model loaded: {inferenceModel.name}");
            LogManager.Instance.log($"[Yolov8Inferencer]Model class count: {classCount_}");
        }

        // Asynchronously run inference on the input texture,if the inference is done, return the result,or return null.
        public Dictionary<int, List<BoundingBox>> inference(Texture2D inputTexture, float confidenceThreshold, float nmsThreshold)
        {
            inputTexture_ = inputTexture;
            if (!inferencePending_)
            {
                inputTensor_ = new Unity.InferenceEngine.Tensor<float>(new Unity.InferenceEngine.TensorShape(1, 3, inputSize_.x, inputSize_.y));
                Unity.InferenceEngine.TextureConverter.ToTensor(inputTexture, inputTensor_);

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

            for (int i = 0; i < outputTensorSize_; i++)
            {
                for (int classIndex = 4; classIndex < classCount + 4; classIndex++)
                {
                    // float confidence = cpuTensor[0, classIndex, i];
                    float confidence = cpuTensorArray[classIndex * outputTensorSize_ + i];
                    if (confidence > confidenceThreshold)
                    {
                        // float xMin = cpuTensor[0, 0, i] - cpuTensor[0, 2, i] / 2;
                        // float xMax = cpuTensor[0, 0, i] + cpuTensor[0, 2, i] / 2;
                        // float yMin = 640f - (cpuTensor[0, 1, i] + cpuTensor[0, 3, i] / 2);
                        // float yMax = 640f - (cpuTensor[0, 1, i] - cpuTensor[0, 3, i] / 2);
                        float xMin = cpuTensorArray[0 * outputTensorSize_ + i] - cpuTensorArray[2 * outputTensorSize_ + i] / 2;
                        float yMax = 640f - (cpuTensorArray[1 * outputTensorSize_ + i] - cpuTensorArray[3 * outputTensorSize_ + i] / 2);
                        float xMax = cpuTensorArray[0 * outputTensorSize_ + i] + cpuTensorArray[2 * outputTensorSize_ + i] / 2;
                        float yMin = 640f - (cpuTensorArray[1 * outputTensorSize_ + i] + cpuTensorArray[3 * outputTensorSize_ + i] / 2);

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


