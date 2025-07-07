using UnityEngine;
using UnityEditor;
using System.Linq;
using AutoUIBinder;

namespace AutoUIBinder.Editor
{
    /// <summary>
    /// 操作按钮绘制器
    /// </summary>
    public class ActionButtonsDrawer
    {
        private ComponentBindingManager bindingManager;
        private UIEventManager eventManager;
        private CodeGenerator codeGenerator;
        
        public ActionButtonsDrawer(ComponentBindingManager bindingManager, UIEventManager eventManager, CodeGenerator codeGenerator)
        {
            this.bindingManager = bindingManager;
            this.eventManager = eventManager;
            this.codeGenerator = codeGenerator;
        }
        
        /// <summary>
        /// 绘制操作按钮区域
        /// </summary>
        public void DrawActionButtons(AutoUIBinderBase target)
        {
            if (!bindingManager.IsInPrefabMode()) return;
            
            EditorGUILayout.BeginHorizontal();
            
            DrawClearBindingsButton(target);
            DrawValidateBindingsButton(target);
            DrawGenerateEventsButton(target);
            DrawGenerateUICodeButton(target);
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawClearBindingsButton(AutoUIBinderBase target)
        {
            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button("清空绑定", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("确认清空", "确定要清空所有组件绑定吗？", "确定", "取消"))
                {
                    bindingManager.ClearAllBindings(target);
                    eventManager.RefreshEventBindings(target);
                }
            }
        }
        
        private void DrawValidateBindingsButton(AutoUIBinderBase target)
        {
            GUI.backgroundColor = new Color(0.8f, 0.6f, 0.4f);
            if (GUILayout.Button("验证绑定", GUILayout.Height(30)))
            {
                var result = bindingManager.ValidateBindings(target);
                Debug.Log($"[AutoUIBinder] 验证完成 - 有效: {result.ValidCount}, 已清理: {result.InvalidCount}");
                EditorUtility.DisplayDialog("验证完成", result.GetMessage(), "确定");
                
                if (result.InvalidCount > 0)
                {
                    eventManager.RefreshEventBindings(target);
                }
            }
        }
        
        private void DrawGenerateEventsButton(AutoUIBinderBase target)
        {
            int selectedCount = eventManager.EventBindings.Count(e => e.IsSelected && !e.AlreadyExists);
            GUI.backgroundColor = selectedCount > 0 ? new Color(0.4f, 0.8f, 0.4f) : Color.gray;
            
            if (GUILayout.Button($"生成事件方法 ({selectedCount})", GUILayout.Height(30)))
            {
                if (selectedCount == 0)
                {
                    EditorUtility.DisplayDialog("提示", "没有选中待生成的事件", "确定");
                    return;
                }
                
                int generated = codeGenerator.GenerateSelectedEvents(target, eventManager.EventBindings);
                EditorUtility.DisplayDialog("生成完成", $"成功生成 {generated} 个事件方法", "确定");
                eventManager.RefreshEventBindings(target);
            }
        }
        
        private void DrawGenerateUICodeButton(AutoUIBinderBase target)
        {
            GUI.backgroundColor = new Color(0.4f, 0.6f, 0.8f);
            if (GUILayout.Button("生成 UI 代码", GUILayout.Height(30)))
            {
                try
                {
                    // 先生成事件
                    int generated = codeGenerator.GenerateSelectedEvents(target, eventManager.EventBindings);
                    if (generated > 0)
                    {
                        eventManager.RefreshEventBindings(target);
                    }
                    
                    // 再生成UI代码
                    codeGenerator.GenerateUICode(target);
                    
                    string className = target.GetType().Name;
                    EditorUtility.DisplayDialog("生成完成", 
                        $"UI代码生成完成\n类名: {className}\n组件数: {target.ComponentRefs.Count}\n事件方法: {generated}", 
                        "确定");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[AutoUIBinder] UI代码生成失败: {ex.Message}");
                    EditorUtility.DisplayDialog("错误", $"UI代码生成失败:\n{ex.Message}", "确定");
                }
            }
        }
    }
}