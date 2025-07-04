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


    [UIEvent("Button_Close_Button", "m_OnClick")]
    private void OnButton_Close_ButtonClick()
    {
        // TODO: 添加事件处理逻辑
    }
}
