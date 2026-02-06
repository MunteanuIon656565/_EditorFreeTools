#if UNITY_6000_3_OR_NEWER
using UnityEditor.Toolbars;
using UnityEngine;

public class TimeScaleToolbarSlider
{
    const float k_MinTimeScale = 0f;
    const float k_MaxTimeScale = 10f;

    [MainToolbarElement("My Tools/Time Scale Slider", defaultDockPosition = MainToolbarDockPosition.Middle)]
    public static MainToolbarElement TimeSlider()
    {
        var content = new MainToolbarContent("Time Scale", "Time Scale");
        return new MainToolbarSlider(content, Time.timeScale, k_MinTimeScale, k_MaxTimeScale, OnSliderValueChanged);
    }
    static void OnSliderValueChanged(float newValue)
    {
        if (!Application.isPlaying) return;
        Time.timeScale = newValue;
    }
}
#endif

