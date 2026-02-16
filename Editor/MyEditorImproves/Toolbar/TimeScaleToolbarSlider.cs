#if UNITY_6000_3_OR_NEWER
using UnityEditor.Toolbars;
using UnityEngine;

namespace Plugins._EditorFreeTools.Editor.MyEditorImproves.Toolbar
{
    public class TimeScaleToolbarSlider
    {
        private const float KMinTimeScale = 0f;
        private const float KMaxTimeScale = 10f;

        [MainToolbarElement("My Tools/Time Scale Slider", defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement TimeSlider()
        {
            var content = new MainToolbarContent("Time Scale", "Time Scale");
            return new MainToolbarSlider(content, Time.timeScale, KMinTimeScale, KMaxTimeScale, OnSliderValueChanged);
        }
        static void OnSliderValueChanged(float newValue)
        {
            if (!Application.isPlaying) return;
            Time.timeScale = newValue;
        }
    }
}
#endif

