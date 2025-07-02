using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using static UnityEngine.Random;

namespace UITool
{
    [InitializeOnLoad]
    public class HierarchyComponentIcons
    {
        // 用于存储组件的高亮状态
        private static Dictionary<int, bool> highlightedComponents = new Dictionary<int, bool>();
        
        // 用于存储每个AutoUIBinderBase的唯一颜色
        private static Dictionary<int, Color> handlerColors = new Dictionary<int, Color>();
        
        // 用于存储GameObject的名称，用于检测重命名
        private static Dictionary<int, string> gameObjectNames = new Dictionary<int, string>();
        
        // 性能优化：控制重绘频率
        private static bool needsRepaint = false;
        private static double lastRepaintTime = 0;
        private const double REPAINT_INTERVAL = 0.1; // 100ms间隔

        static HierarchyComponentIcons()
        {
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;
            EditorApplication.update += OnEditorUpdate;
                
            // 监听预制体打开事件
            PrefabStage.prefabStageOpened += OnPrefabStageOpened;
            // 监听预制体关闭事件，清理高亮状态
            PrefabStage.prefabStageClosing += OnPrefabStageClosing;
            // 监听编辑器刷新事件
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            // 监听组件添加事件
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            // 监听选择变化事件
            Selection.selectionChanged += OnSelectionChanged;
            // 监听编辑器更新事件来进行周期性清理
            EditorApplication.update += OnEditorUpdateWithCleanup;

            // 初始化
            needsRepaint = true;
        }

        private static void OnAfterAssemblyReload()
        {
            // 清空当前的高亮状态
            highlightedComponents.Clear();
            
            // 清理颜色缓存
            CleanupHandlerColors();

            // 清理名称缓存
            gameObjectNames.Clear();

            // 获取当前打开的预制体
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                // 获取预制体的根对象
                GameObject prefabRoot = stage.prefabContentsRoot;
                if (prefabRoot != null)
                {
                    // 查找所有AutoUIBinderBase组件
                    var iconHandlers = prefabRoot.GetComponentsInChildren<AutoUIBinderBase>(true);
                    foreach (var handler in iconHandlers)
                    {
                        if (handler == null || handler.ComponentRefs == null) continue;

                        // 遍历字典中的所有组件引用
                        foreach (var kvp in handler.ComponentRefs)
                        {
                            if (kvp.Value != null)
                            {
                                // 恢复组件的高亮状态
                                highlightedComponents[kvp.Value.GetInstanceID()] = true;
                            }
                        }
                    }
                }
            }

            // 刷新Hierarchy窗口
            RequestRepaint();
        }

        private static void OnPrefabStageOpened(PrefabStage stage)
        {
            // 清空当前的高亮状态
            highlightedComponents.Clear();
            
            // 清理颜色缓存
            CleanupHandlerColors();
            
            // 清理名称缓存
            gameObjectNames.Clear();

            // 获取预制体的根对象
            GameObject prefabRoot = stage.prefabContentsRoot;
            if (prefabRoot == null) return;

            // 查找所有AutoUIBinderBase组件
            var iconHandlers = prefabRoot.GetComponentsInChildren<AutoUIBinderBase>(true);
            foreach (var handler in iconHandlers)
            {
                if (handler == null || handler.ComponentRefs == null) continue;

                // 遍历字典中的所有组件引用
                foreach (var kvp in handler.ComponentRefs)
                {
                    if (kvp.Value != null)
                    {
                        // 恢复组件的高亮状态
                        highlightedComponents[kvp.Value.GetInstanceID()] = true;
                    }
                }
            }

            // 刷新Hierarchy窗口
            RequestRepaint();
        }

        private static void OnPrefabStageClosing(PrefabStage stage)
        {
            if (stage == null || stage.prefabContentsRoot == null) return;

            // 清理名称缓存
            gameObjectNames.Clear();

            // 获取当前预制体中的所有组件
            var allComponents = stage.prefabContentsRoot.GetComponentsInChildren<Component>(true);
            
            // 只移除当前预制体中组件的高亮状态
            foreach (var comp in allComponents)
            {
                if (comp != null)
                {
                    int compId = comp.GetInstanceID();
                    if (highlightedComponents.ContainsKey(compId))
                    {
                        highlightedComponents.Remove(compId);
                    }
                }
            }
            
            RequestRepaint();
        }

