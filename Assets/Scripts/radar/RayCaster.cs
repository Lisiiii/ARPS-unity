using UnityEngine;
using Unity.Sentis;
using UnityEngine.UI;
using System.Collections.Generic;
using radar.Yolov8;
using radar.utils;

public class RayCaster : MonoBehaviour
{
    public ModelAsset modelAsset;
    public RenderTexture inputTexture;
    public Camera rayCastCamera;
    public GameObject Robot;
    Yolov8Inferencer yolov8Inferencer;
    Texture2D texture;
    void Start()
    {
        yolov8Inferencer = new Yolov8Inferencer(modelAsset);
        texture = new Texture2D(inputTexture.width, inputTexture.height, TextureFormat.RGB24, false);
    }

    void Update()
    {
        RenderTexture.active = inputTexture;
        texture.ReadPixels(new Rect(0, 0, inputTexture.width, inputTexture.height), 0, 0);
        texture.Apply();
        List<List<BoundingBox>> results = yolov8Inferencer.inference(texture, 0.1f, 0.3f);
        RayCast(results);
    }

    void RayCast(List<List<BoundingBox>> inferenceResults)
    {
        if (inferenceResults.Count == 0 || inferenceResults[0].Count == 0) return;
        for (int i = 0; i < inferenceResults[0].Count; i++)
        {
            Vector2 centerPoint = new(
                (int)((inferenceResults[0][i].XMin + inferenceResults[0][i].XMax) / 2 * (rayCastCamera.pixelWidth / 640f)),
                (int)((inferenceResults[0][i].YMin + inferenceResults[0][i].YMax) / 2 * (rayCastCamera.pixelHeight / 640f))
            );

            Ray ray = rayCastCamera.ScreenPointToRay(centerPoint);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                GameObject robot = Instantiate(Robot);
                robot.transform.position = hit.point;
                GameObject.Destroy(robot, 0.1f);

                // Debug.DrawLine(rayCastCamera.transform.position, hit.point, Color.green);
                // Vector3 hitPointUp;
                // hitPointUp = hit.point;
                // hitPointUp.y += 1.0f;
                // Debug.DrawLine(hitPointUp, hit.point, Color.red);
            }
        }

    }

    void OnDisable()
    {
        yolov8Inferencer.Dispose();
    }
}


