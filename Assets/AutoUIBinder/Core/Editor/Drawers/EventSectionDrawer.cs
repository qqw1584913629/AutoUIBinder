using UnityEngine;
using UnityEditor;
using System.Linq;
using AutoUIBinder;

namespace AutoUIBinder.Editor
{
    /// <summary>
    /// 事件区域绘制器
    /// </summary>
    public class EventSectionDrawer
    {
        private bool showEventInfoFoldout = true;
        private bool showPreview = false;
        private Vector2 scrollPosition;
        
        private UIEventManager eventManager;
        private ComponentBindingManager bindingManager;
        private CodeGenerator codeGenerator;
        
        public EventSectionDrawer(UIEventManager eventManager, ComponentBindingManager bindingManager, CodeGenerator codeGenerator)
        {
            this.eventManager = eventManager;
            this.bindingManager = bindingManager;
            this.codeGenerator = codeGenerator;
        }
        
        /// <summary>
        /// 绘制事件信息区域
        /// </summary>
        public void DrawEventInfo(AutoUIBinderBase target)
        {
            if (eventManager.NeedsRefresh(target))
            {
                eventManager.RefreshEventBindings(target);
                eventManager.UpdateCacheState(target);
            }
            
            showEventInfoFoldout = EditorGUILayout.Foldout(showEventInfoFoldout, "事件信息", true);
            
            if (!showEventInfoFoldout) return;
            
            EditorGUILayout.Space(5);
            
            DrawEventToolbar(target);
            EditorGUILayout.Space(5);
            DrawEventList(target);
            
            if (showPreview)
            {
                EditorGUILayout.Space(5);
                DrawCodePreview();
            }
        }
        
        private void DrawEventToolbar(AutoUIBinderBase target)
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("刷新事件", GUILayout.Height(25)))
            {
                eventManager.RefreshEventBindings(target);
            }
            
            if (GUILayout.Button("全选", GUILayout.Height(25)))
            {
                eventManager.SelectAllEvents();
                GUI.changed = true;
            }
            
            if (GUILayout.Button("全不选", GUILayout.Height(25)))
            {
                eventManager.DeselectAllEvents();
                GUI.changed = true;
            }
            
            if (GUILayout.Button("智能选择", GUILayout.Height(25)))
            {
                eventManager.SmartSelectEvents();
                GUI.changed = true;
            }
            
            GUILayout.FlexibleSpace();
            
            GUI.backgroundColor = new Color(1.0f, 0.8f, 0.4f);
            if (GUILayout.Button("清理无效", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("清理确认", 
                    "确定要清理无效的事件方法吗？\n\n这将删除那些组件已解绑但方法还存在的事件方法。\n\n建议在清理前备份代码。", 
                    "确定清理", "取消"))
                {
                    int removed = codeGenerator.CleanupInvalidEventMethods(target);
                    if (removed > 0)
                    {
                        EditorUtility.DisplayDialog("清理完成", $"成功清理了 {removed} 个无效的事件方法", "确定");
                        eventManager.RefreshEventBindings(target);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("清理完成", "没有发现需要清理的无效事件方法", "确定");
                    }
                }
            }
            GUI.backgroundColor = Color.white;
            
            GUI.backgroundColor = new Color(0.8f, 0.4f, 0.4f);
            if (GUILayout.Button("清空事件", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("危险操作确认", 
                    "确定要清空所有事件方法吗？\n\n这将删除原始类文件中所有带有[UIEvent]特性的方法！\n\n此操作不可恢复，请确保已备份代码。", 
                    "确定删除", "取消"))
                {
                    int removed = codeGenerator.ClearAllEventMethods(target);
                    EditorUtility.DisplayDialog("操作完成", $"成功删除了 {removed} 个事件方法", "确定");
                    eventManager.RefreshEventBindings(target);
                }
            }
            GUI.backgroundColor = Color.white;
            
            showPreview = GUILayout.Toggle(showPreview, "预览", "Button", GUILayout.Height(25));
            
            EditorGUILayout.EndHorizontal();
            
