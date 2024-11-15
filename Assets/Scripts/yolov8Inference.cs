using UnityEngine;
using Unity.Sentis;
using UnityEngine.UI;
using System.Collections.Generic;
using radar.utils;
using System.ComponentModel;

public class yolov8Inference : MonoBehaviour
{
    public ModelAsset inferenceModel;
    public RenderTexture inputTexture;
    Worker worker;
    public float duration = 10f;
    float windowHeight = Screen.height;
    float windowWidth = Screen.width;
    Texture2D texture;
    // Start is called before the first frame update
    void Start()
    {
        Model runtimeModel = ModelLoader.Load(inferenceModel);
        worker = new Worker(runtimeModel, BackendType.GPUCompute);
        texture = new Texture2D(inputTexture.width, inputTexture.height, TextureFormat.RGB24, false);
    }

    void Update()
    {
        RenderTexture.active = inputTexture;
        texture.ReadPixels(new Rect(0, 0, inputTexture.width, inputTexture.height), 0, 0);
        texture.Apply();
        drawBox(inference(texture, 0.4f, 0.3f));
    }

    List<List<BoundingBox>> inference(Texture2D inputTexture, float confidenceThreshold, float nmsThreshold)
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
                    xMin = cpuTensor[0, 0, i] * (windowWidth / 640f) - cpuTensor[0, 2, i] * (windowWidth / 640f) / 2;
                    yMax = windowHeight - (cpuTensor[0, 1, i] * (windowHeight / 640f) - cpuTensor[0, 3, i] * (windowHeight / 640f) / 2);
                    xMax = cpuTensor[0, 0, i] * (windowWidth / 640f) + cpuTensor[0, 2, i] * (windowWidth / 640f) / 2;
                    yMin = windowHeight - (cpuTensor[0, 1, i] * (windowHeight / 640f) + cpuTensor[0, 3, i] * (windowHeight / 640f) / 2);
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

    void drawBox(List<List<BoundingBox>> boundingBoxes)
    {
        for (int i = 0; i < boundingBoxes.Count; i++)
        {
            for (int j = 0; j < boundingBoxes[i].Count; j++)
            {
                float a = boundingBoxes[i][j].Confidence;
                Color color = new Color(1 - a, a, 0, 0.8F);
                Vector3 leftUp = new Vector3(boundingBoxes[i][j].XMin, boundingBoxes[i][j].YMax, -10);
                Vector3 rightDown = new Vector3(boundingBoxes[i][j].XMax, boundingBoxes[i][j].YMin, -10);
                Vector3 leftDown = new Vector3(boundingBoxes[i][j].XMin, boundingBoxes[i][j].YMin, -10);
                Vector3 rightUp = new Vector3(boundingBoxes[i][j].XMax, boundingBoxes[i][j].YMax, -10);

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


