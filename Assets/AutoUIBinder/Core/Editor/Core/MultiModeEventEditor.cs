using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using AutoUIBinder;
using static AutoUIBinderBaseEditor_New;

// [CustomEditor(typeof(AutoUIBinderBase), true)] // 备用编辑器
public class MultiModeEventEditor : Editor
{
    private EventBindingModes.ViewMode currentViewMode = EventBindingModes.ViewMode.CardView;
    private List<EventBinding> eventBindings = new List<EventBinding>();
    private Vector2 scrollPosition;
    private int wizardStep = 0;
    private bool showInfo = true;
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        EditorGUILayout.Space(10);
        
        DrawInfoSection();
        DrawModeSelector();
        DrawEventInterface();
        DrawActionButtons();
    }
    
    private void DrawInfoSection()
    {
        showInfo = EditorGUILayout.Foldout(showInfo, "📊 组件信息", true);
        if (showInfo)
        {
            var autoUIBinderBase = target as AutoUIBinderBase;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"已绑定组件: {autoUIBinderBase.ComponentRefs.Count} 个");
            EditorGUILayout.LabelField($"可用事件: {eventBindings.Count} 个");
            EditorGUILayout.LabelField($"已选择: {eventBindings.Count(e => e.IsSelected)} 个");
            EditorGUILayout.EndVertical();
        }
    }
    
    private void DrawModeSelector()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("🎨 界面模式", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        var modes = new[]
        {
            (EventBindingModes.ViewMode.CardView, "🎴 卡片"),
            (EventBindingModes.ViewMode.TableView, "📊 表格"),
            (EventBindingModes.ViewMode.TimelineView, "⏱️ 时间线"),
            (EventBindingModes.ViewMode.FlowView, "🔄 流程图"),
            (EventBindingModes.ViewMode.WizardView, "🧙‍♂️ 向导"),
            (EventBindingModes.ViewMode.ExpandableTree, "🌳 传统")
        };
        
        foreach (var (mode, label) in modes)
        {
            GUI.backgroundColor = currentViewMode == mode ? Color.green : Color.white;
            if (GUILayout.Button(label, EditorStyles.miniButton))
            {
                currentViewMode = mode;
                if (mode == EventBindingModes.ViewMode.WizardView)
                    wizardStep = 0; // 重置向导
            }
        }
        
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawEventInterface()
    {
        var autoUIBinderBase = target as AutoUIBinderBase;
        if (autoUIBinderBase == null) return;
        
        RefreshEventBindings(autoUIBinderBase);
        
        if (eventBindings.Count == 0)
        {
            EditorGUILayout.HelpBox("没有找到可绑定的事件", MessageType.Info);
            return;
        }
        
        EditorGUILayout.Space(10);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(400));
        
        switch (currentViewMode)
        {
            case EventBindingModes.ViewMode.CardView:
                EventBindingModes.DrawCardView(eventBindings);
                break;
                
            case EventBindingModes.ViewMode.TableView:
                EventBindingModes.DrawTableView(eventBindings);
                break;
                
            case EventBindingModes.ViewMode.TimelineView:
                EventBindingModes.DrawTimelineView(eventBindings);
                break;
                
            case EventBindingModes.ViewMode.FlowView:
                EventBindingModes.DrawFlowView(eventBindings);
                break;
                
            case EventBindingModes.ViewMode.WizardView:
                EventBindingModes.DrawWizardView(eventBindings, ref wizardStep);
                break;
                
            case EventBindingModes.ViewMode.ExpandableTree:
                DrawTraditionalTreeView();
                break;
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawTraditionalTreeView()
    {
        EditorGUILayout.LabelField("🌳 传统树形模式", EditorStyles.boldLabel);
        
        var groups = eventBindings.GroupBy(e => e.ComponentName).ToList();
        
        foreach (var group in groups)
        {
            bool foldout = EditorGUILayout.Foldout(true, $"{group.Key} ({group.First().ComponentType})");
            if (foldout)
            {
                EditorGUI.indentLevel++;
                foreach (var evt in group)
                {
                    EditorGUILayout.BeginHorizontal();
                    evt.IsSelected = EditorGUILayout.Toggle(evt.IsSelected, GUILayout.Width(20));
                    EditorGUILayout.LabelField(evt.DisplayName);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField(evt.ParameterText, EditorStyles.miniLabel, GUILayout.Width(80));
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
        }
    }
    
    private void DrawActionButtons()
    {
        EditorGUILayout.Space(10);
        
        EditorGUILayout.BeginHorizontal();
        
        // 快速操作
        if (GUILayout.Button("🔄 刷新", GUILayout.Height(25)))
        {
            RefreshEventBindings(target as AutoUIBinderBase);
        }
        
        if (GUILayout.Button("✅ 全选", GUILayout.Height(25)))
        {
            foreach (var evt in eventBindings)
                evt.IsSelected = true;
        }
        
        if (GUILayout.Button("❌ 清空", GUILayout.Height(25)))
        {
            foreach (var evt in eventBindings)
                evt.IsSelected = false;
        }
        
        if (GUILayout.Button("🧠 智能选择", GUILayout.Height(25)))
        {
            SmartSelect();
        }
        
        GUILayout.FlexibleSpace();
        
        // 生成按钮
        int selectedCount = eventBindings.Count(e => e.IsSelected && !e.AlreadyExists);
        GUI.backgroundColor = selectedCount > 0 ? Color.green : Color.gray;
        if (GUILayout.Button($"🚀 生成 ({selectedCount})", GUILayout.Height(25), GUILayout.Width(100)))
        {
            GenerateSelectedEvents();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();
        
        // 状态栏
        EditorGUILayout.Space(5);
        string statusText = $"模式: {GetModeDisplayName(currentViewMode)} | " +
                           $"总计: {eventBindings.Count} | " +
                           $"已选择: {eventBindings.Count(e => e.IsSelected)} | " +
                           $"待生成: {eventBindings.Count(e => e.IsSelected && !e.AlreadyExists)}";
        EditorGUILayout.LabelField(statusText, EditorStyles.centeredGreyMiniLabel);
    }
    
    private string GetModeDisplayName(EventBindingModes.ViewMode mode)
    {
        return mode switch
        {
            EventBindingModes.ViewMode.CardView => "卡片模式",
            EventBindingModes.ViewMode.TableView => "表格模式",
            EventBindingModes.ViewMode.TimelineView => "时间线模式",
            EventBindingModes.ViewMode.FlowView => "流程图模式",
            EventBindingModes.ViewMode.WizardView => "向导模式",
            EventBindingModes.ViewMode.ExpandableTree => "传统模式",
            _ => "未知模式"
        };
    }
    
    private void RefreshEventBindings(AutoUIBinderBase autoUIBinderBase)
    {
        eventBindings.Clear();
        
        foreach (var pair in autoUIBinderBase.ComponentRefs)
        {
            if (pair.Value == null) continue;
            
            // 这里需要实现获取组件事件的逻辑
            // 为了演示，我们创建一些模拟数据
            var mockEvents = GetMockEvents(pair.Value);
            
            foreach (var eventInfo in mockEvents)
            {
                string methodName = GetMethodName(pair.Key, eventInfo.name);
                
                eventBindings.Add(new EventBinding
                {
                    ComponentName = pair.Key,
                    ComponentType = pair.Value.GetType().Name,
                    EventName = eventInfo.name,
                    MethodName = methodName,
                    ParameterType = eventInfo.parameterType,
                    IsSelected = false,
                    AlreadyBound = false, // 这里需要实际检查
                    AlreadyExists = false, // 这里需要实际检查
                    Component = pair.Value
                });
            }
        }
    }
    
    private List<(string name, System.Type parameterType)> GetMockEvents(Component component)
    {
        // 模拟数据，实际应该通过反射获取UnityEvent字段
        var events = new List<(string, System.Type)>();
        
        if (component is UnityEngine.UI.Button)
        {
            events.Add(("onClick", null));
        }
        else if (component is UnityEngine.UI.Toggle)
        {
            events.Add(("onValueChanged", typeof(bool)));
        }
        else if (component is UnityEngine.UI.InputField)
        {
            events.Add(("onValueChanged", typeof(string)));
            events.Add(("onEndEdit", typeof(string)));
            events.Add(("onSubmit", typeof(string)));
        }
        else if (component is UnityEngine.UI.Slider)
        {
            events.Add(("onValueChanged", typeof(float)));
        }
        
        return events;
    }
    
    private string GetMethodName(string componentName, string eventName)
    {
        string cleanEventName = eventName.StartsWith("m_On") ? eventName.Substring(4) : 
                               eventName.StartsWith("on") ? eventName.Substring(2) : eventName;
        return $"On{char.ToUpper(componentName[0])}{componentName.Substring(1)}{cleanEventName}";
    }
    
    private void SmartSelect()
    {
        var commonEvents = new[] { "onClick", "onValueChanged", "onEndEdit", "onSubmit" };
        
        foreach (var evt in eventBindings)
        {
            evt.IsSelected = !evt.AlreadyBound && !evt.AlreadyExists && 
                           commonEvents.Any(common => evt.EventName.ToLower().Contains(common.ToLower()));
        }
    }
    
    private void GenerateSelectedEvents()
    {
        var selectedEvents = eventBindings.Where(e => e.IsSelected && !e.AlreadyExists).ToList();
        
        if (selectedEvents.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "没有选中待生成的事件", "确定");
            return;
        }
        
        // 这里实现实际的代码生成逻辑
        string message = $"将生成 {selectedEvents.Count} 个事件方法:\\n";
        foreach (var evt in selectedEvents.Take(5))
        {
            message += $"• {evt.MethodName}\\n";
        }
        if (selectedEvents.Count > 5)
        {
            message += $"... 还有 {selectedEvents.Count - 5} 个";
        }
        
        if (EditorUtility.DisplayDialog("确认生成", message, "生成", "取消"))
        {
            Debug.Log($"[AutoUIBinder] 生成了 {selectedEvents.Count} 个事件方法");
            RefreshEventBindings(target as AutoUIBinderBase);
        }
    }
}