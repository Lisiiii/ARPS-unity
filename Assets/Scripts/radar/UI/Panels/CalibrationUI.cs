using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using radar.webcamera;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.WebCam;
using OpenCvSharp;
using System;
using radar.data;

namespace radar.ui.panel
{
    struct CalibrationType
    {
        public Transform CallibrationViewRoot;
        public RawImage CameraView;
        public Button returnButton;
        public Button CalibrateButton;
        public Slider alphaSlider;
        public Button clearButton;
        public Button closeInfoButton;
        public GameObject targetIndicatorPrefab_;
        public TextMeshProUGUI CallibrationText;
    };
    public class CalibrationUI : Panel
    {
        Canvas CollaborationPanelCanvasRoot_;
        CalibrationType CalibrationView_;
        public Camera currentCamera_;
        public bool isCalibrationViewEnabled_ = false;
        private bool isTinyMove_ = false;
        private int keyCode_ = 0;
        private readonly float moveSpeed_ = 0.01f;
        private readonly float rotateSpeed_ = 0.1f;
        private float speedScale_ = 1f;
        private List<GameObject> targetIndicators_ = new();
        private List<Vector2> screenPoints_ = new();
        private List<Vector3> worldPoints = new();
        public override void Initialize()
        {
            CollaborationPanelCanvasRoot_ = GetComponent<Canvas>();
            currentCamera_ = WebCameraHandler.Instance.selectedCamera_.raycastCamera_;
            CalibrationView_ = new CalibrationType
            {
                CallibrationViewRoot = CollaborationPanelCanvasRoot_.transform.Find("CalibrationView"),
                CameraView = CollaborationPanelCanvasRoot_.transform.Find("CalibrationView/CameraView").GetComponent<RawImage>(),
                returnButton = CollaborationPanelCanvasRoot_.transform.Find("CalibrationView/ReturnButton").GetComponent<Button>(),
                CalibrateButton = CollaborationPanelCanvasRoot_.transform.Find("CalibrationView/CalibrateButton").GetComponent<Button>(),
                CallibrationText = CollaborationPanelCanvasRoot_.transform.Find("CalibrationView/RawImage").transform.Find("CalibrationText").GetComponent<TextMeshProUGUI>(),
                alphaSlider = CollaborationPanelCanvasRoot_.transform.Find("CalibrationView/Slider").GetComponent<Slider>(),
                clearButton = CollaborationPanelCanvasRoot_.transform.Find("CalibrationView/ClearButton").GetComponent<Button>(),
                closeInfoButton = CollaborationPanelCanvasRoot_.transform.Find("CalibrationView/CloseInfoButton").GetComponent<Button>(),
                targetIndicatorPrefab_ = Resources.Load<GameObject>("Prefab/Indicator")
            };
            CalibrationView_.returnButton.onClick.AddListener(() =>
            {
                clearScreenPoints();
                UIManager.HidePanel<CalibrationUI>();
                setCameraViewEnabled(false);
            });
            CalibrationView_.closeInfoButton.onClick.AddListener(() =>
            {
                bool isActived = !CollaborationPanelCanvasRoot_.transform.Find("CalibrationView/RawImage").gameObject.activeSelf;
                CollaborationPanelCanvasRoot_.transform.Find("CalibrationView/RawImage").gameObject.SetActive(isActived);
            });
            CalibrationView_.clearButton.onClick.AddListener(() =>
            {
                clearScreenPoints();
            });
            CalibrationView_.CalibrateButton.onClick.AddListener(() =>
            {
                SolveCameraPose();
            });
            CalibrationView_.alphaSlider.onValueChanged.AddListener((value) =>
            {
                currentCamera_.transform.Find("Canvas/InputImage").GetComponent<RawImage>().color = new Color(1, 1, 1, value);
            });
            CalibrationView_.CameraView.texture = WebCameraHandler.Instance.selectedCamera_.cameraTexture_;
            setCameraViewEnabled(false);
            GetWorldPoint();

            WebCameraHandler.Instance.OnSelectedCameraChanged += UpdateCamera;
        }
        public void UpdateCamera()
        {
            if (WebCameraHandler.Instance.selectedCamera_ == null) return;
            CalibrationView_.CameraView.texture = WebCameraHandler.Instance.selectedCamera_.cameraTexture_;
            currentCamera_ = WebCameraHandler.Instance.selectedCamera_.raycastCamera_;
        }
        public void setCameraViewEnabled(bool enable)
        {
            CalibrationView_.CameraView.gameObject.SetActive(enable);
            isCalibrationViewEnabled_ = enable;
        }
        public override void Refresh()
        {
            if (isCalibrationViewEnabled_ == false) return;
            moveByKeys();
            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                // +----------+
                // |  +-------+
                // |  | Camera|
                // |  | Image |
                // +--+-------+
                Vector2 mousePos = Input.mousePosition;
                float scale_x = Screen.width / CalibrationView_.CameraView.rectTransform.rect.width;
                float scale_y = Screen.height / CalibrationView_.CameraView.rectTransform.rect.height;
                float offset_x = Screen.width - CalibrationView_.CameraView.rectTransform.rect.width;
                Vector2 oriScreenPoint = new(
                    mousePos.x < offset_x ? 0 : (mousePos.x - offset_x),
                    mousePos.y > CalibrationView_.CameraView.rectTransform.rect.height ? CalibrationView_.CameraView.rectTransform.rect.height : mousePos.y
                );
                Vector2 screenPoint = new(
                    oriScreenPoint.x * scale_x,
                    oriScreenPoint.y * scale_y
                );
                screenPoints_.Add(screenPoint);
                GameObject targetIndicator = Instantiate(CalibrationView_.targetIndicatorPrefab_, CalibrationView_.CameraView.transform);
                targetIndicator.transform.localPosition = new Vector3(
                    oriScreenPoint.x - CalibrationView_.CameraView.rectTransform.rect.width / 2,
                    oriScreenPoint.y - CalibrationView_.CameraView.rectTransform.rect.height / 2,
                    0);
                targetIndicators_.Add(targetIndicator);
                targetIndicator.transform.Find("Num").GetComponent<TextMeshProUGUI>().text = targetIndicators_.Count.ToString() + $":({screenPoint.x:F2}, {screenPoint.y:F2})";
            }
        }
        private void GetWorldPoint()
        {
            List<GameObject> worldPoints_ = GameObject.FindGameObjectsWithTag("Indicator").ToList();
            worldPoints_.Sort((a, b) => a.name.CompareTo(b.name));
            foreach (var worldPoint in worldPoints_)
            {
                Vector3 worldPos = worldPoint.transform.position;
                worldPoints.Add(new Vector3(worldPos.x, worldPos.y, worldPos.z));
            }

        }

