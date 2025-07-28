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
        Button_Close_Button.onClick.AddListener(OnButton_Close_ButtonOnClick);
        LoginButton_Button.onClick.AddListener(OnLoginButton_ButtonOnClick);
    }
    private void OnButton_Close_ButtonOnClick()
    {
        gameObject.SetActive(false);
    }

    private void OnLoginButton_ButtonOnClick()
    {
        Debug.Log($"login account: {Account_InputField.text}");
        Debug.Log($"login password: {Password_InputField.text}");

        //TODO 这里要根据自己的项目UI框架来实现
        Instantiate(LobbyPanelPrefab, transform.parent);
        gameObject.SetActive(false);
    }
}
