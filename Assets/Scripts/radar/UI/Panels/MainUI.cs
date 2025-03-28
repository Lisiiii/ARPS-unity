using radar.Yolov8;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace radar.ui.panel
{
    #region INFOBAR_VIEW
    struct InfoBarViewType
    {
        public Transform InfoBarViewRoot;
        public GameTimeType GameTime;
        public EnemySideType EnemySide;
        public RobotStatusType RobotStatus;
        public TerminalBarType TerminalBar;
    };
    struct GameTimeType
    {
        public Transform GameTimeRoot;
        public TextMeshProUGUI StateName;
        public TextMeshProUGUI Time;
        public TextMeshProUGUI Round;
    };
    struct EnemySideType
    {
        public Transform EnemySideRoot;
        public TextMeshProUGUI Title;
        public TextMeshProUGUI Color;
        public Button SwitchButton;
    };
    struct RobotStatusType
    {
        public Transform RobotStatusRoot;
        public Transform Hero;
        public Transform Engineer;
        public Transform Infantry_1;
        public Transform Infantry_2;
        public Transform Sentry;
    };
    struct TerminalBarType
    {
        public Transform TerminalBarRoot;
        public TextMeshProUGUI TerminalText;
    };
    #endregion

    #region SWITCH_BUTTON_VIEW
    struct SwitchButtonViewType
    {
        public Transform SwitchButtonViewRoot;
        public Button CallibrationButton;
        public Button IOButton;
    };
    #endregion

    #region RAW_CAMERA_VIEW
    struct RawCameraViewType
    {
        public Transform RawCameraViewRoot;
        public RawImage RaycastCameraView;
        public TextMeshProUGUI CameraInfo;
        public Button SwitchInferenceButton;
        public GameObject InferenceIndicator;
    };
    #endregion



    public class MainUI : Panel
    {
        Canvas MainPanelCanvasRoot;
        InfoBarViewType InfoBarView;
        SwitchButtonViewType SwitchButtonView;
        RawCameraViewType RawCameraView;
        public override void Initialize()
        {
            MainPanelCanvasRoot = GetComponent<Canvas>();

            InitializeInfoBarView();
            InitializeSwitchButtonView();
            InitializeRawCameraView();

            initialTime = System.DateTime.Now.AddMinutes(7);
        }
        public override void Update()
        {
            UpdateGameTime();
        }

        private System.DateTime initialTime;
        private float countdownTime = 7 * 60;
        private void InitializeInfoBarView()
        {
            InfoBarView = new InfoBarViewType
            {
                InfoBarViewRoot = MainPanelCanvasRoot.transform.Find("InfoBarView"),
                GameTime = new GameTimeType
                {
                    GameTimeRoot = MainPanelCanvasRoot.transform.Find("InfoBarView/GameTime"),
                    StateName = MainPanelCanvasRoot.transform.Find("InfoBarView/GameTime/StateName").GetComponent<TextMeshProUGUI>(),
                    Time = MainPanelCanvasRoot.transform.Find("InfoBarView/GameTime/Time").GetComponent<TextMeshProUGUI>(),
                    Round = MainPanelCanvasRoot.transform.Find("InfoBarView/GameTime/Round").GetComponent<TextMeshProUGUI>()
                },
                EnemySide = new EnemySideType
                {
                    EnemySideRoot = MainPanelCanvasRoot.transform.Find("InfoBarView/EnemySide"),
                    Title = MainPanelCanvasRoot.transform.Find("InfoBarView/EnemySide/Title").GetComponent<TextMeshProUGUI>(),
                    Color = MainPanelCanvasRoot.transform.Find("InfoBarView/EnemySide/Color").GetComponent<TextMeshProUGUI>(),
                    SwitchButton = MainPanelCanvasRoot.transform.Find("InfoBarView/EnemySide/SwitchButton").GetComponent<Button>()
                },
                RobotStatus = new RobotStatusType
                {
                    RobotStatusRoot = MainPanelCanvasRoot.transform.Find("InfoBarView/RobotStatus"),
                    Hero = MainPanelCanvasRoot.transform.Find("InfoBarView/RobotStatus/Hero"),
                    Engineer = MainPanelCanvasRoot.transform.Find("InfoBarView/RobotStatus/Engineer"),
                    Infantry_1 = MainPanelCanvasRoot.transform.Find("InfoBarView/RobotStatus/Infantry_1"),
                    Infantry_2 = MainPanelCanvasRoot.transform.Find("InfoBarView/RobotStatus/Infantry_2"),
                    Sentry = MainPanelCanvasRoot.transform.Find("InfoBarView/RobotStatus/Sentry")
                },
                TerminalBar = new TerminalBarType
                {
                    TerminalBarRoot = MainPanelCanvasRoot.transform.Find("InfoBarView/TerminalBar"),
                    TerminalText = MainPanelCanvasRoot.transform.Find("InfoBarView/TerminalBar/Background/Info").GetComponent<TextMeshProUGUI>()
                }
            };
        }
        private void InitializeSwitchButtonView()
        {
            SwitchButtonView = new SwitchButtonViewType
            {
                SwitchButtonViewRoot = MainPanelCanvasRoot.transform.Find("SwitchButtonView"),
                CallibrationButton = MainPanelCanvasRoot.transform.Find("SwitchButtonView/CallibrationButton").GetComponent<Button>(),
                IOButton = MainPanelCanvasRoot.transform.Find("SwitchButtonView/IOButton").GetComponent<Button>()
            };
            // TODO: CallibrationUI
            // SwitchButtonView.CallibrationButton.onClick.AddListener(() =>
            // {
            //     UIManager.HidePanel<MainUI>();
            //     UIManager.ShowPanel<CallibrationUI>();
            // });
            SwitchButtonView.IOButton.onClick.AddListener(() =>
            {
                UIManager.ShowPanel<IOHandleUI>();
            });
        }
        private void InitializeRawCameraView()
        {
            RawCameraView = new RawCameraViewType
            {
                RawCameraViewRoot = MainPanelCanvasRoot.transform.Find("RawCameraView"),
                RaycastCameraView = MainPanelCanvasRoot.transform.Find("RawCameraView/Background/RaycastCameraView").GetComponent<RawImage>(),
                CameraInfo = MainPanelCanvasRoot.transform.Find("RawCameraView/Background/Info").GetComponent<TextMeshProUGUI>(),
                SwitchInferenceButton = MainPanelCanvasRoot.transform.Find("RawCameraView/Background/SwitchInferenceButton").GetComponent<Button>(),
                InferenceIndicator = MainPanelCanvasRoot.transform.Find("RawCameraView/Background/InferenceIndicator").gameObject
            };
            RawCameraView.SwitchInferenceButton.onClick.AddListener(() =>
            {
                RayCaster YoloInferencer = GameObject.Find("Yolov8Inferencer").GetComponent<RayCaster>();
                YoloInferencer.ifInference_ = !YoloInferencer.ifInference_;

                if (YoloInferencer.ifInference_)
                {
                    RawCameraView.InferenceIndicator.GetComponent<Image>().color = Color.green;
                    RawCameraView.InferenceIndicator.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = "识别运行中";
                }
                else
                {
                    RawCameraView.InferenceIndicator.GetComponent<Image>().color = Color.yellow;
                    RawCameraView.InferenceIndicator.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = "识别已关闭";
                }
            });
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
            int milliseconds = Mathf.FloorToInt((countdownTime * 1000) % 1000);
            InfoBarView.GameTime.Time.text = string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
        }
    }
}