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
    private void OnButton_Close_ButtonClick()
    {
        
    }

    [UIEvent("InputField_Legacy_InputField", "onSubmit")]
    private void OnInputField_Legacy_InputFieldSubmit(string value)
    {
        
    }



}
