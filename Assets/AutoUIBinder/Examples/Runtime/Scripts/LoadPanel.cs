using System.Collections;
using System.Collections.Generic;
using AutoUIBinder;
using UnityEngine;

public partial class LoadPanel : AutoUIBinderBase
{
    private void OnSlider_SlideronValueChanged(float value)
    {
        Debug.Log($"场景加载进度: {value}");
        if (value >= 0.95)
        {
            Slider_Slider.value = 1;
            gameObject.SetActive(false);
        }
    }
    void Start()
    {
        Slider_Slider.onValueChanged.AddListener(OnSlider_SlideronValueChanged);
        StartCoroutine(LoadScene());
    }
    IEnumerator LoadScene()
    {
        for (int i = 0; i < 100; i++)
        {
            yield return new WaitForSeconds(0.015f);
            Slider_Slider.value = i / 100f;
        }
    }
}
