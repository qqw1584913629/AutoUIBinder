using UnityEngine;
using UnityEngine.UI;
using AutoUIBinder;

/// <summary>
/// AutoUIBinder使用示例 - 展示基本的UI面板实现
/// </summary>
public partial class ExampleUIPanel : AutoUIBinderBase
{
    public void Start()
    {
    }

    [UIEvent("Button_Close_Button", "onClick")]
    private void OnButton_Close_ButtononClick()
    {
        Debug.Log("[ExampleUIPanel] Close button clicked!");
    }

    [UIEvent("InputField_Legacy_InputField", "onSubmit")]
    private void OnInputField_Legacy_InputFieldonSubmit(string value)
    {
        Debug.LogError(value);
    }


}