        private static void HandleMouseEvents(Rect iconRect, int componentID, GameObject gameObject)
        {
            Event current = Event.current;

            // 处理悬停事件
            if (current.type == EventType.Repaint && iconRect.Contains(current.mousePosition))
            {
                Component comp = EditorUtility.InstanceIDToObject(componentID) as Component;
                if (comp != null)
                {
                    AutoUIBinderBase iconHandler = FindIconHandler(comp);
                    if (iconHandler != null)
                    {
                        string tooltipText = $"组件: {comp.GetType().Name}\n属于: {iconHandler.gameObject.name}";
                        EditorGUI.LabelField(iconRect, new GUIContent("", tooltipText));
                    }
                }
            }

            // 处理所有鼠标事件，避免传递给Hierarchy窗口
            if ((current.type == EventType.MouseDown || current.type == EventType.MouseUp || current.type == EventType.MouseDrag) 
                && current.button == 0 && iconRect.Contains(current.mousePosition))
            {
                // 对于非MouseDown事件，只消费事件不执行逻辑
                if (current.type != EventType.MouseDown)
                {
                    current.Use();
                    return;
                }
                Component comp = EditorUtility.InstanceIDToObject(componentID) as Component;
                if (comp != null)
                {
                    // 检查节点名称是否包含括号，如果包含则自动重命名
                    if (comp.gameObject.name.Contains("(") || comp.gameObject.name.Contains(")"))
                    {
                        string oldName = comp.gameObject.name;
                        string newName = oldName.Replace(" ", "_").Replace("(", "").Replace(")", "");
                        comp.gameObject.name = newName;
                        HandleObjectRename(comp.gameObject, oldName);
                    }

                    // 检查当前组件是否是AutoUIBinderBase
                    bool isAutoUIBinderBase = comp is AutoUIBinderBase;
                    
                    // 从组件所在的GameObject开始向上查找AutoUIBinderBase
                    AutoUIBinderBase iconHandler = null;
                    
                    if (isAutoUIBinderBase)
                    {
                        // 对于AutoUIBinderBase，只从父级查找handler
                        var parent = comp.gameObject.transform.parent;
                        while (parent != null)
                        {
                            iconHandler = parent.GetComponent<AutoUIBinderBase>();
                            if (iconHandler != null)
                                break;
                            parent = parent.parent;
                        }
                    }
                    else
                    {
                        // 如果不是AutoUIBinderBase，先查找自身，然后查找最近的父级
                        iconHandler = comp.gameObject.GetComponent<AutoUIBinderBase>();
                        if (iconHandler == null)
                        {
                            var parent = comp.gameObject.transform.parent;
                            while (parent != null)
                            {
                                iconHandler = parent.GetComponent<AutoUIBinderBase>();
                                if (iconHandler != null)
                                    break;
                                parent = parent.parent;
                            }
                        }
                    }

                    if (iconHandler != null)
                    {
                        string key = GetNodeComponentKey(comp);
                        
                    if (highlightedComponents.ContainsKey(componentID))
                    {
                            // 直接从当前handler移除引用
                            if (iconHandler != null)
                            {
                                iconHandler.RemoveComponentRef(key);
                                EditorUtility.SetDirty(iconHandler.gameObject);
                            }
                            // 移除高亮状态
                            highlightedComponents.Remove(componentID);
                        }
                        else
                        {
                            // 如果是AutoUIBinderBase组件，只允许绑定到父级，且不能绑定自己
                            if (comp is AutoUIBinderBase)
                            {
                                if (iconHandler.gameObject == comp.gameObject)
                                {
                                    // 不能绑定到自己
                                    current.Use();
                                    return;
                                }

                                // 检查是否是父级关系
                                Transform parent = comp.gameObject.transform.parent;
                                bool isParent = false;
                                while (parent != null)
                                {
                                    if (parent.gameObject == iconHandler.gameObject)
                                    {
                                        isParent = true;
                                        break;
                                    }
                                    parent = parent.parent;
                                }

                                if (!isParent)
                                {
                                    current.Use();
                                    return;
                                }
                            }

                            // 在添加新引用前，检查节点名称冲突
                            bool hasNameConflict = false;
                            foreach (var kvp in iconHandler.ComponentRefs)
                            {
                                if (kvp.Value != null && 
                                    kvp.Value.gameObject != comp.gameObject && // 不是自己
                                    kvp.Value.gameObject.name == comp.gameObject.name) // 名字相同
                                {
                                    hasNameConflict = true;
                                    break;
                                }
                            }

                            // 如果有名称冲突，生成新的唯一名称
                            if (hasNameConflict)
                            {
                                string uniqueName = GetUniqueNodeName(iconHandler, comp.gameObject.name);
                                comp.gameObject.name = uniqueName;
                                key = GetNodeComponentKey(comp);
                            }

                            // 在添加新引用前，清理其他AutoUIBinderBase中的引用
                            CleanupExistingReference(comp, iconHandler);

                            // 添加新引用
                            highlightedComponents[componentID] = true;
                            iconHandler.AddComponentRef(key, comp);
                        }

                        // 标记为已修改
                        EditorUtility.SetDirty(iconHandler.gameObject);
                    }
                }

                EditorWindow.GetWindow<SceneView>().Repaint();
                RequestRepaint();
                current.Use();
            }
        }

