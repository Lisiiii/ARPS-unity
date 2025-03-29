using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace radar.ui.panel
{
    struct CalibrationType
    {
        public Transform CallibrationViewRoot;
        public RawImage CameraView;
        public Button returnButton;
        public TextMeshProUGUI CallibrationText;
    };
    public class CalibrationUI : Panel
    {
        Canvas CollaborationPanelCanvasRoot_;
        CalibrationType CalibrationView_;
        public RenderTexture raycastCameraTexture_;
        public Camera raycastCamera_;
        public bool isCalibrationViewEnabled_ = false;
        public override void Initialize()
        {
            CollaborationPanelCanvasRoot_ = GetComponent<Canvas>();
            raycastCamera_ = GameObject.Find("RayCastCamera").GetComponent<Camera>();
            CalibrationView_ = new CalibrationType
            {
                CallibrationViewRoot = CollaborationPanelCanvasRoot_.transform.Find("CalibrationView"),
                CameraView = CollaborationPanelCanvasRoot_.transform.Find("CalibrationView/CameraView").GetComponent<RawImage>(),
                returnButton = CollaborationPanelCanvasRoot_.transform.Find("CalibrationView/ReturnButton").GetComponent<Button>(),
                CallibrationText = CollaborationPanelCanvasRoot_.transform.Find("CalibrationView/CalibrationText").GetComponent<TextMeshProUGUI>()
            };
            CalibrationView_.returnButton.onClick.AddListener(() =>
            {
                UIManager.HidePanel<CalibrationUI>();
                setCameraViewEnabled(false);
            });
            CalibrationView_.CameraView.texture = raycastCameraTexture_;
            setCameraViewEnabled(false);
        }
        public void setCameraViewEnabled(bool enable)
        {
            CalibrationView_.CameraView.gameObject.SetActive(enable);
            isCalibrationViewEnabled_ = enable;
        }
        private bool isTinyMove_ = false;
        private int keyCode_ = 0;
        private float moveSpeed_ = 0.01f;
        private float rotateSpeed_ = 0.1f;
        private float speedScale_ = 1f;

        public override void Update()
        {
            if (isCalibrationViewEnabled_ == false) return;
            moveByKeys();
        }

        private KeyCode[] keys_ = {
                KeyCode.LeftShift, KeyCode.LeftControl,
                KeyCode.CapsLock,
                KeyCode.Space,
                KeyCode.Z, KeyCode.X,
                KeyCode.Q, KeyCode.E,
                KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D ,
                KeyCode.UpArrow, KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.DownArrow
                };
        private void getKeyboardInput()
        {
            keyCode_ = 0;
            for (int i = 0; i < keys_.Length; i++)
                if (Input.GetKey(keys_[i]))
                    keyCode_ += (int)Mathf.Pow(2, keys_.Length - 1 - i);
        }
        private int isKeyPressed(KeyCode keyCode)
        {
            int keyNumber = System.Array.IndexOf(keys_, keyCode);

            if (keyNumber < 0 || keyNumber >= keys_.Length) return 0;
            return (keyCode_ & (int)Mathf.Pow(2, keys_.Length - 1 - keyNumber)) > 0 ? 1 : 0;
        }

        private void moveByKeys()
        {
            getKeyboardInput();

            if (keyCode_ == 0) return;
            if (isKeyPressed(KeyCode.CapsLock) != 0) isTinyMove_ = !isTinyMove_;
            speedScale_ = isTinyMove_ ? 0.01f : 1f;

            Vector3 moveDirection = Vector3.forward * isKeyPressed(KeyCode.W) +
                                     Vector3.back * isKeyPressed(KeyCode.S) +
                                     Vector3.left * isKeyPressed(KeyCode.A) +
                                     Vector3.right * isKeyPressed(KeyCode.D) +
                                     Vector3.down * isKeyPressed(KeyCode.LeftControl) +
                                     Vector3.up * isKeyPressed(KeyCode.LeftShift);
            Vector3 rotateDirection = Vector3.left * isKeyPressed(KeyCode.UpArrow) +
                                     Vector3.right * isKeyPressed(KeyCode.DownArrow) +
                                     Vector3.down * isKeyPressed(KeyCode.LeftArrow) +
                                     Vector3.up * isKeyPressed(KeyCode.RightArrow) +
                                     Vector3.forward * isKeyPressed(KeyCode.Z) +
                                     Vector3.back * isKeyPressed(KeyCode.X);

            raycastCamera_.transform.Translate(moveDirection * moveSpeed_ * speedScale_, Space.Self);
            raycastCamera_.transform.Rotate(rotateDirection * rotateSpeed_ * speedScale_, Space.Self);
            raycastCamera_.fieldOfView += (isKeyPressed(KeyCode.Q) - isKeyPressed(KeyCode.E)) * 0.1f * speedScale_;
        }
    }
}