            int selectedCount = eventManager.GetSelectedCount();
            int boundCount = eventManager.GetBoundCount();
            EditorGUILayout.LabelField($"总计: {eventManager.EventBindings.Count} | 已绑定: {boundCount} | 已选择: {selectedCount}", EditorStyles.miniLabel);
        }
        
        private void DrawEventList(AutoUIBinderBase target)
        {
            var eventBindings = eventManager.EventBindings;
            
            if (eventBindings.Count == 0)
            {
                if (target.ComponentRefs.Count == 0)
                {
                    EditorGUILayout.HelpBox("还没有绑定任何组件！\n\n请在预制体编辑模式下，点击Hierarchy窗口中的组件图标来绑定组件。", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox("已绑定的组件中没有找到可用的UI事件。\n\n确保绑定的组件是Button、Toggle、InputField等UI组件。", MessageType.Info);
                }
                return;
            }
            
            int eventCount = eventBindings.Count;
            int groupCount = eventBindings.GroupBy(e => e.ComponentName).Count();
            
            float estimatedHeight = groupCount * 35f + eventCount * 25f + 20f;
            float minHeight = 80f;
            float maxHeight = Screen.height * 0.6f;
            float dynamicHeight = Mathf.Clamp(estimatedHeight, minHeight, maxHeight);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(dynamicHeight));
            
            var groupedEvents = eventBindings.GroupBy(e => e.ComponentName).ToList();
            
            foreach (var group in groupedEvents)
            {
                DrawEventGroup(target, group);
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawEventGroup(AutoUIBinderBase target, System.Linq.IGrouping<string, UIEventManager.EventBinding> group)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            
            var firstComponent = group.First().Component;
            if (firstComponent != null)
            {
                var icon = EditorGUIUtility.ObjectContent(firstComponent, firstComponent.GetType()).image as Texture2D;
                if (icon != null)
                {
                    GUILayout.Label(icon, GUILayout.Width(16), GUILayout.Height(16));
                }
            }
            
            EditorGUILayout.LabelField($"{group.Key} ({group.First().ComponentType})", EditorStyles.boldLabel);
            
            if (GUILayout.Button("全选", GUILayout.Width(40)))
            {
                foreach (var evt in group)
                    evt.IsSelected = true;
                GUI.changed = true;
            }
            
            if (GUILayout.Button("清空", GUILayout.Width(40)))
            {
                foreach (var evt in group)
                    evt.IsSelected = false;
                GUI.changed = true;
            }
            
            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                if (EditorUtility.DisplayDialog("确认移除", 
                    $"确定要移除组件 '{group.Key}' 的所有事件方法吗？\n\n这将删除该组件的所有事件方法代码，但保留组件绑定。", 
                    "确定", "取消"))
                {
                    int removed = codeGenerator.ClearComponentEventMethods(target, group.Key);
                    eventManager.RefreshEventBindings(target);
                    EditorUtility.DisplayDialog("操作完成", $"成功删除了 {removed} 个事件方法", "确定");
                }
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
            
            foreach (var evt in group)
            {
                DrawEventItem(evt);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(1);
        }
        
        private void DrawEventItem(UIEventManager.EventBinding evt)
        {
            var lineRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 4);
            
            float helpBoxPadding = 8f;
            float visualIndent = 8f;
            float totalLeftMargin = helpBoxPadding + visualIndent;
            
            lineRect.x += totalLeftMargin;
            lineRect.width -= (totalLeftMargin + helpBoxPadding + 10f);
            
            bool wasSelected = evt.IsSelected;
            if (Event.current.type == EventType.MouseDown && lineRect.Contains(Event.current.mousePosition))
            {
                evt.IsSelected = !evt.IsSelected;
                Event.current.Use();
                GUI.changed = true;
            }
            
            if (evt.IsSelected)
            {
                EditorGUI.DrawRect(lineRect, new Color(0.2f, 0.5f, 1f, 0.3f));
            }
            else if (lineRect.Contains(Event.current.mousePosition))
            {
                EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 0.1f));
            }
            
            var toggleRect = new Rect(lineRect.x + 2, lineRect.y + 2, 16, 16);
            EditorGUI.Toggle(toggleRect, evt.IsSelected);
            
            float startX = toggleRect.xMax + 4f;
            float availableWidth = lineRect.xMax - startX;
            availableWidth = Mathf.Max(80f, availableWidth);
            
            float nameWidth = evt.IsSelected ? availableWidth * 0.35f : availableWidth * 0.6f;
            float paramWidth = availableWidth * 0.2f;
            float statusWidth = availableWidth * 0.15f;
            float methodWidth = evt.IsSelected ? availableWidth * 0.3f : 0f;
            
            float currentX = startX;
            
            var nameRect = new Rect(currentX, lineRect.y, nameWidth - 2f, lineRect.height);
            var nameStyle = new GUIStyle(EditorStyles.label);
            if (evt.AlreadyBound)
            {
                nameStyle.normal.textColor = Color.green;
                nameStyle.fontStyle = FontStyle.Bold;
            }
            else if (evt.AlreadyExists)
            {
                nameStyle.normal.textColor = new Color(1f, 0.5f, 0f);
            }
            nameStyle.clipping = TextClipping.Clip;
            
            GUI.Label(nameRect, evt.DisplayName, nameStyle);
            
            if (nameRect.Contains(Event.current.mousePosition))
            {
                GUI.tooltip = evt.DisplayName;
            }
            
            currentX += nameWidth;
            
            if (paramWidth > 20f)
            {
                var paramRect = new Rect(currentX, lineRect.y, paramWidth - 2f, lineRect.height);
                var paramStyle = new GUIStyle(EditorStyles.miniLabel) 
                { 
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
                    clipping = TextClipping.Clip
                };
                GUI.Label(paramRect, evt.ParameterText, paramStyle);
            }
            
            currentX += paramWidth;
            
            if (statusWidth > 20f)
            {
                var statusRect = new Rect(currentX, lineRect.y, statusWidth - 2f, lineRect.height);
                var statusStyle = new GUIStyle(EditorStyles.miniLabel) 
                { 
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = evt.StatusColor },
                    clipping = TextClipping.Clip
                };
                GUI.Label(statusRect, evt.StatusText, statusStyle);
            }
            
            currentX += statusWidth;
            
            if (evt.IsSelected && methodWidth > 30f)
            {
                var methodRect = new Rect(currentX, lineRect.y, methodWidth - 2f, lineRect.height);
                var methodStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.3f, 0.6f, 1f) },
                    fontStyle = FontStyle.Italic,
                    alignment = TextAnchor.MiddleLeft,
                    clipping = TextClipping.Clip
                };
                
                string methodText = $"-> {evt.MethodName}";
                GUI.Label(methodRect, methodText, methodStyle);
            }
        }
        
        private void DrawCodePreview()
        {
            var selectedEvents = eventManager.EventBindings.Where(e => e.IsSelected && !e.AlreadyExists).ToList();
            
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
    }
}