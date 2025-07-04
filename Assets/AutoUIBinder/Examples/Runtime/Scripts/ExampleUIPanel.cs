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

    [UIEvent("InputField_Legacy_InputField", "m_OnDidEndEdit")]
    private void OnInputField_Legacy_InputFieldDidEndEdit(string value)
    {
        // TODO: 添加事件处理逻辑
    }

    [UIEvent("InputField_Legacy_InputField", "m_OnValueChanged")]
    private void OnInputField_Legacy_InputFieldValueChanged(string value)
    {
        // TODO: 添加事件处理逻辑
    }
}
