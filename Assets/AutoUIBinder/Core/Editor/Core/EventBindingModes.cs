using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using static AutoUIBinderBaseEditor_New;

/// <summary>
/// 多种不同的事件绑定交互模式
/// </summary>
public class EventBindingModes
{
    public enum ViewMode
    {
        ExpandableTree,    // 传统下拉树形
        CardView,          // 卡片模式
        TableView,         // 表格模式
        TimelineView,      // 时间线模式
        FlowView,          // 流程图模式
        WizardView         // 向导模式
    }
    
    public static ViewMode currentMode = ViewMode.CardView;
    
    // 卡片模式 - 每个组件都是一张独立的卡片
    public static void DrawCardView(List<EventBinding> eventBindings)
    {
        EditorGUILayout.LabelField("🎴 卡片模式", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        var groups = eventBindings.GroupBy(e => e.ComponentName).ToList();
        
        // 网格布局
        int cardsPerRow = Mathf.FloorToInt(EditorGUIUtility.currentViewWidth / 300f);
        cardsPerRow = Mathf.Max(1, cardsPerRow);
        
        for (int i = 0; i < groups.Count; i += cardsPerRow)
        {
            EditorGUILayout.BeginHorizontal();
            
            for (int j = 0; j < cardsPerRow && i + j < groups.Count; j++)
            {
                var group = groups[i + j];
                DrawEventCard(group);
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }
    }
    
    private static void DrawEventCard(IGrouping<string, EventBinding> group)
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(280), GUILayout.MinHeight(120));
        
        // 卡片头部
        EditorGUILayout.BeginHorizontal();
        var icon = EditorGUIUtility.ObjectContent(group.First().Component, group.First().Component.GetType()).image;
        if (icon != null)
        {
            GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
        }
        EditorGUILayout.LabelField(group.Key, EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.LabelField($"类型: {group.First().ComponentType}", EditorStyles.miniLabel);
        
        // 快速操作按钮
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("全选", EditorStyles.miniButton))
        {
            foreach (var evt in group) evt.IsSelected = true;
        }
        if (GUILayout.Button("清空", EditorStyles.miniButton))
        {
            foreach (var evt in group) evt.IsSelected = false;
        }
        if (GUILayout.Button("智能", EditorStyles.miniButton))
        {
            foreach (var evt in group)
            {
                evt.IsSelected = IsCommonEvent(evt.EventName);
            }
        }
        EditorGUILayout.EndHorizontal();
        
        // 事件标签云
        EditorGUILayout.BeginHorizontal();
        int eventCount = 0;
        foreach (var evt in group)
        {
            if (eventCount > 0 && eventCount % 3 == 0)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
            
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = evt.IsSelected ? Color.green : (evt.AlreadyBound ? Color.yellow : Color.white);
            
            if (GUILayout.Button(evt.DisplayName, EditorStyles.miniButton, GUILayout.MinWidth(60)))
            {
                evt.IsSelected = !evt.IsSelected;
            }
            
            GUI.backgroundColor = oldColor;
            eventCount++;
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    // 表格模式 - 类似Excel的表格视图
    public static void DrawTableView(List<EventBinding> eventBindings)
    {
        EditorGUILayout.LabelField("📊 表格模式", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        // 表头
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("选择", EditorStyles.boldLabel, GUILayout.Width(40));
        EditorGUILayout.LabelField("组件", EditorStyles.boldLabel, GUILayout.Width(120));
        EditorGUILayout.LabelField("事件", EditorStyles.boldLabel, GUILayout.Width(100));
        EditorGUILayout.LabelField("方法名", EditorStyles.boldLabel, GUILayout.Width(150));
        EditorGUILayout.LabelField("参数", EditorStyles.boldLabel, GUILayout.Width(80));
        EditorGUILayout.LabelField("状态", EditorStyles.boldLabel, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();
        
        // 分割线
        var rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, Color.gray);
        
        // 表格内容
        foreach (var evt in eventBindings)
        {
            EditorGUILayout.BeginHorizontal();
            
            evt.IsSelected = EditorGUILayout.Toggle(evt.IsSelected, GUILayout.Width(40));
            
            EditorGUILayout.LabelField(evt.ComponentName, GUILayout.Width(120));
            EditorGUILayout.LabelField(evt.DisplayName, GUILayout.Width(100));
            EditorGUILayout.LabelField(evt.MethodName, GUILayout.Width(150));
            EditorGUILayout.LabelField(evt.ParameterText, GUILayout.Width(80));
            
            // 状态指示器
            Color statusColor = evt.AlreadyBound ? Color.green : (evt.AlreadyExists ? Color.yellow : Color.gray);
            GUI.color = statusColor;
            EditorGUILayout.LabelField("●", GUILayout.Width(20));
            GUI.color = Color.white;
            EditorGUILayout.LabelField(evt.StatusText, GUILayout.Width(40));
            
            EditorGUILayout.EndHorizontal();
        }
    }
    
    // 时间线模式 - 按事件类型分组的横向时间线
    public static void DrawTimelineView(List<EventBinding> eventBindings)
    {
        EditorGUILayout.LabelField("⏱️ 时间线模式", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        var eventTypes = new[] { "onClick", "onValueChanged", "onEndEdit", "onSubmit", "其他" };
        
        foreach (var eventType in eventTypes)
        {
            var matchingEvents = eventBindings.Where(e => 
                eventType == "其他" ? !eventTypes.Take(4).Any(t => e.EventName.ToLower().Contains(t.ToLower()))
                : e.EventName.ToLower().Contains(eventType.ToLower())).ToList();
            
            if (matchingEvents.Count == 0) continue;
            
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField($"📅 {eventType} 事件", EditorStyles.boldLabel);
            
            // 横向滚动的事件列表
            EditorGUILayout.BeginHorizontal();
            foreach (var evt in matchingEvents)
            {
                EditorGUILayout.BeginVertical(GUI.skin.button, GUILayout.Width(100));
                
                evt.IsSelected = EditorGUILayout.Toggle(evt.IsSelected);
                EditorGUILayout.LabelField(evt.ComponentName, EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.LabelField(evt.DisplayName, EditorStyles.miniLabel);
                
                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
    }
    
    // 流程图模式 - 可视化的连接线
    public static void DrawFlowView(List<EventBinding> eventBindings)
    {
        EditorGUILayout.LabelField("🔄 流程图模式", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        var groups = eventBindings.GroupBy(e => e.ComponentName).ToList();
        
        foreach (var group in groups)
        {
            EditorGUILayout.BeginHorizontal();
            
            // 组件节点
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(120));
            EditorGUILayout.LabelField(group.Key, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(group.First().ComponentType, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            // 连接线和事件
            EditorGUILayout.BeginVertical();
            foreach (var evt in group)
            {
                EditorGUILayout.BeginHorizontal();
                
                // 连接线
                EditorGUILayout.LabelField("——→", GUILayout.Width(30));
                
                // 事件节点
                Color oldBg = GUI.backgroundColor;
                GUI.backgroundColor = evt.IsSelected ? Color.green : Color.white;
                
                if (GUILayout.Button($"{evt.DisplayName}\n{evt.ParameterText}", GUILayout.Width(100), GUILayout.Height(35)))
                {
                    evt.IsSelected = !evt.IsSelected;
                }
                
                GUI.backgroundColor = oldBg;
                
                // 方法节点
                EditorGUILayout.LabelField("——→", GUILayout.Width(30));
                EditorGUILayout.LabelField(evt.MethodName, GUI.skin.box, GUILayout.Width(150));
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);
        }
    }
    
    // 向导模式 - 分步骤引导
    public static void DrawWizardView(List<EventBinding> eventBindings, ref int wizardStep)
    {
        EditorGUILayout.LabelField("🧙‍♂️ 向导模式", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        // 步骤指示器
        EditorGUILayout.BeginHorizontal();
        string[] steps = { "选择组件", "选择事件", "确认生成" };
        for (int i = 0; i < steps.Length; i++)
        {
            GUI.backgroundColor = wizardStep == i ? Color.blue : (wizardStep > i ? Color.green : Color.gray);
            EditorGUILayout.LabelField($"{i + 1}. {steps[i]}", GUI.skin.button, GUILayout.Height(25));
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        switch (wizardStep)
        {
            case 0: // 选择组件
                EditorGUILayout.LabelField("请选择要绑定事件的组件:", EditorStyles.boldLabel);
                var groups = eventBindings.GroupBy(e => e.ComponentName).ToList();
                foreach (var group in groups)
                {
                    bool hasSelected = group.Any(e => e.IsSelected);
                    bool newSelected = EditorGUILayout.ToggleLeft($"📦 {group.Key} ({group.First().ComponentType})", hasSelected);
                    
                    if (newSelected != hasSelected)
                    {
                        foreach (var evt in group)
                            evt.IsSelected = newSelected;
                    }
                }
                break;
                
            case 1: // 选择事件
                EditorGUILayout.LabelField("请选择具体的事件:", EditorStyles.boldLabel);
                var selectedComponents = eventBindings.Where(e => e.IsSelected).GroupBy(e => e.ComponentName);
                foreach (var group in selectedComponents)
                {
                    EditorGUILayout.LabelField($"组件: {group.Key}", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    foreach (var evt in group)
                    {
                        evt.IsSelected = EditorGUILayout.ToggleLeft($"⚡ {evt.DisplayName} ({evt.ParameterText})", evt.IsSelected);
                    }
                    EditorGUI.indentLevel--;
                }
                break;
                
            case 2: // 确认生成
                EditorGUILayout.LabelField("确认要生成的事件方法:", EditorStyles.boldLabel);
                var toGenerate = eventBindings.Where(e => e.IsSelected && !e.AlreadyExists).ToList();
                foreach (var evt in toGenerate)
                {
                    EditorGUILayout.LabelField($"✨ {evt.MethodName} -> {evt.ComponentName}.{evt.DisplayName}");
                }
                
                if (toGenerate.Count == 0)
                {
                    EditorGUILayout.HelpBox("没有需要生成的新方法", MessageType.Info);
                }
                break;
        }
        
        EditorGUILayout.Space(10);
        
        // 向导导航按钮
        EditorGUILayout.BeginHorizontal();
        
        GUI.enabled = wizardStep > 0;
        if (GUILayout.Button("← 上一步"))
        {
            wizardStep--;
        }
        GUI.enabled = true;
        
        GUILayout.FlexibleSpace();
        
        if (wizardStep < 2)
        {
            if (GUILayout.Button("下一步 →"))
            {
                wizardStep++;
            }
        }
        else
        {
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("🚀 生成事件方法"))
            {
                // 执行生成逻辑
                wizardStep = 0; // 重置向导
            }
            GUI.backgroundColor = Color.white;
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    private static bool IsCommonEvent(string eventName)
    {
        var commonEvents = new[] { "onClick", "onValueChanged", "onEndEdit", "onSubmit" };
        return commonEvents.Any(common => eventName.ToLower().Contains(common.ToLower()));
    }
}

// 注意：EventBinding类现在定义在AutoUIBinderBaseEditor_New.cs中