using System.Collections;
using System.Collections.Generic;
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
            }
            else
                Debug.Log("Camera permission denied.");

        }

        public bool OpenCamera(string devicename)
        {
            int height = inputTexture_.height;
            int width = inputTexture_.width;
            if (WebCamTexture.devices.Length <= 0)
                return false;
            int i = 0;
            for (; i < WebCamTexture.devices.Length; i++)
                if (WebCamTexture.devices[i].name.CompareTo(devicename) == 0)
                    break;
            if (i == WebCamTexture.devices.Length)
                return false;

            webCamTexture_ = new WebCamTexture(devicename, width, height, 60)
            {
                wrapMode = TextureWrapMode.Repeat
            };
            webCamTexture_.Play();

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
