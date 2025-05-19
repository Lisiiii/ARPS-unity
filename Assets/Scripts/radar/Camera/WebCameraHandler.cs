using System.Collections;
using System.Collections.Generic;
using radar.data;
using UnityEngine;
using UnityEngine.UI;

namespace radar.webcamera
{
    public class WebCameraHandler : MonoBehaviour
    {
        public RenderTexture inputTexture_;
        private WebCamTexture webCamTexture_;
        public WebCamDevice[] devices_;
        public event System.Action<WebCamDevice[]> OnCameraScanned;

        void Start()
        {
            LogManager.Instance.log("[WebCameraHandler]Starting...");
        }

        void Update()
        {
            if (webCamTexture_ != null && webCamTexture_.isPlaying)
            {
                Graphics.Blit(webCamTexture_, inputTexture_);
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
                if (webCamTexture_ != null)
                    webCamTexture_.Stop();

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
            int height = inputTexture_.height;
            int width = inputTexture_.width;
            if (WebCamTexture.devices.Length <= 0)
            {
                LogManager.Instance.error("[WebCameraHandler]No camera devices found.");
                return false;
            }
            int i = 0;
            for (; i < WebCamTexture.devices.Length; i++)
                if (WebCamTexture.devices[i].name.CompareTo(devicename) == 0)
                    break;
            if (i == WebCamTexture.devices.Length)
            {
                LogManager.Instance.error("[WebCameraHandler]Camera device not found.");
                return false;
            }

            webCamTexture_ = new WebCamTexture(devicename, width, height, 60)
            {
                wrapMode = TextureWrapMode.Repeat
            };
            webCamTexture_.Play();

            if (webCamTexture_.isPlaying)
                LogManager.Instance.log("[WebCameraHandler]Camera opened successfully.");
            else
            {
                LogManager.Instance.error("[WebCameraHandler]Failed to open camera.");
                return false;
            }
            return true;
        }

        private void OnApplicationPause(bool pause)
        {
            if (webCamTexture_ != null)
                if (pause)
                    webCamTexture_.Pause();
                else
                    webCamTexture_.Play();
        }

        private void OnDestroy()
        {
            CloseCamera();
        }

        public void CloseCamera()
        {
            StopAllCoroutines();
            if (webCamTexture_ != null)
            {
                webCamTexture_.Stop();
                webCamTexture_ = null;
            }
        }
    }
}
