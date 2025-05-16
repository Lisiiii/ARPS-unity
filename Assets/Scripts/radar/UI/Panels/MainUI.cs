using radar.Yolov8;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using radar.detector;
using System;
using Unity.VisualScripting;
using radar.data;
using System.Collections.Generic;

namespace radar.ui.panel
{


    #region GAMETIME_VIEW
    public class GameTimeType
    {
        public Transform GameTimeRoot_;
        public TextMeshProUGUI StateName_;
        public TextMeshProUGUI Time_;
        public Button RoundButton_;
        public Button ResetButton_;
        public GameTimeType(Transform GameTimeRoot)
        {
            GameTimeRoot_ = GameTimeRoot;
            StateName_ = GameTimeRoot.Find("StateName").GetComponent<TextMeshProUGUI>();
            Time_ = GameTimeRoot.Find("Time").GetComponent<TextMeshProUGUI>();
            RoundButton_ = GameTimeRoot.Find("RoundButton").GetComponent<Button>();
            ResetButton_ = GameTimeRoot.Find("ResetButton").GetComponent<Button>();

            RoundButton_.onClick.AddListener(() =>
            {
                DataManager.Instance.UploadData(
                    ((int)DataManager.Instance.stateData.gameState_.GameStage + 1) % 5,
                    (stage) =>
                    {
                        DataManager.Instance.stateData.gameState_.GameStage = (GameStage)stage;
                    });
            });
        }
        public void SetStateName(GameStage stage)
        {
            StateName_.text = StageName.Chinese[stage];
            switch (stage)
            {
                case GameStage.NotStarted:
                    StateName_.color = Color.white;
                    break;
                case GameStage.Preparing:
                    StateName_.color = Color.yellow;
                    break;
                case GameStage.Countdown:
                    StateName_.color = Color.red;
                    break;
                case GameStage.Started:
                    StateName_.color = Color.blue;
                    break;
                case GameStage.Finished:
                    StateName_.color = Color.gray;
                    break;
            }
        }
        public void SetRound(int round, int bo = 3)
        {
            RoundButton_.transform.GetComponentInChildren<TextMeshProUGUI>().text = "Round  " + round.ToString() + "/" + bo.ToString();
        }
        public void SetTime(string time)
        {
            Time_.text = time;
        }

    };
    #endregion

    #region ENEMYSIDE_VIEW
    public class EnemySideType
    {
        public Transform EnemySideRoot_;
        public TextMeshProUGUI Color_;
        public Button SwitchButton_;

        public EnemySideType(Transform enemySideRoot)
        {
            EnemySideRoot_ = enemySideRoot;
            Color_ = enemySideRoot.Find("Color").GetComponent<TextMeshProUGUI>();
            SwitchButton_ = enemySideRoot.Find("SwitchButton").GetComponent<Button>();

            SwitchButton_.onClick.AddListener(() =>
            {
                DataManager.Instance.UploadData(
                    DataManager.Instance.stateData.gameState_.EnemySide == Team.Blue ? Team.Red : Team.Blue,
                    (team) =>
                    {
                        DataManager.Instance.stateData.gameState_.EnemySide = team;
                    });
            });
        }
        public void SetEnemySide(Team team)
        {
            Color_.color = (team == Team.Blue) ? Color.blue : Color.red;
            Color_.text = (team == Team.Blue) ? "蓝方" : "红方";
        }

    };
    #endregion

    #region ROBOT_STATUS_VIEW
    public class RobotStatusType
    {
        public RobotStatusType(Transform root)
        {
            RobotStatusRoot_ = root;
            RobotPrefabList_.Add(RobotType.Hero, new RobotPrefab(root.Find("Hero")));
            RobotPrefabList_.Add(RobotType.Engineer, new RobotPrefab(root.Find("Engineer")));
            RobotPrefabList_.Add(RobotType.Infantry3, new RobotPrefab(root.Find("Infantry_1")));
            RobotPrefabList_.Add(RobotType.Infantry4, new RobotPrefab(root.Find("Infantry_2")));
            RobotPrefabList_.Add(RobotType.Sentry, new RobotPrefab(root.Find("Sentry")));
        }
        public Transform RobotStatusRoot_;

