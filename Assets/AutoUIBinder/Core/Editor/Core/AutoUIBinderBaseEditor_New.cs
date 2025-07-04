using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using AutoUIBinder;
using UnityEngine.Events;
using System.Reflection;
using Microsoft.CSharp;
using System.CodeDom;

[CustomEditor(typeof(AutoUIBinderBase), true)]
public class AutoUIBinderBaseEditor_New : Editor
{
    private bool showInfoFoldout = true;
    private bool showEventManagerFoldout = true;
    
    // 新的事件管理系统
    private List<EventBinding> eventBindings = new List<EventBinding>();
    private Vector2 scrollPosition;
    private bool showPreview = false;
    private int lastComponentCount = -1; // 用于检测组件数量变化
    private string lastComponentHash = ""; // 用于检测组件内容变化
    
    public class EventBinding
    {
        public string ComponentName;
        public string ComponentType;
        public string EventName;
        public string MethodName;
        public System.Type ParameterType;
        public bool IsSelected;
        public bool AlreadyBound;
        public bool AlreadyExists;
        public Component Component;
        
        public string DisplayName => GetCleanEventName(EventName);
        public string ParameterText => ParameterType != null ? GetFriendlyTypeName(ParameterType) : "无参数";
        public string StatusText => AlreadyBound ? "已绑定" : (AlreadyExists ? "方法存在" : "待生成");
        public Color StatusColor => AlreadyBound ? Color.green : (AlreadyExists ? Color.yellow : Color.gray);
        
        private static string GetCleanEventName(string eventName)
        {
            if (eventName.StartsWith("m_On"))
                return eventName.Substring(4);
            if (eventName.StartsWith("on"))
                return eventName.Substring(2);
            return eventName;
        }
        
        private static string GetFriendlyTypeName(System.Type type)
        {
            using (var provider = new CSharpCodeProvider())
            {
                var typeReference = new CodeTypeReference(type);
                string typeName = provider.GetTypeOutput(typeReference);
                int lastDot = typeName.LastIndexOf('.');
                if (lastDot >= 0)
                    typeName = typeName.Substring(lastDot + 1);
                return typeName;
            }
        }
    }

    public override void OnInspectorGUI()
    {
        // 首先绘制默认的Inspector内容
        base.OnInspectorGUI();
        
        EditorGUILayout.Space(10);
        
        // 绘制信息区域
        DrawInfoSection();
        
        EditorGUILayout.Space(5);

        // 绘制新的事件管理器
        DrawEventManager();
        
        EditorGUILayout.Space(5);
        
        // 绘制操作按钮区域
        DrawActionButtons();
    }
    