        private static string GetNodeComponentKey(Component component)
        {
            if (component == null) return "";
            
            // 直接使用节点名字
            string nodeName = component.gameObject.name;
            
            // 替换空格为下划线
            nodeName = nodeName.Replace(" ", "_");
            
            // 组合key: 节点名_组件类型
            string componentType = component.GetType().Name;
            string key = $"{nodeName}_{componentType}";
            
            return key;
        }

        private static string GetFullPath(GameObject obj)
        {
            if (obj == null) return "";
            
            var path = new System.Text.StringBuilder(obj.name);
            var current = obj.transform.parent;
            
            // 向上遍历直到找到AutoUIBinderBase或到达根节点
            while (current != null)
            {
                // 如果找到AutoUIBinderBase，停止
                if (current.GetComponent<AutoUIBinderBase>() != null)
                    break;
                    
                path.Insert(0, current.name + "/");
                current = current.parent;
            }
            
            return path.ToString().Replace(" ", "_");
        }

        private static void OnEditorUpdate()
        {
            // 性能优化：只在需要时重绘，并限制重绘频率
            if (needsRepaint && EditorApplication.timeSinceStartup > lastRepaintTime + REPAINT_INTERVAL)
            {
                EditorApplication.RepaintHierarchyWindow();
                lastRepaintTime = EditorApplication.timeSinceStartup;
                needsRepaint = false;
            }
        }
        
        // 新的编辑器更新方法，包含清理逻辑
        private static double lastCleanupTime = 0;
        private const double CLEANUP_INTERVAL = 1.0; // 1秒间隔进行清理检查
        
        private static void OnEditorUpdateWithCleanup()
        {
            // 调用原有的更新逻辑
            OnEditorUpdate();
            
            // 定期清理孤儿组件
            if (EditorApplication.timeSinceStartup > lastCleanupTime + CLEANUP_INTERVAL)
            {
                var stage = PrefabStageUtility.GetCurrentPrefabStage();
                if (stage != null && stage.prefabContentsRoot != null)
                {
                    CleanupOrphanComponents(stage.prefabContentsRoot);
                }
                lastCleanupTime = EditorApplication.timeSinceStartup;
            }
        }
        
        private static void OnSelectionChanged()
        {
            // 选择变化时标记需要重绘
            RequestRepaint();
        }
        
        private static void RequestRepaint()
        {
            needsRepaint = true;
        }