        public Dictionary<RobotType, RobotPrefab> RobotPrefabList_ = new Dictionary<RobotType, RobotPrefab>();
        public class RobotPrefab
        {
            public RobotPrefab(Transform robotPrefabRoot)
            {
                RobotNumber = robotPrefabRoot.Find("Number").GetComponent<TextMeshProUGUI>();
                RobotName = robotPrefabRoot.Find("Name").GetComponent<TextMeshProUGUI>();
                RobotState = robotPrefabRoot.Find("State").GetComponent<TextMeshProUGUI>();
                RobotHp = robotPrefabRoot.Find("HP").GetComponent<Slider>();
            }
            private TextMeshProUGUI RobotName;
            private TextMeshProUGUI RobotNumber;
            private TextMeshProUGUI RobotState;
            private Slider RobotHp;
            public void SetRobotName(string name)
            {
                RobotName.text = radar.data.RobotName.Chinese[Enum.Parse<RobotType>(name)];
            }
            public void SetRobotNumber(int number, Team team)
            {
                RobotNumber.color = (team == Team.Blue) ? Color.blue : Color.red;
                RobotNumber.text = ((team == Team.Blue) ? "B" : "R") + number.ToString();
            }
            public void SetRobotState(bool isTracked)
            {
                RobotState.text = isTracked ? "已被识别" : "未被识别";
                RobotState.color = isTracked ? Color.green : new Color(1.0f, 0.5f, 0.0f);
            }
            public void SetRobotHp(float hp, Team team)
            {
                RobotHp.maxValue = 200;
                RobotHp.minValue = 0;
                RobotHp.value = hp;
                RobotHp.gameObject.transform.Find("HPText").GetComponent<TextMeshProUGUI>().text = hp.ToString() + "/200";
                RobotHp.gameObject.transform.Find("Fill Area/Fill").GetComponent<Image>().color = team == Team.Blue ? Color.blue : Color.red;
            }


        };
    };
    #endregion
    public class TerminalBarType
    {
        public Transform TerminalBarRoot;
        public TextMeshProUGUI TerminalText;
        public TerminalBarType(Transform terminalBarRoot)
        {
            TerminalBarRoot = terminalBarRoot;
            TerminalText = terminalBarRoot.Find("Background/Info").GetComponent<TextMeshProUGUI>();

            // Using ui to display log will consume too many compute resources
            // LogManager.Instance.onLogUpdated_ += SetTerminalContent;

        }
        public void SetTerminalContent(string text)
        {
            TerminalText.text = text;
        }

    }
    #region INFOBAR_VIEW
    public class InfoBarViewType
    {
        public Transform InfoBarViewRoot;
        public GameTimeType GameTime;
        public EnemySideType EnemySide;
        public RobotStatusType RobotStatus;
        public TerminalBarType TerminalBar;

        public InfoBarViewType(Transform rootTransform)
        {
            InfoBarViewRoot = rootTransform.Find("InfoBarView");
            GameTime = new GameTimeType(rootTransform.Find("InfoBarView/GameTime"));
            EnemySide = new EnemySideType(rootTransform.Find("InfoBarView/EnemySide"));
            RobotStatus = new RobotStatusType(rootTransform.Find("InfoBarView/RobotStatus"));
            TerminalBar = new TerminalBarType(rootTransform.Find("InfoBarView/TerminalBar"));
        }
    };
    #endregion
    #region SWITCH_BUTTON_VIEW
    public class SwitchButtonViewType
    {
        public Transform SwitchButtonViewRoot;
        public Button CallibrationButton;
        public Button IOButton;

        public SwitchButtonViewType(Transform rootTransform)
        {
            SwitchButtonViewRoot = rootTransform.Find("SwitchButtonView");
            CallibrationButton = rootTransform.Find("SwitchButtonView/CallibrationButton").GetComponent<Button>();
            IOButton = rootTransform.Find("SwitchButtonView/IOButton").GetComponent<Button>();

            CallibrationButton.onClick.AddListener(() =>
            {
                UIManager.ShowPanel<CalibrationUI>();
                UIManager.GetPanel<CalibrationUI>().setCameraViewEnabled(true);
            });

            IOButton.onClick.AddListener(() =>
            {
                UIManager.ShowPanel<IOHandleUI>();
            });
        }


    };
    #endregion

    #region RAW_CAMERA_VIEW
    public class RawCameraViewType
    {
        public Transform RawCameraViewRoot;
        public RawImage RaycastCameraView;
        public TextMeshProUGUI CameraInfo;
        public Button SwitchInferenceButton;
        public GameObject InferenceIndicator;

        public RawCameraViewType(Transform rootTransform)
        {
            RawCameraViewRoot = rootTransform;
            RaycastCameraView = RawCameraViewRoot.Find("RawCameraView/Background/RaycastCameraView").GetComponent<RawImage>();
            CameraInfo = RawCameraViewRoot.Find("RawCameraView/Background/Info").GetComponent<TextMeshProUGUI>();
            SwitchInferenceButton = RawCameraViewRoot.Find("RawCameraView/Background/SwitchInferenceButton").GetComponent<Button>();
            InferenceIndicator = RawCameraViewRoot.Find("RawCameraView/Background/InferenceIndicator").gameObject;

            SwitchInferenceButton.onClick.AddListener(() =>
               {
                   Detector detector = GameObject.Find("Detector").GetComponent<Detector>();
                   detector.SwitchInference();

                   if (detector.ifInference_)
                   {
                       InferenceIndicator.GetComponent<Image>().color = Color.green;
                       InferenceIndicator.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = "识别运行中";
                   }
                   else
                   {
                       InferenceIndicator.GetComponent<Image>().color = Color.yellow;
                       InferenceIndicator.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = "识别已关闭";
                   }
               });
        }
    };
    #endregion



