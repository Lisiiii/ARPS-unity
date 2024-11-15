using UnityEngine;
using Unity.Sentis;
using UnityEngine.UI;
using System.Collections.Generic;
using radar.utils;

public class sentisInfer : MonoBehaviour
{
    public ModelAsset inferenceModel;
    public Texture2D inputTexture;
    Worker worker;
    public RawImage image;
    Tensor outputTensor;
    Tensor<float> cpuTensor;
    List<BoundingBox> results = new List<BoundingBox>();
    float duration = 0.03f;
    float windowHeight;
    float windowWidth;
    // Start is called before the first frame update
    void Start()
    {
        Model runtimeModel = ModelLoader.Load(inferenceModel);

        // Convert a texture to a tensor
        Tensor<float> inputTensor = TextureConverter.ToTensor(inputTexture, width: 640, height: 640);
        worker = new Worker(runtimeModel, BackendType.GPUCompute);
        worker.Schedule(inputTensor);
        outputTensor = worker.PeekOutput("outputs");
        cpuTensor = outputTensor.ReadbackAndClone() as Tensor<float>;
        inputTensor.Dispose();

    }

    void drawBox()
    {
        windowHeight = Screen.height;
        windowWidth = Screen.width;
        for (int i = 0; i < 8400; i++)
        {
            if (cpuTensor[0, 4, i] > 0.1f)
            {
                float xMin, xMax, yMin, yMax;
                xMin = cpuTensor[0, 0, i] * (windowWidth / 640f) - cpuTensor[0, 2, i] * (windowWidth / 640f) / 2;
                yMax = windowHeight - (cpuTensor[0, 1, i] * (windowHeight / 640f) - cpuTensor[0, 3, i] * (windowHeight / 640f) / 2);
                xMax = cpuTensor[0, 0, i] * (windowWidth / 640f) + cpuTensor[0, 2, i] * (windowWidth / 640f) / 2;
                yMin = windowHeight - (cpuTensor[0, 1, i] * (windowHeight / 640f) + cpuTensor[0, 3, i] * (windowHeight / 640f) / 2);

                results.Add(new BoundingBox
                {
                    XMin = xMin,
                    XMax = xMax,
                    YMin = yMin,
                    YMax = yMax,
                    Confidence = cpuTensor[0, 4, i]
                });
            }
        }

        results = NMS.NonMaxSuppression(results, 0.3f);

        for (int i = 0; i < results.Count; i++)
        {
            float a = results[i].Confidence;
            Color color = new Color(1 - a, a, 0, 0.8F);
            Vector3 leftUp = new Vector3(results[i].XMin, results[i].YMax, -10);
            Vector3 rightDown = new Vector3(results[i].XMax, results[i].YMin, -10);
            Vector3 leftDown = new Vector3(results[i].XMin, results[i].YMin, -10);
            Vector3 rightUp = new Vector3(results[i].XMax, results[i].YMax, -10);

            Debug.DrawLine(leftUp, rightUp, color, duration);
            Debug.DrawLine(rightUp, rightDown, color, duration);
            Debug.DrawLine(rightDown, leftDown, color, duration);
            Debug.DrawLine(leftDown, leftUp, color, duration);
        }

        results.Clear();
    }

    // Update is called once per frame
    void Update()
    {
        drawBox();
    }

    void OnDisable()
    {
        outputTensor.Dispose();
        cpuTensor.Dispose();
        worker.Dispose();
    }
}


