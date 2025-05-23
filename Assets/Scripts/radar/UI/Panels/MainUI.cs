using TMPro;
using UnityEngine;
using UnityEngine.UI;
using radar.detector;
using radar.data;
using System.Collections.Generic;
using radar.webcamera;

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
                    ((int)DataManager.Instance.stateData.gameState.GameStage + 1) % 5,
                    (stage) =>
                    {
                        DataManager.Instance.stateData.gameState.GameStage = (GameStage)stage;
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
                case GameStage.SelfCheck:
                    StateName_.color = new Color(1, 140f / 255f, 0);
                    break;
                case GameStage.Countdown:
                    StateName_.color = Color.red;
                    break;
                case GameStage.Started:
                    StateName_.color = new Color(0, 1, 1);
                    break;
                case GameStage.Finished:
                    StateName_.color = Color.black;
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
                    DataManager.Instance.stateData.gameState.EnemySide == Team.Blue ? Team.Red : Team.Blue,
                    (team) =>
                    {
                        DataManager.Instance.stateData.gameState.EnemySide = team;
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

        public Dictionary<RobotType, RobotPrefab> RobotPrefabList_ = new();
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
                RobotName.text = name;
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
                RobotHp.maxValue = hp;
                RobotHp.minValue = 0;
                RobotHp.value = hp;
                RobotHp.gameObject.transform.Find("HPText").GetComponent<TextMeshProUGUI>().text = hp.ToString() + "/" + RobotHp.maxValue.ToString();
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
        public Button SwitchCameraButton;
        public GameObject InferenceIndicator;

        public RawCameraViewType(Transform rootTransform)
        {
            RawCameraViewRoot = rootTransform;
            RaycastCameraView = RawCameraViewRoot.Find("RawCameraView/Background/RaycastCameraView").GetComponent<RawImage>();
            CameraInfo = RawCameraViewRoot.Find("RawCameraView/Background/Info").GetComponent<TextMeshProUGUI>();
            SwitchInferenceButton = RawCameraViewRoot.Find("RawCameraView/Background/SwitchInferenceButton").GetComponent<Button>();
            SwitchCameraButton = RawCameraViewRoot.Find("RawCameraView/Background/SwitchCameraButton").GetComponent<Button>();
            InferenceIndicator = RawCameraViewRoot.Find("RawCameraView/Background/InferenceIndicator").gameObject;

            UpdateCamera();
            WebCameraHandler.Instance.OnSelectedCameraChanged += UpdateCamera;

            SwitchCameraButton.onClick.AddListener(() =>
            {
                WebCameraHandler.Instance.ChangeSelectedCamera();
                CameraInfo.text = "当前相机:\n" + WebCameraHandler.Instance.selectedCamera_.root.name;
            });

            SwitchInferenceButton.onClick.AddListener(() =>
               {
                   Detector.Instance.SwitchInference();

                   if (Detector.Instance.ifInference_)
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

        public void UpdateCamera()
        {
            RaycastCameraView.texture = WebCameraHandler.Instance.selectedCamera_.cameraTexture_;
            if (WebCameraHandler.Instance.raycastCameras_.Count == 0)
                CameraInfo.text = "当前为测试视频流";
        }
    };
    #endregion

    #region TOPBAR_VIEW
    public class TopBarViewType
    {
        public class HPBarType
        {
            public Transform HPBarRoot;
            public TextMeshProUGUI BaseHPText;
            public TextMeshProUGUI OutpostHPText;
            public Slider BaseHPSlider;
            public Slider OutpostHPSlider;
            public HPBarType(Transform root, Team team)
            {
                HPBarRoot = root.transform.Find("TopBarView/" + (team == Team.Blue ? "Blue" : "Red"));
                BaseHPText = HPBarRoot.Find("BaseHPText").GetComponent<TextMeshProUGUI>();
                OutpostHPText = HPBarRoot.Find("OutpostHPText").GetComponent<TextMeshProUGUI>();
                BaseHPSlider = HPBarRoot.Find("BaseHPSlider").GetComponent<Slider>();
                OutpostHPSlider = HPBarRoot.Find("OutpostHPSlider").GetComponent<Slider>();
                BaseHPSlider.maxValue = 5000;
                OutpostHPSlider.maxValue = 1500;
            }

            public void SetBaseHP(float hp)
            {
                BaseHPSlider.value = hp;
                BaseHPText.text = hp.ToString() + "/" + BaseHPSlider.maxValue.ToString();
            }

            public void SetOutpostHP(float hp)
            {
                OutpostHPSlider.value = hp;
                OutpostHPText.text = hp.ToString() + "/" + OutpostHPSlider.maxValue.ToString();
            }
        }

        public Transform TopBarViewRoot;
        public HPBarType BlueHPBar;
        public HPBarType RedHPBar;
        public TopBarViewType(Transform rootTransform)
        {
            TopBarViewRoot = rootTransform;
            BlueHPBar = new HPBarType(TopBarViewRoot, Team.Blue);
            RedHPBar = new HPBarType(TopBarViewRoot, Team.Red);
            BlueHPBar.SetBaseHP(5000);
            BlueHPBar.SetOutpostHP(1500);
            RedHPBar.SetBaseHP(5000);
            RedHPBar.SetOutpostHP(1500);
        }
    };
    #endregion


    public class MainUI : Panel
    {
        Canvas MainPanelCanvasRoot;
        InfoBarViewType InfoBarView;
        SwitchButtonViewType SwitchButtonView;
        RawCameraViewType RawCameraView;
        TopBarViewType TopBarView;
        public GameObject robotPrefab_;
        // public GameObject minimap_;
        private Dictionary<RobotType, GameObject> robotList_;
        private float countdownTime = 7 * 60;
        public override void Initialize()
        {
            MainPanelCanvasRoot = GetComponent<Canvas>();

            InfoBarView = new InfoBarViewType(MainPanelCanvasRoot.transform);
            SwitchButtonView = new SwitchButtonViewType(MainPanelCanvasRoot.transform);
            RawCameraView = new RawCameraViewType(MainPanelCanvasRoot.transform);
            TopBarView = new TopBarViewType(MainPanelCanvasRoot.transform);

            // minimap_ = GameObject.FindGameObjectWithTag("Minimap");
            robotPrefab_ = Resources.Load<GameObject>("Prefab/Robot");

            DataManager.Instance.OnDataUpdated += UpdateRobotPosition;
            DataManager.Instance.OnDataUpdated += UpdateGameState;
            DataManager.Instance.OnDataUpdated += UpdateGameTime;
            DataManager.Instance.OnDoubleDebuffChancesEnabled += (chance) =>
            {
                if (chance > 0)
                    InfoBarView.TerminalBar.SetTerminalContent($"双倍易伤第{chance}次开启 ");
            };
        }

        private void UpdateGameTime(StateDatas stateDatas)
        {
            countdownTime = stateDatas.gameState.GameTimeSeconds;
            if (countdownTime < 0)
                countdownTime = 0;

            int minutes = Mathf.FloorToInt(countdownTime / 60);
            int seconds = Mathf.FloorToInt(countdownTime % 60);
            int milliseconds = Mathf.FloorToInt(countdownTime * 1000 % 1000);
            InfoBarView.GameTime.Time_.text = string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
        }
        private void UpdateGameState(StateDatas state)
        {
            InfoBarView.GameTime.SetStateName(state.gameState.GameStage);
            // initialTime = DateTime.Now.AddMinutes(7);

            InfoBarView.GameTime.SetRound(1, 3);
            InfoBarView.EnemySide.SetEnemySide(state.gameState.EnemySide);
            foreach (var robot in InfoBarView.RobotStatus.RobotPrefabList_)
            {
                robot.Value.SetRobotHp(state.enemyRobots.Data[robot.Key].HP, state.gameState.EnemySide);
                robot.Value.SetRobotNumber((int)robot.Key, state.gameState.EnemySide);
                robot.Value.SetRobotName(RobotName.Chinese[robot.Key]);
                robot.Value.SetRobotState(state.enemyRobots.Data[robot.Key].IsTracked);
            }

            RobotSets blueFacilities = state.gameState.EnemySide == Team.Blue ? state.enemyFacilities : state.allieFacilities;
            RobotSets redFacilities = state.gameState.EnemySide == Team.Red ? state.enemyFacilities : state.allieFacilities;
            TopBarView.BlueHPBar.SetBaseHP(blueFacilities.Data[RobotType.Base].HP);
            TopBarView.BlueHPBar.SetOutpostHP(blueFacilities.Data[RobotType.Outpost].HP);
            TopBarView.RedHPBar.SetBaseHP(redFacilities.Data[RobotType.Base].HP);
            TopBarView.RedHPBar.SetOutpostHP(redFacilities.Data[RobotType.Outpost].HP);
        }

        private void UpdateRobotPosition(StateDatas stateData)
        {
            if (robotList_ == null || robotList_.Count == 0)
            {
                robotList_ = new();
                List<RobotType> unInstantiatedRobots = new(){
                        RobotType.Dart,
                        RobotType.Drone,
                        RobotType.Outpost,
                        RobotType.Base
                    };
                foreach (var robotState in stateData.enemyRobots.Data)
                {
                    if (unInstantiatedRobots.Contains(robotState.Key)) continue;

                    GameObject robot = Instantiate(robotPrefab_);
                    robotPrefab_.name = RobotName.Chinese[robotState.Key];
                    robot.transform.Find("Cylinder").GetComponent<Renderer>().material.color = stateData.gameState.EnemySide == Team.Blue ? Color.blue : Color.red;
                    if (robotState.Key == RobotType.Unkown)
                        robot.transform.Find("Cylinder").GetComponent<Renderer>().material.color = Color.gray;
                    robot.GetComponentInChildren<TextMeshProUGUI>().text = robotState.Key.ToString();
                    robotList_.Add(robotState.Key, robot);
                }
            }
            foreach (var robotState in stateData.enemyRobots.Data)
            {
                Vector3 robotPosition = new(robotState.Value.Position.x, robotState.Value.Position.z + 0.2f, robotState.Value.Position.y);
                robotList_[robotState.Key].transform.position = robotPosition;
                robotList_[robotState.Key].transform.Find("Cylinder").GetComponent<Renderer>().material.color = stateData.gameState.EnemySide == Team.Blue ? Color.blue : Color.red;
            }
        }

        public void UpdateTerminalContent(string text)
        {
            InfoBarView.TerminalBar.SetTerminalContent(text);
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