    public class MainUI : Panel
    {
        Canvas MainPanelCanvasRoot;
        InfoBarViewType InfoBarView;
        SwitchButtonViewType SwitchButtonView;
        RawCameraViewType RawCameraView;
        public GameObject robotPrefab_;
        public GameObject minimap_;
        private Dictionary<RobotType, GameObject> robotList_;

        private System.DateTime initialTime;
        private float countdownTime = 7 * 60;
        public override void Initialize()
        {
            MainPanelCanvasRoot = GetComponent<Canvas>();

            InfoBarView = new InfoBarViewType(MainPanelCanvasRoot.transform);
            SwitchButtonView = new SwitchButtonViewType(MainPanelCanvasRoot.transform);
            RawCameraView = new RawCameraViewType(MainPanelCanvasRoot.transform);

            minimap_ = GameObject.FindGameObjectWithTag("Minimap");
            robotPrefab_ = Resources.Load<GameObject>("Prefab/Robot");

            initialTime = System.DateTime.Now.AddMinutes(7);

            DataManager.Instance.OnDataUpdated += UpdateRobotPosition;
            DataManager.Instance.OnDataUpdated += UpdateGameState;
        }
        public override void Update()
        {
            UpdateGameTime();
        }

        private void UpdateGameTime()
        {
            countdownTime = (float)(initialTime - System.DateTime.Now).TotalSeconds;
            if (countdownTime < 0)
            {
                countdownTime = 0;
            }

            int minutes = Mathf.FloorToInt(countdownTime / 60);
            int seconds = Mathf.FloorToInt(countdownTime % 60);
            int milliseconds = Mathf.FloorToInt(countdownTime * 1000 % 1000);
            InfoBarView.GameTime.Time_.text = string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
        }
        private void UpdateGameState(StateDatas state)
        {
            InfoBarView.GameTime.SetStateName(state.gameState_.GameStage);
            initialTime = DateTime.Now.AddMinutes(7);

            InfoBarView.GameTime.SetRound(1, 3);
            InfoBarView.EnemySide.SetEnemySide(state.gameState_.EnemySide);
            foreach (var robot in InfoBarView.RobotStatus.RobotPrefabList_)
            {
                robot.Value.SetRobotHp(state.enemyRobots.Data[robot.Key].HP, state.gameState_.EnemySide);
                robot.Value.SetRobotNumber((int)robot.Key, state.gameState_.EnemySide);
                robot.Value.SetRobotName(robot.Key.ToString());
                robot.Value.SetRobotState(state.enemyRobots.Data[robot.Key].IsTracked);
            }
        }

        private void UpdateRobotPosition(StateDatas stateData)
        {
            if (robotList_ == null || robotList_.Count == 0)
            {
                robotList_ = new();
                List<RobotType> unInstantiatedRobots = new List<RobotType>(){
                        RobotType.Dart,
                        RobotType.Drone,
                        RobotType.Outpost,
                        RobotType.Base
                    };
                foreach (var robotState in stateData.enemyRobots.Data)
                {
                    if (unInstantiatedRobots.Contains(robotState.Key)) continue;

                    GameObject robot = Instantiate(robotPrefab_);
                    robotPrefab_.name = robotState.Key.ToString();
                    robot.transform.Find("Cylinder").GetComponent<Renderer>().material.color = stateData.gameState_.EnemySide == Team.Blue ? Color.blue : Color.red;
                    if (robotState.Key == RobotType.Unkown)
                        robot.transform.Find("Cylinder").GetComponent<Renderer>().material.color = Color.gray;
                    robot.GetComponentInChildren<TextMeshProUGUI>().text = robotState.Key.ToString();
                    robotList_.Add(robotState.Key, robot);
                }
            }
            foreach (var robotState in stateData.enemyRobots.Data)
            {
                Vector3 robotPosition;
                if (!robotState.Value.IsTracked)
                    robotPosition = new Vector3(0, minimap_.transform.position.y + 0.2f, 0);
                else
                    robotPosition = new Vector3(robotState.Value.Position.x, minimap_.transform.position.y + 0.2f, robotState.Value.Position.y);
                robotList_[robotState.Key].transform.position = robotPosition;
                robotList_[robotState.Key].transform.Find("Cylinder").GetComponent<Renderer>().material.color = stateData.gameState_.EnemySide == Team.Blue ? Color.blue : Color.red;
            }
        }

        public void OnDestroy()
        {
            if (!this.gameObject.scene.isLoaded) return;
            if (DataManager.Instance != null)
                DataManager.Instance.OnDataUpdated -= UpdateRobotPosition;
            if (robotList_ != null)
            {
                foreach (var robot in robotList_)
                {
                    Destroy(robot.Value);
                }
                robotList_.Clear();
            }
        }
    }
}