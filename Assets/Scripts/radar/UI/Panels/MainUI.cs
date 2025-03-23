using UnityEngine;
using UnityEngine.UI;

namespace radar.ui.panel
{
    public class MainUI : Panel
    {
        #region DEBUG_CODE_ONLY_CONTAINS_COUNTDOWN_TIMER
        Canvas main_panel_canvas_;
        Text game_time_text_;
        private System.DateTime initialTime;
        public override void Initialize()
        {
            main_panel_canvas_ = GetComponent<Canvas>();
            game_time_text_ = main_panel_canvas_.transform.Find("InfoBarView/GameTime/TimeText").GetComponent<Text>();
            initialTime = System.DateTime.Now.AddMinutes(7);
        }
        private float countdownTime = 7 * 60;
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

            game_time_text_.text = string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
        }
        public override void Update()
        {
            UpdateGameTime();
        }
        #endregion
    }
}