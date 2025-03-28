using UnityEngine;
using Unity.Sentis;
using UnityEngine.UI;
using System.Collections.Generic;
using radar.Yolov8;
using radar.utils;

public class RayCaster : MonoBehaviour
{
    public ModelAsset modelAsset_;
    public RenderTexture inputTexture_;
    public Camera rayCastCamera_;
    public GameObject robotPrefab_;
    public bool ifInference_ = false;
    Yolov8Inferencer yolov8Inferencer_;
    Texture2D texture_;
    List<List<BoundingBox>> inferenceResults_;
    void Start()
    {
        yolov8Inferencer_ = new Yolov8Inferencer(modelAsset_);
        texture_ = new Texture2D(inputTexture_.width, inputTexture_.height, TextureFormat.RGB24, false);
        inferenceResults_ = new List<List<BoundingBox>>();
    }

    void Update()
    {
        RenderTexture.active = inputTexture_;
        texture_.ReadPixels(new Rect(0, 0, inputTexture_.width, inputTexture_.height), 0, 0);
        texture_.Apply();

        if (ifInference_)
        {
            List<List<BoundingBox>> results = yolov8Inferencer_.inference(texture_, 0.1f, 0.3f);
            if (results != null)
                inferenceResults_ = results;
            RayCast(inferenceResults_);
        }
    }

    void RayCast(List<List<BoundingBox>> inferenceResults)
    {
        if (inferenceResults.Count == 0 || inferenceResults[0].Count == 0) return;
        for (int i = 0; i < inferenceResults[0].Count; i++)
        {
            Vector2 centerPoint = new(
                (int)((inferenceResults[0][i].XMin + inferenceResults[0][i].XMax) / 2 * (rayCastCamera_.pixelWidth / 640f)),
                (int)((inferenceResults[0][i].YMin + inferenceResults[0][i].YMax) / 2 * (rayCastCamera_.pixelHeight / 640f))
            );

            Ray ray = rayCastCamera_.ScreenPointToRay(centerPoint);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                GameObject robot = Instantiate(robotPrefab_);
                robot.transform.position = hit.point;
                GameObject.Destroy(robot, 0.1f);

                Debug.DrawLine(rayCastCamera_.transform.position, hit.point, Color.green);
                Vector3 hitPointUp;
                hitPointUp = hit.point;
                hitPointUp.y += 1.0f;
                Debug.DrawLine(hitPointUp, hit.point, Color.red);
            }
        }

    }

    void OnDisable()
    {
        yolov8Inferencer_.Dispose();
    }
}


