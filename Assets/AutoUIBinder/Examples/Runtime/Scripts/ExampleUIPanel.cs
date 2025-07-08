using UnityEngine;
using UnityEngine.UI;
using AutoUIBinder;

/// <summary>
/// AutoUIBinder使用示例 - 展示基本的UI面板实现
/// </summary>
public partial class ExampleUIPanel : AutoUIBinderBase
{
    //lobby panel prefab
    public GameObject LobbyPanelPrefab;
    public void Start()
    {
    }



    [UIEvent("Button_Close_Button", "onClick")]
    private void OnButton_Close_ButtononClick()
    {
        gameObject.SetActive(false);
    }

    [UIEvent("LoginButton_Button", "onClick")]
    private void OnLoginButton_ButtononClick()
    {
        Debug.Log($"login account: {Account_InputField.text}");
        Debug.Log($"login password: {Password_InputField.text}");

        //TODO 这里要根据自己的项目UI框架来实现
        Instantiate(LobbyPanelPrefab, transform.parent);
        gameObject.SetActive(false);
    }
}
