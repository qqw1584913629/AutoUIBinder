using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UITool使用示例 - 展示基本的UI面板实现
/// </summary>
public partial class ExampleUIPanel : ShowComponentIconsBase
{
    [Header("示例配置")]
    [SerializeField] private string panelTitle = "示例面板";

    /// <summary>
    /// 关闭按钮点击事件示例
    /// </summary>
    private void OnCloseButtonClick()
    {
        Debug.Log("[ExampleUIPanel] 关闭按钮被点击");
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 设置面板标题
    /// </summary>
    public void SetTitle(string title)
    {
        panelTitle = title;
        // Text_Title?.SetText(title); // 使用生成的组件引用
        Debug.Log($"[ExampleUIPanel] 设置标题为: {title}");
    }

    /// <summary>
    /// 显示面板
    /// </summary>
    public void ShowPanel()
    {
        gameObject.SetActive(true);
        Debug.Log("[ExampleUIPanel] 显示面板");
    }

    /// <summary>
    /// 隐藏面板
    /// </summary>
    public void HidePanel()
    {
        gameObject.SetActive(false);
        Debug.Log("[ExampleUIPanel] 隐藏面板");
    }

    /// <summary>
    /// 获取面板状态
    /// </summary>
    public bool IsVisible => gameObject.activeInHierarchy;

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