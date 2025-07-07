using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AutoUIBinder使用示例 - 展示基本的UI面板实现
/// </summary>
public partial class ExampleUIPanel : AutoUIBinderBase
{
    public void Start()
    {
    }

    [UIEvent("Toggle_2_Toggle", "onValueChanged")]
    private void OnToggle_2_ToggleonValueChanged(bool value)
    {
        
    }
}
