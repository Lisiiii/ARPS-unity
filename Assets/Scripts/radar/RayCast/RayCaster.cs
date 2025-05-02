using UnityEngine;
using Unity.Sentis;
using UnityEngine.UI;
using System.Collections.Generic;
using radar.Yolov8;
using radar.utils;
using radar.data;

public class RayCaster : MonoBehaviour
{
    public ModelAsset modelAsset_;
    public int classCount_;
    public RenderTexture inputTexture_;
    public Camera rayCastCamera_;
    public GameObject robotPrefab_;
    public bool ifInference_ = false;
    private Yolov8Inferencer yolov8Inferencer_;
    private Texture2D texture_;
    private Dictionary<int, List<BoundingBox>> inferenceResults_;
    void Start()
    {
        Initialize();
    }
    void Initialize()
    {
        yolov8Inferencer_ = new Yolov8Inferencer(modelAsset_);
        yolov8Inferencer_.classCount_ = classCount_;
        texture_ = new Texture2D(inputTexture_.width, inputTexture_.height, TextureFormat.RGB24, false);
        inferenceResults_ = new Dictionary<int, List<BoundingBox>>();
    }

    void Update()
    {
        RenderTexture.active = inputTexture_;
        texture_.ReadPixels(new Rect(0, 0, inputTexture_.width, inputTexture_.height), 0, 0);
        texture_.Apply();

        if (ifInference_)
        {
            Dictionary<int, List<BoundingBox>> results = yolov8Inferencer_.inference(texture_, 0.05f, 0.2f);
            if (results != null)
                inferenceResults_ = results;
            RayCast(inferenceResults_);
        }
    }

    public void swichInference()
    {
        ifInference_ = !ifInference_;
        if (ifInference_)
        {
            Initialize();
        }
        else
        {
            yolov8Inferencer_.Dispose();
            yolov8Inferencer_ = null;
        }
    }

    void RayCast(Dictionary<int, List<BoundingBox>> inferenceResults)
    {
        bool isUpdateData = false;
        if (inferenceResults.Count == 0) return;
        // 0-5 for blue, 6-11 for red
        Vector2Int classScale = DataManager.Instance.stateData_.gameState_.EnemySide == Team.Blue ? new Vector2Int(0, 5) : new Vector2Int(6, 11);
        foreach (var kvp in inferenceResults)
        {
            int classIndex = kvp.Key;
            if (inferenceResults[classIndex].Count == 0) continue;
            if (classIndex < classScale.x || classIndex > classScale.y) continue; // Only process classes that are enemy robots
            for (int classCount = 0; classCount < inferenceResults[classIndex].Count; classCount++)
            {
                Vector2 centerPoint = new(
                    (int)((inferenceResults[classIndex][classCount].XMin + inferenceResults[classIndex][classCount].XMax) / 2 * (rayCastCamera_.pixelWidth / 640f)),
                    (int)((inferenceResults[classIndex][classCount].YMin + inferenceResults[classIndex][classCount].YMax) / 2 * (rayCastCamera_.pixelHeight / 640f))
                );

                Ray ray = rayCastCamera_.ScreenPointToRay(centerPoint);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    Vector2 robotCoordinate = new(hit.point.x, hit.point.z);
                    RobotType robotType = (RobotType)(classIndex - classScale.x);
                    DataManager.Instance.stateData_.enemyRobotStates_[robotType].IsTracked = true;
                    DataManager.Instance.stateData_.enemyRobotStates_[robotType].Position = robotCoordinate;
                    DataManager.Instance.stateData_.enemyRobotStates_[robotType].LastUpdateTime = System.DateTime.Now;
                    isUpdateData = true;

                    // Visualization
                    Vector3 hitPointUp;
                    hitPointUp = hit.point;
                    hitPointUp.y += 1.0f;

                    Debug.DrawLine(rayCastCamera_.transform.position, hit.point, Color.green);
                    Debug.DrawLine(hitPointUp, hit.point, Color.red);

                    GameObject robot = Instantiate(robotPrefab_);
                    robot.transform.position = hit.point;
                    GameObject.Destroy(robot, 0.1f);
                }
            }
        }
        if (isUpdateData)
            DataManager.Instance.isDataUpdated_ = true;
    }

    void OnDisable()
    {
        if (yolov8Inferencer_ != null)
            yolov8Inferencer_.Dispose();
    }
}