        private void clearScreenPoints()
        {
            screenPoints_.Clear();
            foreach (var targetIndicator in targetIndicators_)
            {
                Destroy(targetIndicator);
            }
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

            currentCamera_.transform.Translate(moveDirection * moveSpeed_ * speedScale_, Space.Self);
            currentCamera_.transform.Rotate(rotateDirection * rotateSpeed_ * speedScale_, Space.Self);
            currentCamera_.fieldOfView += (isKeyPressed(KeyCode.Q) - isKeyPressed(KeyCode.E)) * 0.1f * speedScale_;
        }

        private void SolveCameraPose()
        {
            // 内存泄漏！暂时不上了

            // if (worldPoints.Count < 4 || screenPoints_.Count < 4)
            // {
            //     LogManager.Instance.warning("[UIManager]Need at least 4 point correspondences for PnP.");
            //     return;
            // }

            // // 1. Unity world coordinates (x, y, z) -> OpenCV world coordinates (x, -y, z)
            // // 2. Unity screen coordinates (x, y, origin at bottom-left) -> OpenCV pixel coordinates (x, y, origin at top-left)
            // // 3. Unity camera uses left-handed Y-up, OpenCV uses right-handed Z-forward
            // // 4. OpenCV's PnP output rvec/tvec is the transformation from world to camera

            // var objectPoints = new List<Point3f>();
            // var imagePoints = new List<Point2f>();
            // for (int i = 0; i < Math.Min(worldPoints.Count, screenPoints_.Count); i++)
            // {
            //     var wp = worldPoints[i];
            //     objectPoints.Add(new Point3f(wp.x, -wp.y, wp.z));
            //     Debug.Log($"worldPoints[{i}]: {wp.x}, {-wp.y}, {wp.z}");
            //     Debug.Log($"screenPoints_[{i}]: {screenPoints_[i].x}, {screenPoints_[i].y}");

            //     var sp = screenPoints_[i];
            //     float x_ = sp.x;
            //     float y_ = currentCamera_.pixelHeight - sp.y; // toggle y-axis
            //     imagePoints.Add(new Point2f(x_, y_));
            // }

            // // TODO: Change the camera matrix according to the camera parameters
            // float width = currentCamera_.pixelWidth;
            // float height = currentCamera_.pixelHeight;
            // float fovY = currentCamera_.fieldOfView;
            // float fy = height * 0.5f / Mathf.Tan(0.5f * fovY * Mathf.Deg2Rad);
            // float fx = fy * (21 / 16); // TODO
            // float cx = width / 2f;
            // float cy = height / 2f;
            // var cameraMatrix = new Mat(3, 3, MatType.CV_64FC1, Scalar.All(0));
            // cameraMatrix.Set<double>(0, 0, fx);
            // cameraMatrix.Set<double>(1, 1, fy);
            // cameraMatrix.Set<double>(0, 2, cx);
            // cameraMatrix.Set<double>(1, 2, cy);
            // cameraMatrix.Set<double>(2, 2, 1);

            // // TODO: Distortion coefficients
            // var distCoeffs = new Mat(1, 5, MatType.CV_64FC1, Scalar.All(0));

            // Mat objectPointsMat = new(objectPoints.Count, 1, MatType.CV_32FC3);
            // for (int i = 0; i < objectPoints.Count; i++)
            // {
            //     objectPointsMat.Set(i, 0, new Vec3f(objectPoints[i].X, objectPoints[i].Y, objectPoints[i].Z));
            // }
            // Mat imagePointsMat = new(imagePoints.Count, 1, MatType.CV_32FC2);
            // for (int i = 0; i < imagePoints.Count; i++)
            // {
            //     imagePointsMat.Set(i, 0, new Vec2f(imagePoints[i].X, imagePoints[i].Y));
            // }

            // var rvec = new Mat(3, 1, MatType.CV_64FC1);
            // currentCamera_.transform.localRotation.ToAngleAxis(out float angle, out Vector3 axis);
            // rvec.Set(0, 0, angle * axis.x);
            // rvec.Set(1, 0, angle * axis.y);
            // rvec.Set(2, 0, angle * axis.z);

            // var tvec = new Mat(3, 1, MatType.CV_64FC1);
            // tvec.Set(0, 0, currentCamera_.transform.localPosition.x);
            // tvec.Set(1, 0, currentCamera_.transform.localPosition.y);
            // tvec.Set(2, 0, currentCamera_.transform.localPosition.z);

            // Cv2.SolvePnP(
            // objectPointsMat,
            // imagePointsMat,
            // cameraMatrix,
            // distCoeffs,
            // rvec,
            // tvec,
            // false,
            // SolvePnPFlags.Iterative
            // );

            // // 打印结果
            // Debug.Log($"rvec: [{rvec.At<double>(0)}, {rvec.At<double>(1)}, {rvec.At<double>(2)}]");
            // Debug.Log($"tvec: [{tvec.At<double>(0)}, {tvec.At<double>(1)}, {tvec.At<double>(2)}]");

            // // 重投影误差
            // var projectedPoints = new Mat();
            // Cv2.ProjectPoints(objectPointsMat, rvec, tvec, cameraMatrix, distCoeffs, projectedPoints);

            // double totalError = 0;
            // for (int i = 0; i < imagePoints.Count; i++)
            // {
            //     var proj = projectedPoints.At<Point2f>(i);
            //     var orig = imagePoints[i];
            //     double err = Math.Sqrt(Math.Pow(proj.X - orig.X, 2) + Math.Pow(proj.Y - orig.Y, 2));
            //     totalError += err;
            //     Debug.Log($"Point {i}: projected=({proj.X:F2},{proj.Y:F2}), original=({orig.X:F2},{orig.Y:F2}), error={err:F2}");
            // }
            // Debug.Log($"Reprojection mean error: {totalError / imagePoints.Count:F2}");

            // // 反解相机在世界坐标系下的位置和姿态
            // // rvec/tvec是世界到相机的变换（R|t），要求相机在世界下的位置和朝向，需要反变换
            // // 世界到相机: Xc = R*Xw + t
            // // 相机到世界: Xw = R^T*(Xc - t)
            // // 相机在世界坐标系下的位置: C = -R^T * t

            // // 1. rvec转旋转矩阵
            // var R = new Mat();
            // Cv2.Rodrigues(rvec, R);

            // // 2. 取转置
            // var RtExpr = R.T();
            // var Rt = RtExpr.ToMat();

            // // 3. 相机位置 C = -R^T * t
            // var minusRtExpr = -Rt * tvec;
            // var minusRt = minusRtExpr.ToMat();

            // Vector3 CameraPosition = new(
            //     (float)minusRt.At<double>(0),
            //     -(float)minusRt.At<double>(1),
            //     (float)minusRt.At<double>(2)
            // );
            // Console.WriteLine("R:\n" + R.Dump());
            // Console.WriteLine("t:\n" + tvec.Dump());
            // Console.WriteLine("CameraPosition:\n" + CameraPosition);
            // // 4. 相机欧拉角（世界坐标系下的朝向）
            // // 用Rt（相机到世界的旋转）转为欧拉角
            // // OpenCV默认旋转顺序为XYZ
            // double sy = Math.Sqrt(Rt.At<double>(0, 0) * Rt.At<double>(0, 0) + Rt.At<double>(1, 0) * Rt.At<double>(1, 0));
            // bool singular = sy < 1e-6;
            // double x, y, z;
            // if (!singular)
            // {
            //     x = Math.Atan2(Rt.At<double>(2, 1), Rt.At<double>(2, 2));
            //     y = Math.Atan2(-Rt.At<double>(2, 0), sy);
            //     z = Math.Atan2(Rt.At<double>(1, 0), Rt.At<double>(0, 0));
            // }
            // else
            // {
            //     x = Math.Atan2(-Rt.At<double>(1, 2), Rt.At<double>(1, 1));
            //     y = Math.Atan2(-Rt.At<double>(2, 0), sy);
            //     z = 0;
            // }
            // var CameraEulerAngles = new Vector3(
            //     -(float)(x * Mathf.Rad2Deg),
            //     (float)(y * Mathf.Rad2Deg),
            //     -(float)(z * Mathf.Rad2Deg)
            // );

            // Debug.Log($"Camera Position (world): {CameraPosition}");
            // Debug.Log($"Camera Euler Angles (world): {CameraEulerAngles}");
            // WebCameraHandler.Instance.cameraAnchor.transform.SetPositionAndRotation(CameraPosition, Quaternion.Euler(CameraEulerAngles));
        }
    }
}