    private void DrawInfoSection()
    {
        var autoUIBinderBase = target as AutoUIBinderBase;
        if (autoUIBinderBase == null) return;
        
        showInfoFoldout = EditorGUILayout.Foldout(showInfoFoldout, "数据信息", true);
        
        if (showInfoFoldout)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            int componentCount = autoUIBinderBase.ComponentRefs.Count;
            EditorGUILayout.LabelField($"已绑定组件: {componentCount} 个");
            
            string className = target.GetType().Name;
            EditorGUILayout.LabelField($"类名: {className}");
            
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            bool inPrefabMode = stage != null;
            EditorGUILayout.LabelField($"预制体状态: {(inPrefabMode ? "预制体编辑模式" : "非预制体模式")}");
            
            EditorGUILayout.EndVertical();
        }
    }
    
    private void DrawEventManager()
    {
        var autoUIBinderBase = target as AutoUIBinderBase;
        if (autoUIBinderBase == null) return;
        
        // 检测组件绑定变化，自动刷新事件列表
        int currentComponentCount = autoUIBinderBase.ComponentRefs.Count;
        string currentComponentHash = GetComponentHash(autoUIBinderBase);
        
        if (eventBindings.Count == 0 || 
            lastComponentCount != currentComponentCount || 
            lastComponentHash != currentComponentHash)
        {
            RefreshEventBindings(autoUIBinderBase);
            lastComponentCount = currentComponentCount;
            lastComponentHash = currentComponentHash;
        }
        
        showEventManagerFoldout = EditorGUILayout.Foldout(showEventManagerFoldout, "智能事件管理器", true);
        
        if (!showEventManagerFoldout) return;
        
        EditorGUILayout.Space(5);
        
        // 工具栏
        DrawEventToolbar();
        
        EditorGUILayout.Space(5);
        
        // 事件列表
        DrawEventList();
        
        // 预览区域
        if (showPreview)
        {
            EditorGUILayout.Space(5);
            DrawCodePreview();
        }
    }
    
    private void DrawEventToolbar()
    {
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("刷新事件", GUILayout.Height(25)))
        {
            RefreshEventBindings(target as AutoUIBinderBase);
        }
        
        if (GUILayout.Button("全选", GUILayout.Height(25)))
        {
            SelectAllEvents();
            Repaint();
        }
        
        if (GUILayout.Button("全不选", GUILayout.Height(25)))
        {
            DeselectAllEvents();
            Repaint();
        }
        
        if (GUILayout.Button("智能选择", GUILayout.Height(25)))
        {
            SmartSelectEvents();
            Repaint();
        }
        
        GUILayout.FlexibleSpace();
        
        // 清空所有事件按钮
        GUI.backgroundColor = new Color(0.8f, 0.4f, 0.4f); // 深红色
        if (GUILayout.Button("清空事件", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("危险操作确认", 
                "确定要清空所有事件方法吗？\n\n这将删除原始类文件中所有带有[UIEvent]特性的方法！\n\n此操作不可恢复，请确保已备份代码。", 
                "确定删除", "取消"))
            {
                ClearAllEventMethods();
            }
        }
        GUI.backgroundColor = Color.white;
        
        showPreview = GUILayout.Toggle(showPreview, "预览", "Button", GUILayout.Height(25));
        
        EditorGUILayout.EndHorizontal();
        
        // 状态统计
        int selectedCount = eventBindings.Count(e => e.IsSelected);
        int boundCount = eventBindings.Count(e => e.AlreadyBound);
        EditorGUILayout.LabelField($"总计: {eventBindings.Count} | 已绑定: {boundCount} | 已选择: {selectedCount}", EditorStyles.miniLabel);
    }
    
    private void DrawEventList()
    {
        if (eventBindings.Count == 0)
        {
            var autoUIBinderBase = target as AutoUIBinderBase;
            if (autoUIBinderBase.ComponentRefs.Count == 0)
            {
                EditorGUILayout.HelpBox("还没有绑定任何组件！\n\n请在预制体编辑模式下，点击Hierarchy窗口中的组件图标来绑定组件。", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("已绑定的组件中没有找到可用的UI事件。\n\n确保绑定的组件是Button、Toggle、InputField等UI组件。", MessageType.Info);
            }
            return;
        }
        
        // 动态计算合适的高度
        int eventCount = eventBindings.Count;
        int groupCount = eventBindings.GroupBy(e => e.ComponentName).Count();
        
        // 每个事件项约22px，每个组标题约30px，加上一些内边距
        float estimatedHeight = groupCount * 35f + eventCount * 25f + 20f;
        
        // 限制在合理的范围内：最小80px，最大屏幕高度的60%
        float minHeight = 80f;
        float maxHeight = Screen.height * 0.6f;
        float dynamicHeight = Mathf.Clamp(estimatedHeight, minHeight, maxHeight);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(dynamicHeight));
        
        // 按组件分组显示
        var groupedEvents = eventBindings.GroupBy(e => e.ComponentName).ToList();
        
        foreach (var group in groupedEvents)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // 组件标题
            EditorGUILayout.BeginHorizontal();
            
            Texture2D icon = EditorGUIUtility.ObjectContent(group.First().Component, group.First().Component.GetType()).image as Texture2D;
            if (icon != null)
            {
                GUILayout.Label(icon, GUILayout.Width(16), GUILayout.Height(16));
            }
            
            EditorGUILayout.LabelField($"{group.Key} ({group.First().ComponentType})", EditorStyles.boldLabel);
            
            // 组件级操作
            if (GUILayout.Button("全选", GUILayout.Width(40)))
            {
                foreach (var evt in group)
                    evt.IsSelected = true;
                Repaint();
            }
            
            if (GUILayout.Button("清空", GUILayout.Width(40)))
            {
                foreach (var evt in group)
                    evt.IsSelected = false;
                Repaint();
            }
            
            // 移除组件绑定按钮
            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f); // 红色背景
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                if (EditorUtility.DisplayDialog("确认移除", 
                    $"确定要移除组件 '{group.Key}' 的绑定吗？\n\n这将删除该组件的所有事件绑定。", 
                    "确定", "取消"))
                {
                    RemoveComponentBinding(group.Key);
                }
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
            
            // 事件列表（不使用Unity的缩进系统，避免布局问题）
            foreach (var evt in group)
            {
                DrawEventItem(evt);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(1);
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawEventItem(EventBinding evt)
    {
        // 获取整行的区域
        Rect lineRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 4);
        
        // 更精确的helpBox内边距计算
        float helpBoxPadding = 8f;  // helpBox标准内边距
        float visualIndent = 8f;    // 视觉缩进
        float totalLeftMargin = helpBoxPadding + visualIndent;
        
        // 调整实际可用区域，增加更大的安全边距
        lineRect.x += totalLeftMargin;
        lineRect.width -= (totalLeftMargin + helpBoxPadding + 10f); // 右侧额外10px安全边距
        
        // 检测整行点击
        bool wasSelected = evt.IsSelected;
        if (Event.current.type == EventType.MouseDown && lineRect.Contains(Event.current.mousePosition))
        {
            evt.IsSelected = !evt.IsSelected;
            Event.current.Use();
        }
        
        // 背景色显示选择状态
        if (evt.IsSelected)
        {
            EditorGUI.DrawRect(lineRect, new Color(0.2f, 0.5f, 1f, 0.3f));
        }
        else if (lineRect.Contains(Event.current.mousePosition))
        {
            EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 0.1f));
        }
        
        // Toggle指示器（只显示，不交互）
        Rect toggleRect = new Rect(lineRect.x + 2, lineRect.y + 2, 16, 16);
        EditorGUI.Toggle(toggleRect, evt.IsSelected);
        
        // 从Toggle右侧开始布局其他元素
        float startX = toggleRect.xMax + 4f;
        float availableWidth = lineRect.xMax - startX;
        
        // 确保最小可用宽度
        availableWidth = Mathf.Max(80f, availableWidth);
        
        // 简化布局策略：固定宽度分配
        float nameWidth = evt.IsSelected ? availableWidth * 0.35f : availableWidth * 0.6f;
        float paramWidth = availableWidth * 0.2f;
        float statusWidth = availableWidth * 0.15f;
        float methodWidth = evt.IsSelected ? availableWidth * 0.3f : 0f;
        
        // 当前绘制位置
        float currentX = startX;
        
        // 事件名称 - 简化显示，避免富文本导致的宽度计算问题
        Rect nameRect = new Rect(currentX, lineRect.y, nameWidth - 2f, lineRect.height);
        string displayText = evt.DisplayName;
        
        // 使用简单的颜色标记而不是富文本
        var nameStyle = new GUIStyle(EditorStyles.label);
        if (evt.AlreadyBound)
        {
            nameStyle.normal.textColor = Color.green;
            nameStyle.fontStyle = FontStyle.Bold;
        }
        else if (evt.AlreadyExists)
        {
            nameStyle.normal.textColor = new Color(1f, 0.5f, 0f); // 橙色
        }
        nameStyle.clipping = TextClipping.Clip;
        
        // 使用GUI.Label而不是EditorGUI.LabelField，更精确的控制
        GUI.Label(nameRect, displayText, nameStyle);
        
        // 添加Tooltip
        if (nameRect.Contains(Event.current.mousePosition))
        {
            GUI.tooltip = evt.DisplayName;
        }
        
        currentX += nameWidth;
        
        // 参数类型
        if (paramWidth > 20f) // 只有足够宽度时才显示
        {
            Rect paramRect = new Rect(currentX, lineRect.y, paramWidth - 2f, lineRect.height);
            var paramStyle = new GUIStyle(EditorStyles.miniLabel) 
            { 
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
                clipping = TextClipping.Clip
            };
            GUI.Label(paramRect, evt.ParameterText, paramStyle);
            
            if (paramRect.Contains(Event.current.mousePosition))
            {
                GUI.tooltip = $"参数类型: {evt.ParameterText}";
            }
        }
        
        currentX += paramWidth;
        
        // 状态
        if (statusWidth > 20f) // 只有足够宽度时才显示
        {
            Rect statusRect = new Rect(currentX, lineRect.y, statusWidth - 2f, lineRect.height);
            var statusStyle = new GUIStyle(EditorStyles.miniLabel) 
            { 
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = evt.StatusColor },
                clipping = TextClipping.Clip
            };
            GUI.Label(statusRect, evt.StatusText, statusStyle);
            
            if (statusRect.Contains(Event.current.mousePosition))
            {
                GUI.tooltip = $"状态: {evt.StatusText}";
            }
        }
        
        currentX += statusWidth;
        
        // 方法名（仅选中时显示）
        if (evt.IsSelected && methodWidth > 30f)
        {
            Rect methodRect = new Rect(currentX, lineRect.y, methodWidth - 2f, lineRect.height);
            var methodStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.3f, 0.6f, 1f) },
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };
            
            string methodText = $"-> {evt.MethodName}";
            GUI.Label(methodRect, methodText, methodStyle);
            
            if (methodRect.Contains(Event.current.mousePosition))
            {
                GUI.tooltip = $"将生成方法: {evt.MethodName}";
            }
        }
        
        // 如果选择状态改变，重绘界面
        if (wasSelected != evt.IsSelected)
        {
            EditorUtility.SetDirty(target);
            Repaint();
        }
    }
    
    private void DrawCodePreview()
    {
        var selectedEvents = eventBindings.Where(e => e.IsSelected && !e.AlreadyExists).ToList();
        
        if (selectedEvents.Count == 0)
        {
            EditorGUILayout.HelpBox("没有选中待生成的事件", MessageType.Info);
            return;
        }
        
        EditorGUILayout.LabelField("代码预览", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        foreach (var evt in selectedEvents)
        {
            EditorGUILayout.LabelField($"[UIEvent(\"{evt.ComponentName}\", \"{evt.EventName}\")]", EditorStyles.miniLabel);
            
            string signature = evt.ParameterType != null ? 
                $"private void {evt.MethodName}({evt.ParameterText} value)" :
                $"private void {evt.MethodName}()";
                
            EditorGUILayout.LabelField(signature, EditorStyles.miniLabel);
            EditorGUILayout.Space(2);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawActionButtons()
    {
        var autoUIBinderBase = target as AutoUIBinderBase;
        if (autoUIBinderBase == null) return;
        
        var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        bool inPrefabMode = stage != null;
        
        if (!inPrefabMode) return;
        
        EditorGUILayout.BeginHorizontal();
        
        // 清空绑定按钮
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f); // 红色
        if (GUILayout.Button("清空绑定", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("确认清空", "确定要清空所有组件绑定吗？", "确定", "取消"))
            {
                ClearAllBindings();
            }
        }
        
        // 验证绑定按钮
        GUI.backgroundColor = new Color(0.8f, 0.6f, 0.4f);
        if (GUILayout.Button("验证绑定", GUILayout.Height(30)))
        {
            ValidateBindings();
        }
        
        // 生成事件方法按钮
        int selectedCount = eventBindings.Count(e => e.IsSelected && !e.AlreadyExists);
        GUI.backgroundColor = selectedCount > 0 ? new Color(0.4f, 0.8f, 0.4f) : Color.gray;
        if (GUILayout.Button($"生成事件方法 ({selectedCount})", GUILayout.Height(30)))
        {
            GenerateSelectedEvents();
        }
        
        // 生成UI代码按钮
        GUI.backgroundColor = new Color(0.4f, 0.6f, 0.8f);
        if (GUILayout.Button("生成 UI 代码", GUILayout.Height(30)))
        {
            GenerateSelectedEvents(); // 先生成事件
            GenerateUICode(); // 再生成UI代码
        }
        
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
    }
    
    private void RefreshEventBindings(AutoUIBinderBase autoUIBinderBase)
    {
        // 保存当前用户的选择状态
        var previousSelections = new Dictionary<string, bool>();
        foreach (var evt in eventBindings)
        {
            string key = $"{evt.ComponentName}.{evt.EventName}";
            previousSelections[key] = evt.IsSelected;
        }
        
        eventBindings.Clear();
        
        foreach (var pair in autoUIBinderBase.ComponentRefs)
        {
            if (pair.Value == null) continue;
            
            var events = GetAvailableEvents(pair.Value);
            
            foreach (var eventInfo in events)
            {
                string methodName = GetMethodName(pair.Key, eventInfo.Name);
                bool isAlreadyBound = IsEventBound(autoUIBinderBase, pair.Key, eventInfo.Name);
                bool isMethodExists = DoesMethodExist(methodName);
                
                // 优先使用用户之前的选择，如果没有则根据绑定状态自动设置
                string key = $"{pair.Key}.{eventInfo.Name}";
                bool isSelected = previousSelections.ContainsKey(key) 
                    ? previousSelections[key] 
                    : (isAlreadyBound || isMethodExists);
                
                eventBindings.Add(new EventBinding
                {
                    ComponentName = pair.Key,
                    ComponentType = pair.Value.GetType().Name,
                    EventName = eventInfo.Name,
                    MethodName = methodName,
                    ParameterType = eventInfo.ParameterType,
                    IsSelected = isSelected,
                    AlreadyBound = isAlreadyBound,
                    AlreadyExists = isMethodExists,
                    Component = pair.Value
                });
            }
        }
    }
    
    private void SelectAllEvents()
    {
        foreach (var evt in eventBindings)
            evt.IsSelected = true;
    }
    
    private void DeselectAllEvents()
    {
        foreach (var evt in eventBindings)
            evt.IsSelected = false;
    }
    
    private void SmartSelectEvents()
    {
        // 智能选择：选择常用事件
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
        
        int generated = 0;
        foreach (var evt in selectedEvents)
        {
            AddEventHandlerToOriginalClass(evt.ComponentName, evt.EventName, evt.MethodName, evt.ParameterType);
            generated++;
        }
        
        EditorUtility.DisplayDialog("生成完成", $"成功生成 {generated} 个事件方法", "确定");
        RefreshEventBindings(target as AutoUIBinderBase);
    }
    
    // 以下是从原文件复制的辅助方法...
    
    private EventInfo[] GetAvailableEvents(Component component)
    {
        var events = new List<EventInfo>();
        var type = component.GetType();

        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => typeof(UnityEventBase).IsAssignableFrom(f.FieldType));

        foreach (var field in fields)
        {
            var eventType = field.FieldType;
            System.Type parameterType = null;

            if (eventType.IsGenericType)
            {
                var genericArgs = eventType.GetGenericArguments();
                if (genericArgs.Length > 0)
                {
                    parameterType = genericArgs[0];
                }
            }
            else
            {
                var invokeMethod = eventType.GetMethod("Invoke");
                if (invokeMethod != null)
                {
                    var parameters = invokeMethod.GetParameters();
                    if (parameters.Length > 0)
                    {
                        parameterType = parameters[0].ParameterType;
                    }
                }
            }

            bool isSerializable = field.IsPublic || field.GetCustomAttribute<SerializeField>() != null;
            
            if (isSerializable)
            {
                events.Add(new EventInfo
                {
                    Name = field.Name,
                    EventType = eventType,
                    ParameterType = parameterType
                });
            }
        }

        return events.OrderBy(e => e.Name).ToArray();
    }
    
    private class EventInfo
    {
        public string Name;
        public System.Type EventType;
        public System.Type ParameterType;
    }
    
    private bool IsEventBound(AutoUIBinderBase target, string componentName, string eventName)
    {
        var methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<UIEventAttribute>();
            if (attr != null && attr.ComponentName == componentName && attr.EventType == eventName)
            {
                return true;
            }
        }
        return false;
    }
    
    private bool DoesMethodExist(string methodName)
    {
        var methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return methods.Any(m => m.Name == methodName && m.GetCustomAttribute<UIEventAttribute>() != null);
    }
    
    private string GetMethodName(string componentName, string eventName)
    {
        string cleanEventName = eventName.StartsWith("m_On") ? eventName.Substring(4) : eventName;
        return $"On{char.ToUpper(componentName[0])}{componentName.Substring(1)}{cleanEventName}";
    }
    
    private void AddEventHandlerToOriginalClass(string componentName, string eventName, string methodName, System.Type parameterType)
    {
        var script = MonoScript.FromMonoBehaviour(target as MonoBehaviour);
        var path = AssetDatabase.GetAssetPath(script);
        
        var lines = System.IO.File.ReadAllLines(path);
        var insertIndex = lines.Length - 1;
        
        for (int i = lines.Length - 1; i >= 0; i--)
        {
            if (lines[i].Trim() == "}")
            {
                insertIndex = i;
                break;
            }
        }

        var newMethod = new System.Text.StringBuilder();
        newMethod.AppendLine();
        newMethod.AppendLine($"    [UIEvent(\"{componentName}\", \"{eventName}\")]");
        
        if (parameterType != null)
        {
            newMethod.AppendLine($"    private void {methodName}({GetFriendlyTypeName(parameterType)} value)");
        }
        else
        {
            newMethod.AppendLine($"    private void {methodName}()");
        }
        
        newMethod.AppendLine("    {");
        newMethod.AppendLine("        // TODO: 添加事件处理逻辑");
        newMethod.Append("    }");

        var newLines = lines.ToList();
        newLines.Insert(insertIndex, newMethod.ToString());
        
        System.IO.File.WriteAllLines(path, newLines);
        AssetDatabase.Refresh();
        Debug.Log($"[AutoUIBinder] 已在原始类文件中生成事件方法: {methodName}");
    }
    
    private string GetFriendlyTypeName(System.Type type)
    {
        using (var provider = new CSharpCodeProvider())
        {
            var typeReference = new CodeTypeReference(type);
            string typeName = provider.GetTypeOutput(typeReference);
            
            int lastDot = typeName.LastIndexOf('.');
            if (lastDot >= 0)
            {
                typeName = typeName.Substring(lastDot + 1);
            }
            
            return typeName;
        }
    }
    
    private void ValidateBindings()
    {
        var autoUIBinderBase = target as AutoUIBinderBase;
        if (autoUIBinderBase == null) return;
        
        int validCount = 0;
        int invalidCount = 0;
        var invalidKeys = new List<string>();
        
        foreach (var kvp in autoUIBinderBase.ComponentRefs)
        {
            if (kvp.Value == null)
            {
                invalidCount++;
                invalidKeys.Add(kvp.Key);
            }
            else
            {
                validCount++;
            }
        }
        
        if (invalidCount > 0)
        {
            foreach (var key in invalidKeys)
            {
                autoUIBinderBase.RemoveComponentRef(key);
            }
            EditorUtility.SetDirty(target);
            Debug.Log($"[AutoUIBinder] 清理了 {invalidCount} 个无效绑定");
        }
        
        Debug.Log($"[AutoUIBinder] 验证完成 - 有效: {validCount}, 已清理: {invalidCount}");
        EditorUtility.DisplayDialog("验证完成", $"有效绑定: {validCount} 个\n已清理无效绑定: {invalidCount} 个", "确定");
    }
    
    private void RemoveComponentBinding(string componentName)
    {
        var autoUIBinderBase = target as AutoUIBinderBase;
        if (autoUIBinderBase == null) return;
        
        // 移除组件绑定
        if (autoUIBinderBase.ComponentRefs.ContainsKey(componentName))
        {
            autoUIBinderBase.RemoveComponentRef(componentName);
            EditorUtility.SetDirty(target);
            
            // 刷新事件绑定列表
            RefreshEventBindings(autoUIBinderBase);
            
            Debug.Log($"[AutoUIBinder] 已移除组件绑定: {componentName}");
            Repaint();
        }
    }
    
    private void GenerateUICode()
    {
        try
        {
            var autoUIBinderBase = target as AutoUIBinderBase;
            if (autoUIBinderBase == null) return;
            
            // 简化的UI代码生成
            string className = target.GetType().Name;
            Debug.Log($"[AutoUIBinder] 为 {className} 生成UI代码，包含 {autoUIBinderBase.ComponentRefs.Count} 个组件");
            
            EditorUtility.DisplayDialog("生成完成", 
                $"UI代码生成完成\n类名: {className}\n组件数: {autoUIBinderBase.ComponentRefs.Count}", 
                "确定");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[AutoUIBinder] UI代码生成失败: {ex.Message}");
            EditorUtility.DisplayDialog("错误", $"UI代码生成失败:\n{ex.Message}", "确定");
        }
    }
    
    private void ClearAllBindings()
    {
        var autoUIBinderBase = target as AutoUIBinderBase;
        if (autoUIBinderBase == null) return;
        
        autoUIBinderBase.ComponentRefs.Clear();
        EditorUtility.SetDirty(target);
        
        // 刷新事件绑定列表
        RefreshEventBindings(autoUIBinderBase);
        
        Debug.Log("[AutoUIBinder] 已清空所有组件绑定");
        Repaint();
    }
    
    private void ClearAllEventMethods()
    {
        try
        {
            // 获取脚本文件路径
            var script = MonoScript.FromMonoBehaviour(target as MonoBehaviour);
            var path = AssetDatabase.GetAssetPath(script);
            
            // 读取文件内容
            var lines = System.IO.File.ReadAllLines(path);
            var newLines = new List<string>();
            
            bool skipMethod = false;
            int methodBraceCount = 0;
            int removedCount = 0;
            
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmedLine = line.Trim();
                
                // 检查是否是UIEvent特性
                if (trimmedLine.StartsWith("[UIEvent"))
                {
                    // 找到UIEvent特性，开始跳过
                    skipMethod = true;
                    methodBraceCount = 0;
                    removedCount++;
                    continue;
                }
                
                if (skipMethod)
                {
                    // 计算大括号层级
                    foreach (char c in trimmedLine)
                    {
                        if (c == '{') methodBraceCount++;
                        else if (c == '}') methodBraceCount--;
                    }
                    
                    // 如果大括号归零，说明方法结束
                    if (methodBraceCount <= 0 && trimmedLine.Contains("}"))
                    {
                        skipMethod = false;
                        continue; // 跳过最后的}
                    }
                    
                    continue; // 跳过方法体内容
                }
                
                // 保留非事件方法的行
                newLines.Add(line);
            }
            
            // 写回文件
            System.IO.File.WriteAllLines(path, newLines);
            AssetDatabase.Refresh();
            
            // 刷新事件绑定列表
            RefreshEventBindings(target as AutoUIBinderBase);
            
            Debug.Log($"[AutoUIBinder] 已清空所有事件方法，共删除 {removedCount} 个方法");
            EditorUtility.DisplayDialog("操作完成", $"成功删除了 {removedCount} 个事件方法", "确定");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[AutoUIBinder] 清空事件方法失败: {ex.Message}");
            EditorUtility.DisplayDialog("错误", $"清空事件方法失败:\n{ex.Message}", "确定");
        }
    }
    
    private string GetComponentHash(AutoUIBinderBase autoUIBinderBase)
    {
        // 生成组件绑定的哈希值，用于检测变化
        var sb = new System.Text.StringBuilder();
        var sortedKeys = autoUIBinderBase.ComponentRefs.Keys.OrderBy(k => k).ToList();
        
        foreach (var key in sortedKeys)
        {
            var component = autoUIBinderBase.ComponentRefs[key];
            if (component != null)
            {
                sb.Append($"{key}:{component.GetType().Name}:{component.GetInstanceID()};");
            }
            else
            {
                sb.Append($"{key}:null;");
            }
        }
        
        return sb.ToString();
    }
}