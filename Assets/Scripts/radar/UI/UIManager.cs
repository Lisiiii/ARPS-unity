using System.Collections.Generic;
using radar.ui.panel;
using UnityEngine;

namespace radar.ui
{
    public class UIManager : MonoBehaviour
    {
        public static Dictionary<string, Panel> _panels = new Dictionary<string, Panel>();

        public void Awake()
        {
            var uiPanelsPrefabs = Resources.LoadAll<Transform>("UI/Panel");
            foreach (var uiPanelPrefab in uiPanelsPrefabs)
            {
                if (!_panels.ContainsKey(uiPanelPrefab.name))
                {
                    Transform panel = Instantiate(uiPanelPrefab);
                    panel.name = panel.name.Replace("(Clone)", "");
                    _panels.Add(uiPanelPrefab.name, panel.GetComponent<Panel>());
                }
            }
            foreach (var panel in _panels)
            {
                panel.Value.Initialize();
                panel.Value.Hide();
            }
            ShowPanel<MainUI>();
        }

        public void Update()
        {
            foreach (var panel in _panels)
            {
                panel.Value.Update();
            }
        }
        public static T GetPanel<T>() where T : Panel
        {
            foreach (var panel in _panels)
                if (panel.Value is T)
                    return (T)panel.Value;
            return null;
        }

        public static void ShowPanel<T>() where T : Panel
        {
            foreach (var panel in _panels)
                if (panel.Value is T)
                    panel.Value.Show();
        }

        public static void HidePanel<T>() where T : Panel
        {
            foreach (var panel in _panels)
                if (panel.Value is T)
                    panel.Value.Hide();
        }
    }

}