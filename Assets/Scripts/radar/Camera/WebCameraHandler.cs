using System;
using System.Collections;
using System.Collections.Generic;
using radar.data;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.Video;

namespace radar.webcamera
{
    public class RaycastCameraType
    {
        public GameObject root;
        public WebCamTexture webCamTexture_;
        public RenderTexture renderTexture_;
        public Texture2D outputTexture2D_;
        public Camera raycastCamera_;
        public RenderTexture cameraTexture_;
        public RawImage raycastCameraImage_;
        public RaycastCameraType(GameObject parentObject, RenderTexture renderTexture, string name = "RaycastCamera(Default)")
        {
            // Instantiate
            root = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Prefab/RaycastCamera"));
            root.transform.SetParent(parentObject.transform);
            root.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;
            root.name = name.Replace("(Clone)", "");

            // Set up camera
            raycastCamera_ = root.GetComponent<Camera>();
            cameraTexture_ = new RenderTexture(renderTexture.width, renderTexture.height, 1);
            cameraTexture_.name = name + "_CameraTexture";
            cameraTexture_.Create();
            raycastCamera_.targetTexture = cameraTexture_;

            raycastCameraImage_ = root.transform.Find("Canvas/InputImage").GetComponent<RawImage>();
            raycastCameraImage_.texture = renderTexture;

            webCamTexture_ = new WebCamTexture();
            renderTexture_ = renderTexture;
            outputTexture2D_ = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
        }