        private static bool ShouldShowIcons(GameObject gameObject)
        {
            // 检查是否在预制体编辑模式
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null)
            {
                return false;  // 不在预制体编辑模式中
            }

            // 检查自身或任意父级对象是否继承了AutoUIBinderBase
            Transform current = gameObject.transform;
            while (current != null)
            {
                if (current.GetComponent<AutoUIBinderBase>() != null)
                    return true;
                current = current.parent;
            }
            return false;
        }

        private static void HierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            try
            {
                if (Event.current == null)
                {
                    return;
                }

                if (selectionRect.width <= 0 || selectionRect.height <= 0)
                {
                    return;
                }

                GameObject gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
                if (gameObject == null)
                    return;

                // 检测重命名
                if (gameObjectNames.TryGetValue(instanceID, out string oldName))
                {
                    if (oldName != gameObject.name)
                    {
                        HandleObjectRename(gameObject, oldName);
                        gameObjectNames[instanceID] = gameObject.name;
                    }
                }
                else
                {
                    gameObjectNames[instanceID] = gameObject.name;
                }

                // 检查该GameObject是否应该显示图标
                if (!ShouldShowIcons(gameObject))
                    return;

                // 检查是否有AutoUIBinderBase组件（用于判断是否显示图标）
                var baseComponent = gameObject.GetComponent<AutoUIBinderBase>();
                
                // 如果当前节点有AutoUIBinderBase组件，绘制背景色标识
                if (baseComponent != null)
                {
                    Color handlerColor = GetHandlerColor(baseComponent);
                    Color bgColor = new Color(handlerColor.r, handlerColor.g, handlerColor.b, 0.15f);
                    
                    // 绘制节点背景色，稍微缩小一点，避免覆盖选中高亮
                    Rect bgRect = new Rect(selectionRect.x + 1, selectionRect.y + 1, selectionRect.width - 2, selectionRect.height - 2);
                    EditorGUI.DrawRect(bgRect, bgColor);
                }

                // 获取所有组件
                Component[] components = gameObject.GetComponents<Component>();
                if (components.Length == 0)
                    return;

                // 计算图标显示区域
                float iconSize = 16f;
                float padding = 2f;
                float startX = selectionRect.xMax - (iconSize + padding) * components.Length;
                
                // 确保绘制区域在有效范围内
                if (startX < selectionRect.xMin)
                    startX = selectionRect.xMin;
            

                // 为每个组件绘制图标
                for (int i = 0; i < components.Length; i++)
                {
                    Component component = components[i];
                    if (component == null)
                        continue;

                    Rect iconRect = new Rect(startX + (iconSize + padding) * i, selectionRect.y, iconSize, iconSize);
                    
                    // 确保图标区域有效
                    if (iconRect.width <= 0 || iconRect.height <= 0 || iconRect.x < 0)
                        continue;
                
                    // 获取组件的唯一ID
                    int componentID = component.GetInstanceID();
                    
                    // 检查是否需要处理鼠标事件
                    HandleMouseEvents(iconRect, componentID, gameObject);

                    // 如果组件被高亮，使用其关联的AutoUIBinderBase的颜色绘制高亮背景
                    if (highlightedComponents.ContainsKey(componentID) && highlightedComponents[componentID])
                    {
                        // 查找该组件关联的AutoUIBinderBase
                        AutoUIBinderBase handler = null;
                        Transform current = component.transform;
                        while (current != null)
                        {
                            handler = current.GetComponent<AutoUIBinderBase>();
                            if (handler != null)
                                break;
                            current = current.parent;
                        }

                        if (handler != null)
                        {
                            // 使用简单的高亮颜色
                            Color highlightColor = new Color(0.3f, 0.7f, 1f, 0.5f); // 蓝色高亮
                            EditorGUI.DrawRect(iconRect, highlightColor);
                        }
                    }

                    // 获取组件的图标
                    DrawIcon(iconRect, component);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UITool] Error in HierarchyWindowItemOnGUI: {e.Message}\n{e.StackTrace}");
            }
        }

        private static void DrawIcon(Rect rect, Component component)
        {
            GUIContent content = EditorGUIUtility.ObjectContent(component, component.GetType());
            if (content.image != null)
            {
                // 获取该组件关联的AutoUIBinderBase
                AutoUIBinderBase iconHandler = FindIconHandler(component);

                // 绘制组件图标
                GUI.DrawTexture(rect, content.image);

                // 如果找到了AutoUIBinderBase，绘制层级指示器
                if (iconHandler != null)
                {
                    string key = GetNodeComponentKey(component);
                    bool isReferenced = false;

                    // 检查是否在当前handler中被引用
                    if (iconHandler.ComponentRefs.ContainsKey(key))
                    {
                        var referencedComponent = iconHandler.ComponentRefs[key];
                        // 确保引用的是同一个组件
                        if (referencedComponent == component)
                        {
                            isReferenced = true;
                        }
                    }
                    
                    // 获取handler的专属颜色
                    Color handlerColor = GetHandlerColor(iconHandler);

                    if (isReferenced)
                    {
                        // 绘制层级指示器 - 在图标右下角绘制小圆点
                        float indicatorSize = 4f;
                        Rect indicatorRect = new Rect(
                            rect.x + rect.width - indicatorSize, 
                            rect.y + rect.height - indicatorSize, 
                            indicatorSize, 
                            indicatorSize
                        );
                        
                        // 绘制白色背景圆圈（提高可见性）
                        EditorGUI.DrawRect(new Rect(indicatorRect.x - 1, indicatorRect.y - 1, indicatorRect.width + 2, indicatorRect.height + 2), Color.white);
                        
                        // 绘制handler专属颜色的圆点
                        EditorGUI.DrawRect(indicatorRect, handlerColor);
                        
                        // 在图标背景添加轻微的颜色提示
                        Color bgColor = new Color(handlerColor.r, handlerColor.g, handlerColor.b, 0.1f);
                        EditorGUI.DrawRect(rect, bgColor);
                    }
                    else if (highlightedComponents.ContainsKey(component.GetInstanceID()))
                    {
                        // 检查是否是真正需要高亮的组件
                        bool shouldHighlight = false;

                        // 检查是否在任何handler中被引用
                        var stage = PrefabStageUtility.GetCurrentPrefabStage();
                        if (stage != null && stage.prefabContentsRoot != null)
                        {
                            var allHandlers = stage.prefabContentsRoot.GetComponentsInChildren<AutoUIBinderBase>(true);
                            foreach (var handler in allHandlers)
                            {
                                if (handler.ComponentRefs.ContainsKey(key) && handler.ComponentRefs[key] == component)
                                {
                                    shouldHighlight = true;
                                    break;
                                }
                            }
                        }

                        if (!shouldHighlight)
                        {
                            // 如果不应该高亮，移除高亮状态
                            highlightedComponents.Remove(component.GetInstanceID());
                        }
                        else
                        {
                            // 如果是高亮状态但未绑定，显示黄色边框
                            Color originalColor = GUI.color;
                            GUI.color = new Color(1f, 1f, 0f, 0.5f);
                            EditorGUI.DrawRect(new Rect(rect.x - 1, rect.y - 1, rect.width + 2, rect.height + 2), GUI.color);
                            GUI.color = originalColor;
                        }
                    }
                }
            }
        }
        // 添加新方法：清理其他AutoUIBinderBase中的引用
        private static void CleanupExistingReference(Component comp, AutoUIBinderBase currentHandler)
        {
            if (comp == null || currentHandler == null) return;

            // 获取当前预制体的根节点
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null || stage.prefabContentsRoot == null) return;

            // 查找所有AutoUIBinderBase
            var allHandlers = stage.prefabContentsRoot.GetComponentsInChildren<AutoUIBinderBase>(true);
            string key = GetNodeComponentKey(comp);

            foreach (var handler in allHandlers)
            {
                // 跳过当前的handler
                if (handler == currentHandler) continue;

                // 如果在其他handler中找到了这个组件的引用，清理它
                if (handler.ComponentRefs != null && handler.ComponentRefs.ContainsKey(key))
                {
                    handler.RemoveComponentRef(key);
                    EditorUtility.SetDirty(handler.gameObject);
                }
            }
        }

        private static void OnHierarchyChanged()
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null || stage.prefabContentsRoot == null) return;


            // 获取所有AutoUIBinderBase组件
            var allHandlers = stage.prefabContentsRoot.GetComponentsInChildren<AutoUIBinderBase>(true);
            
            // 检查所有高亮组件
            var highlightedToRemove = new List<int>();
            foreach (var kvp in highlightedComponents)
            {
                Component comp = EditorUtility.InstanceIDToObject(kvp.Key) as Component;
                if (comp == null)
                {
                    highlightedToRemove.Add(kvp.Key);
                    continue;
                }

                // 找到最近的AutoUIBinderBase父级
                AutoUIBinderBase nearestHandler = null;
                Transform current = comp.transform;
                while (current != null)
                {
                    var handler = current.GetComponent<AutoUIBinderBase>();
                    if (handler != null)
                    {
                        nearestHandler = handler;
                        break;
                    }
                    current = current.parent;
                }

                if (nearestHandler != null)
                {
                    string key = GetNodeComponentKey(comp);
                    
                    // 区分AutoUIBinderBase和普通组件的处理
                    if (comp is AutoUIBinderBase)
                    {
                        // 对于AutoUIBinderBase，检查是否在父级handler的引用列表中
                        Transform parent = comp.gameObject.transform.parent;
                        bool foundInParentHandler = false;
                        
                        while (parent != null)
                        {
                            var parentHandler = parent.GetComponent<AutoUIBinderBase>();
                            if (parentHandler != null)
                            {
                                if (parentHandler.ComponentRefs.ContainsKey(key) && parentHandler.ComponentRefs[key] == comp)
                                {
                                    foundInParentHandler = true;
                                }
                                break; // 找到第一个父级handler就停止
                            }
                            parent = parent.parent;
                        }
                        
                        if (!foundInParentHandler)
                        {
                            highlightedToRemove.Add(kvp.Key);
                        }
                    }
                    else
                    {
                        // 对于普通组件，检查是否在最近handler的引用列表中
                        if (!nearestHandler.ComponentRefs.ContainsKey(key))
                        {
                            highlightedToRemove.Add(kvp.Key);
                        }
                    }
                }
                else
                {
                    highlightedToRemove.Add(kvp.Key);
                }
            }

            // 移除无效的高亮状态
            foreach (var id in highlightedToRemove)
            {
                highlightedComponents.Remove(id);
            }
            
            foreach (var handler in allHandlers)
            {
                ValidateComponentReferences(handler);
            }
            
            // 清理孤儿组件（失去父级AutoUIBinderBase的组件）
            CleanupOrphanComponents(stage.prefabContentsRoot);
            
            // 如果有任何清理操作，刷新界面
            if (highlightedToRemove.Count > 0)
            {
                RequestRepaint();
            }
        }

        private static void ValidateComponentReferences(AutoUIBinderBase handler)
        {
            if (handler == null || handler.ComponentRefs == null) return;

            var keysToRemove = new List<string>();
            var componentsToUnhighlight = new List<Component>();

            foreach (var kvp in handler.ComponentRefs)
            {
                var component = kvp.Value;
                if (component == null) continue;

                // 如果引用的是AutoUIBinderBase组件，则允许保留
                if (component is AutoUIBinderBase)
                    continue;

                // 检查组件所在的GameObject是否有更近的AutoUIBinderBase
                Transform current = component.transform;
                bool shouldRemove = false;

                while (current != null && current != handler.transform)
                {
                    if (current.GetComponent<AutoUIBinderBase>() != null)
                    {
                        shouldRemove = true;
                        break;
                    }
                    current = current.parent;
                }

                if (shouldRemove)
                {
                    keysToRemove.Add(kvp.Key);
                    componentsToUnhighlight.Add(component);
                }
            }

            // 移除无效的引用
            foreach (var key in keysToRemove)
            {
                handler.RemoveComponentRef(key);
            }

            // 取消高亮状态
            foreach (var component in componentsToUnhighlight)
            {
                if (component != null)
                {
                    int compId = component.GetInstanceID();
                    if (highlightedComponents.ContainsKey(compId))
                    {
                        highlightedComponents.Remove(compId);
                    }
                }
            }

            if (keysToRemove.Count > 0)
            {
                EditorUtility.SetDirty(handler.gameObject);
                RequestRepaint();
            }
        }
        
        // 清理孤儿组件（失去父级AutoUIBinderBase的组件）
        private static void CleanupOrphanComponents(GameObject prefabRoot)
        {
            if (prefabRoot == null) return;
            
            
            var componentsToUnhighlight = new List<int>();
            
            // 检查所有当前高亮的组件
            foreach (var kvp in highlightedComponents.ToList())
            {
                int componentID = kvp.Key;
                Component comp = EditorUtility.InstanceIDToObject(componentID) as Component;
                
                
                // 如果组件已被删除，直接移除高亮状态
                if (comp == null)
                {
                    componentsToUnhighlight.Add(componentID);
                    continue;
                }
                
                // 检查该组件是否还有有效的父级AutoUIBinderBase
                AutoUIBinderBase iconHandler = FindIconHandler(comp);
                
                if (iconHandler == null)
                {
                    // 没有找到父级AutoUIBinderBase，移除高亮状态
                    componentsToUnhighlight.Add(componentID);
                }
            }
            
            // 移除孤儿组件的高亮状态
            foreach (var componentID in componentsToUnhighlight)
            {
                highlightedComponents.Remove(componentID);
            }
            
            // 如果有组件被清理，刷新界面
            if (componentsToUnhighlight.Count > 0)
            {
                RequestRepaint();
            }
        }

        
        // 获取唯一的节点名称
        private static string GetUniqueNodeName(AutoUIBinderBase handler, string originalName)
        {
            if (handler == null || string.IsNullOrEmpty(originalName)) return originalName;
            
            // 收集所有已使用的节点名称
            var usedNames = new HashSet<string>();
            foreach (var kvp in handler.ComponentRefs)
            {
                if (kvp.Value != null)
                {
                    usedNames.Add(kvp.Value.gameObject.name);
                }
            }
            
            // 如果原名称没有被使用，直接返回
            if (!usedNames.Contains(originalName))
                return originalName;
            
            // 如果名称已被使用，生成新的名称
            int counter = 1;
            string uniqueName;
            do
            {
                uniqueName = $"{originalName}_{counter}";
                counter++;
            } 
            while (usedNames.Contains(uniqueName) && counter < 100); // 防止无限循环
            
            return uniqueName;
        }
        
        // 为AutoUIBinderBase生成唯一颜色
        private static Color GetHandlerColor(AutoUIBinderBase handler)
        {
            if (handler == null) return Color.white;
            
            int handlerID = handler.GetInstanceID();
            if (!handlerColors.ContainsKey(handlerID))
            {
                // 生成基于handler名称和路径的稳定颜色
                string uniqueKey = GetHandlerUniqueKey(handler);
                int hash = uniqueKey.GetHashCode();
                
                // 使用System.Random确保稳定性
                System.Random random = new System.Random(hash);
                
                // 生成饱和度和亮度较高的颜色，确保可见性
                float hue = (float)random.NextDouble();
                float saturation = 0.6f + (float)random.NextDouble() * 0.3f; // 0.6-0.9
                float value = 0.7f + (float)random.NextDouble() * 0.2f; // 0.7-0.9
                
                Color color = Color.HSVToRGB(hue, saturation, value);
                handlerColors[handlerID] = color;
            }
            
            return handlerColors[handlerID];
        }
        
        // 生成handler的唯一标识符
        private static string GetHandlerUniqueKey(AutoUIBinderBase handler)
        {
            if (handler == null) return "";
            
            // 使用Transform路径和名称组合确保唯一性
            string path = "";
            Transform current = handler.transform;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            
            return path + handler.GetType().Name;
        }
        
        // 查找组件对应的AutoUIBinderBase
        private static AutoUIBinderBase FindIconHandler(Component component)
        {
            if (component == null) return null;
            
            AutoUIBinderBase iconHandler = null;
            
            // 检查当前组件是否是AutoUIBinderBase
            bool isAutoUIBinderBase = component is AutoUIBinderBase;

            if (isAutoUIBinderBase)
            {
                // 如果是AutoUIBinderBase，查找最近的父级AutoUIBinderBase
                var parent = component.gameObject.transform.parent;
                while (parent != null)
                {
                    iconHandler = parent.GetComponent<AutoUIBinderBase>();
                    if (iconHandler != null)
                        break;
                    parent = parent.parent;
                }
            }
            else
            {
                // 如果不是AutoUIBinderBase，先查找自身，然后查找最近的父级
                iconHandler = component.gameObject.GetComponent<AutoUIBinderBase>();
                if (iconHandler == null)
                {
                    var parent = component.transform.parent;
                    while (parent != null)
                    {
                        iconHandler = parent.GetComponent<AutoUIBinderBase>();
                        if (iconHandler != null)
                            break;
                        parent = parent.parent;
                    }
                }
            }
            
            return iconHandler;
        }
        
        // 清理已删除handler的颜色缓存
        private static void CleanupHandlerColors()
        {
            var keysToRemove = new List<int>();
            foreach (var kvp in handlerColors)
            {
                var handler = EditorUtility.InstanceIDToObject(kvp.Key) as AutoUIBinderBase;
                if (handler == null)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                handlerColors.Remove(key);
            }
        }

        private static void HandleObjectRename(GameObject renamedObject, string oldName)
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null || stage.prefabContentsRoot == null) return;

            // 获取对象上的所有组件
            var components = renamedObject.GetComponents<Component>();
            foreach (var component in components)
            {
                if (component == null) continue;

                // 找到最近的AutoUIBinderBase父级
                AutoUIBinderBase nearestHandler = null;
                Transform current = component.transform;
                while (current != null)
                {
                    var handler = current.GetComponent<AutoUIBinderBase>();
                    if (handler != null)
                    {
                        nearestHandler = handler;
                        break;
                    }
                    current = current.parent;
                }

                if (nearestHandler != null)
                {
                    // 首先检查是否有节点名称冲突（不考虑组件类型）
                    bool hasNameConflict = false;
                    foreach (var kvp in nearestHandler.ComponentRefs)
                    {
                        if (kvp.Value != null && 
                            kvp.Value.gameObject != renamedObject && // 不是自己
                            kvp.Value.gameObject.name == renamedObject.name) // 名字相同
                        {
                            hasNameConflict = true;
                            break;
                        }
                    }

                    // 如果有名称冲突，生成新的唯一名称
                    if (hasNameConflict)
                    {
                        string uniqueName = GetUniqueNodeName(nearestHandler, renamedObject.name);
                        renamedObject.name = uniqueName;
                    }
                    
                    // 构造旧的key和新的key
                    string oldKey = $"{oldName.Replace(" ", "_")}_{component.GetType().Name}";
                    string newKey = GetNodeComponentKey(component);
                    // 检查是否存在旧引用
                    if (nearestHandler.ComponentRefs.ContainsKey(oldKey))
                    {
                        var oldComponent = nearestHandler.ComponentRefs[oldKey];
                        if (oldComponent == component)
                        {
                            nearestHandler.RemoveComponentRef(oldKey);
                            nearestHandler.AddComponentRef(newKey, component);

                            // 确保高亮状态保持
                            int componentId = component.GetInstanceID();
                            highlightedComponents[componentId] = true;

                            EditorUtility.SetDirty(nearestHandler.gameObject);
                        }
                    }
                }
            }

            RequestRepaint();
        }
    }
} 