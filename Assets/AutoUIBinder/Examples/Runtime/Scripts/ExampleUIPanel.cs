using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UITool使用示例 - 展示基本的UI面板实现
/// </summary>
public partial class ExampleUIPanel : AutoUIBinderBase
{
    public void Start()
    {
        Button_Close_Button.onClick.AddListener(OnCloseButtonClick);
        Text_Title_1_1_1_Text.text = "123";
        Button_Close_Image.color = Color.red;
    }

    /// <summary>
    /// 关闭按钮点击事件示例
    /// </summary>
    private void OnCloseButtonClick()
    {
        Debug.Log("[ExampleUIPanel] 关闭按钮被点击");
        gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    [ContextMenu("打印组件引用")]
    private void PrintComponentRefs()
    {
        Debug.Log($"[ExampleUIPanel] 当前绑定的组件数量: {ComponentRefs.Count}");
        foreach (var kvp in ComponentRefs)
        {
            Debug.Log($"  {kvp.Key} -> {kvp.Value?.GetType().Name ?? "null"}");
        }
    }
#endif
}