        public void AsyncTransRenderTextureToTexture2D()
        {
            if (webCamTexture_ != null && webCamTexture_.isPlaying && renderTexture_ != null)
            {
                Graphics.Blit(webCamTexture_, renderTexture_);
            }
            RenderTexture.active = renderTexture_;
            AsyncGPUReadback.Request(renderTexture_, 0, TextureFormat.RGBA32, request =>
            {
                if (!request.hasError)
                {
                    var data = request.GetData<byte>();
                    if (data == null || data.Length == 0 || outputTexture2D_ == null)
                        return;
                    outputTexture2D_.LoadRawTextureData(data);
                    outputTexture2D_.Apply();
                }
            });
        }
        ~RaycastCameraType()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (root != null)
            {
                raycastCamera_.targetTexture = null;
                raycastCamera_.gameObject.SetActive(false);
                raycastCamera_.enabled = false;
                raycastCameraImage_.texture = null;
                raycastCameraImage_.gameObject.SetActive(false);
                raycastCameraImage_.enabled = false;
                webCamTexture_.Stop();
                webCamTexture_ = null;
                renderTexture_.Release();
                renderTexture_ = null;

                UnityEngine.Object.Destroy(root);
            }
        }

    }
    public class WebCameraHandler : MonoBehaviour
    {
        public static WebCameraHandler Instance
        {
            get
            {
                if (instance_ == null)
                {
                    instance_ = FindAnyObjectByType<WebCameraHandler>();
                    if (instance_ == null)
                    {
                        GameObject obj = new GameObject("WebCameraHandler");
                        instance_ = obj.AddComponent<WebCameraHandler>();
                    }
                }
                return instance_;
            }
        }
        private static WebCameraHandler instance_;
        private VideoPlayer videoPlayer_;
        private RaycastCameraType defaultCamera;
        private int selectedIndex = 0;
        public WebCamDevice[] devices_;
        public GameObject cameraAnchor;
        public List<RaycastCameraType> raycastCameras_;
        public RaycastCameraType selectedCamera_;
        public Action OnSelectedCameraChanged;
        public event Action<WebCamDevice[]> OnCameraScanned;

        void Awake()
        {
            LogManager.Instance.log("[WebCameraHandler]Starting...");
            if (videoPlayer_ == null)
                videoPlayer_ = FindFirstObjectByType<VideoPlayer>();
            raycastCameras_ = new List<RaycastCameraType>();

            RenderTexture renderTexture = new RenderTexture(1920, 1080, 1);
            renderTexture.name = "DefaultCamera_RenderTexture";
            renderTexture.Create();
            videoPlayer_.targetTexture = renderTexture;
            defaultCamera = new RaycastCameraType(cameraAnchor, renderTexture)
            {
                webCamTexture_ = null
            };
            videoPlayer_.enabled = true;
            videoPlayer_.Play();

            selectedCamera_ = defaultCamera;
        }

        public void ChangeSelectedCamera()
        {
            if (selectedCamera_ == defaultCamera)
                return;
            selectedIndex++;
            selectedIndex %= raycastCameras_.Count;

            selectedCamera_ = raycastCameras_[selectedIndex];
            OnSelectedCameraChanged?.Invoke();
        }

        void Update()
        {
            if (raycastCameras_.Count == 0)
            {
                if (selectedCamera_ != defaultCamera)
                {
                    selectedCamera_ = defaultCamera;
                    OnSelectedCameraChanged?.Invoke();
                }
                defaultCamera.AsyncTransRenderTextureToTexture2D();
            }
            else
            {
                foreach (var raycastCamera in raycastCameras_)
                {
                    raycastCamera.AsyncTransRenderTextureToTexture2D();
                }
            }

        }
        public void ScanCamera()
        {
            StartCoroutine("ToOpenCamera");
        }
        private IEnumerator ToOpenCamera()
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            if (Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                foreach (var raycastCamera in raycastCameras_)
                {
                    if (raycastCamera.webCamTexture_ != null)
                        raycastCamera.webCamTexture_.Stop();
                }

                for (int i = 0; i < 10; i++)
                {
                    if (WebCamTexture.devices.Length > 0)
                        break;
                    yield return new WaitForEndOfFrame();
                }
                devices_ = WebCamTexture.devices;
                OnCameraScanned?.Invoke(devices_);
                LogManager.Instance.log("[WebCameraHandler]Camera devices scanned: " + devices_.Length + " devices found.");
                for (int i = 0; i < devices_.Length; i++)
                    LogManager.Instance.log("[WebCameraHandler]--- Camera device " + i + ": " + devices_[i].name);
            }
            else
                LogManager.Instance.error("[WebCameraHandler]Camera permission denied.");

        }

        public bool OpenCamera(string devicename)
        {
            videoPlayer_.enabled = false;
            defaultCamera.root.SetActive(false);
            defaultCamera.raycastCamera_.enabled = false;

            int height = 1080, width = 1920, refreshRateRatio = 60;

            if (WebCamTexture.devices.Length <= 0)
            {
                LogManager.Instance.error("[WebCameraHandler]No camera devices found.");
                return false;
            }

            bool isFound = false;
            for (int i = 0; i < WebCamTexture.devices.Length; i++)
                if (WebCamTexture.devices[i].name.CompareTo(devicename) == 0)
                {
                    isFound = true;
                    break;
                }
            if (!isFound)
            {
                LogManager.Instance.error("[WebCameraHandler]Camera device not found.");
                return false;
            }

            WebCamTexture newWebCamTexture;
            newWebCamTexture = new(devicename, width, height, refreshRateRatio)
            {
                wrapMode = TextureWrapMode.Repeat,
            };
            newWebCamTexture.Play();

            if (newWebCamTexture.isPlaying)
            {
                LogManager.Instance.log($"[WebCameraHandler]{devicename} opened successfully.");
                RenderTexture renderTexture = new RenderTexture(width, height, 1);
                renderTexture.name = (devicename + "_RenderTexture").Replace(" ", "_");
                renderTexture.Create();
                RaycastCameraType raycastCamera = new RaycastCameraType(cameraAnchor, renderTexture, devicename);
                raycastCamera.webCamTexture_ = newWebCamTexture;
                raycastCameras_.Add(raycastCamera);
                selectedCamera_ = raycastCamera;
                OnSelectedCameraChanged?.Invoke();

                return true;
            }
            else
            {
                LogManager.Instance.error($"[WebCameraHandler]Failed to open {devicename}.");
                return false;
            }
        }

        private void OnApplicationPause(bool pause)
        {
            if (raycastCameras_ == null || raycastCameras_.Count == 0)
            {
                if (pause)
                    videoPlayer_.Pause();
                else
                    videoPlayer_.Play();
            }
            else
                foreach (var raycastCamera in raycastCameras_)
                    if (raycastCamera.webCamTexture_ != null)
                        if (pause)
                            raycastCamera.webCamTexture_.Pause();
                        else
                            raycastCamera.webCamTexture_.Play();
        }

        private void OnDestroy()
        {
            CloseAllCamera();
        }

        public void CloseAllCamera()
        {
            StopAllCoroutines();
            if (raycastCameras_.Count == 0)
                return;
            else
            {
                foreach (var raycastCamera in raycastCameras_)
                {
                    if (raycastCamera.webCamTexture_ != null)
                        raycastCamera.webCamTexture_.Stop();
                    raycastCamera.Dispose();
                }
                raycastCameras_.Clear();
            }

            if (videoPlayer_ != null)
            {
                videoPlayer_.enabled = true;
                defaultCamera.root.SetActive(true);
                defaultCamera.raycastCamera_.enabled = true;
            }
        }
    }
}
