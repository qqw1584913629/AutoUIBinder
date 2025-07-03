using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AutoUIBinder使用示例 - 展示基本的UI面板实现
/// </summary>
public partial class ExampleUIPanel : AutoUIBinderBase
{
    public void Start()
    {
        Text_Title_1_1_1_Text.text = "123";
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
