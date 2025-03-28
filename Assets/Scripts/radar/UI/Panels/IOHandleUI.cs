using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.VisualScripting;

using System.IO.Ports;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace radar.ui.panel
{
    public class IOHandleUI : Panel
    {
        Canvas IOHandlePanelCanvasRoot;
        struct COMViewType
        {
            public Transform COMViewRoot;
            public Button ScanButton;
            public Button CloseButton;
            public Transform COMViewListRoot;
            public TextMeshProUGUI Info;
            public string[] ports;
            public Transform COMSubmenuPrefab;
        }
        COMViewType COMView;

        Button SwitchToMainButton;
        serial.SerialHandler SerialHandler;
        Transform CameraViewListRoot;
        public override void Initialize()
        {
            IOHandlePanelCanvasRoot = GetComponent<Canvas>();

            COMView = new COMViewType
            {
                COMViewRoot = IOHandlePanelCanvasRoot.transform.Find("COMView"),
                ScanButton = IOHandlePanelCanvasRoot.transform.Find("COMView/ScanButton").GetComponent<Button>(),
                CloseButton = IOHandlePanelCanvasRoot.transform.Find("COMView/CloseButton").GetComponent<Button>(),
                COMViewListRoot = IOHandlePanelCanvasRoot.transform.Find("COMView/List"),
                Info = IOHandlePanelCanvasRoot.transform.Find("COMView/Info").GetComponent<TextMeshProUGUI>(),
                ports = new string[0],
                COMSubmenuPrefab = Resources.Load<Transform>("Prefab/COMSubmenu")
            };

            SwitchToMainButton = IOHandlePanelCanvasRoot.transform.Find("SwitchToMainButton").GetComponent<Button>();
            SwitchToMainButton.onClick.AddListener(() =>
            {
                UIManager.HidePanel<IOHandleUI>();
            });

            CameraViewListRoot = IOHandlePanelCanvasRoot.transform.Find("CameraView/List");
            SerialHandler = GameObject.Find("SerialHandler").transform.GetComponent<serial.SerialHandler>();

            AddCOMSubmenu();

            COMView.ScanButton.onClick.AddListener(() =>
            {
                COMView.ports = SerialHandler.GetComponent<radar.serial.SerialHandler>().ScanPorts();
                foreach (Transform child in COMView.COMViewListRoot)
                    Destroy(child.gameObject);
                if (COMView.ports.Length > 0)
                    foreach (string port in COMView.ports)
                        AddCOMSubmenu(port);
                else
                    AddCOMSubmenu();
            });
            COMView.CloseButton.onClick.AddListener(() =>
            {
                if (SerialHandler.ClosePort())
                    COMView.Info.text = "已关闭端口";
                else
                    COMView.Info.text = "关闭端口失败";
            });
        }
        public override void Update()
        {
        }
        private void AddCOMSubmenu(string portName = null)
        {
            Transform COMSubmenu = Instantiate(COMView.COMSubmenuPrefab, COMView.COMViewListRoot);
            if (portName == null)
            {
                COMSubmenu.Find("Button/Text").GetComponent<TextMeshProUGUI>().text = "无端口";
                COMSubmenu.Find("Button").GetComponent<Button>().interactable = false;
            }
            else
            {
                COMSubmenu.name = portName;
                COMSubmenu.Find("Button/Text").GetComponent<TextMeshProUGUI>().text = portName;
                COMSubmenu.Find("Button").GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f);
                COMSubmenu.Find("Button").GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (SerialHandler.ClosePort())
                        COMView.Info.text = "已关闭端口";
                    else
                        COMView.Info.text = "关闭端口失败";
                    if (SerialHandler.Connect(portName, 9600, Parity.None, 8, StopBits.One))
                        COMView.Info.text = "已连接到 " + portName;
                    else
                        COMView.Info.text = "连接失败";
                });
            }
        }
    }
}