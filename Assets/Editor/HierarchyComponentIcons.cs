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
    
    // 用于存储每个ShowComponentIconsBase的唯一颜色
    private static Dictionary<int, Color> handlerColors = new Dictionary<int, Color>();
    
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

        // 获取当前打开的预制体
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null)
        {
            // 获取预制体的根对象
            GameObject prefabRoot = stage.prefabContentsRoot;
            if (prefabRoot != null)
            {
                // 查找所有ShowComponentIconsBase组件
                var iconHandlers = prefabRoot.GetComponentsInChildren<ShowComponentIconsBase>(true);
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

        // 获取预制体的根对象
        GameObject prefabRoot = stage.prefabContentsRoot;
        if (prefabRoot == null) return;

        // 查找所有ShowComponentIconsBase组件
        var iconHandlers = prefabRoot.GetComponentsInChildren<ShowComponentIconsBase>(true);
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
                ShowComponentIconsBase iconHandler = FindIconHandler(comp);
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
                // 检查当前组件是否是ShowComponentIconsBase
                bool isShowComponentIconsBase = comp is ShowComponentIconsBase;
                
                // 从组件所在的GameObject开始向上查找ShowComponentIconsBase
                ShowComponentIconsBase iconHandler = null;
                
                if (isShowComponentIconsBase)
                {
                    // 检查是否是预制体的根节点
                    var stage = PrefabStageUtility.GetCurrentPrefabStage();
                    if (stage != null && stage.prefabContentsRoot == comp.gameObject)
                    {
                        // 如果是根节点的ShowComponentIconsBase，消费事件后返回
                        current.Use();
                        return;
                    }

                    // 如果不是根节点，从父级开始查找
                    var parent = comp.gameObject.transform.parent;
                    while (parent != null)
                    {
                        iconHandler = parent.GetComponent<ShowComponentIconsBase>();
                        if (iconHandler != null)
                        {
                            // 找到父级的ShowComponentIconsBase后，将当前的ShowComponentIconsBase添加到其字典中
                            string key = GetNodeComponentKey(comp);
                            if (!iconHandler.ComponentRefs.ContainsKey(key))
                            {
                                highlightedComponents[componentID] = true;
                                iconHandler.AddComponentRef(key, comp);
                                EditorUtility.SetDirty(iconHandler.gameObject);
                            }
                            else if (highlightedComponents.ContainsKey(componentID))
                            {
                                // 如果已经在字典中且处于高亮状态，则移除
                                highlightedComponents.Remove(componentID);
                                iconHandler.RemoveComponentRef(key);
                                EditorUtility.SetDirty(iconHandler.gameObject);
                            }
                            
                            // 标记为已修改并消费事件
                            EditorUtility.SetDirty(iconHandler.gameObject);
                            EditorWindow.GetWindow<SceneView>().Repaint();
                            RequestRepaint();
                            current.Use();
                            return;
                        }
                        parent = parent.parent;
                    }
                }

                // 如果不是ShowComponentIconsBase或没找到父级Handler，使用原有逻辑
                iconHandler = comp.gameObject.GetComponent<ShowComponentIconsBase>();
                if (iconHandler == null)
                {
                    var parent = comp.gameObject.transform.parent;
                    while (parent != null)
                    {
                        iconHandler = parent.GetComponent<ShowComponentIconsBase>();
                        if (iconHandler != null)
                            break;
                        parent = parent.parent;
                    }
                }

                if (iconHandler != null)
                {
                    string key = GetNodeComponentKey(comp);
                    
                    // 切换组件的高亮状态
                    if (highlightedComponents.ContainsKey(componentID))
                    {
                        bool referenceRemoved = false;
                        
                        // 查找当前组件实际被哪个ShowComponentIconsBase引用
                        var stage = PrefabStageUtility.GetCurrentPrefabStage();
                        if (stage != null && stage.prefabContentsRoot != null)
                        {
                            var allHandlers = stage.prefabContentsRoot.GetComponentsInChildren<ShowComponentIconsBase>(true);
                            
                            // 查找实际引用该组件的ShowComponentIconsBase
                            foreach (var handler in allHandlers)
                            {
                                if (handler.ComponentRefs != null && handler.ComponentRefs.ContainsKey(key))
                                {
                                    handler.RemoveComponentRef(key);
                                    EditorUtility.SetDirty(handler.gameObject);
                                    referenceRemoved = true;
                                }
                            }
                        }
                        
                        // 无论是否找到并移除了引用，都要移除高亮状态
                        highlightedComponents.Remove(componentID);
                        
                        // 如果没有找到引用但存在当前handler，也尝试从当前handler移除
                        if (!referenceRemoved && iconHandler != null)
                        {
                            iconHandler.RemoveComponentRef(key);
                            EditorUtility.SetDirty(iconHandler.gameObject);
                        }
                    }
                    else
                    {
                        // 在添加新引用前，清理其他ShowComponentIconsBase中的引用
                        CleanupExistingReference(comp, iconHandler);

                        // 智能处理命名冲突
                        if (iconHandler.ComponentRefs.ContainsKey(key))
                        {
                            // 生成新的节点名称和键名
                            string originalNodeName = comp.gameObject.name;
                            string newNodeName = GetUniqueNodeName(iconHandler, originalNodeName);
                            string newKey = $"{newNodeName.Replace(" ", "_")}_{comp.GetType().Name}";
                            
                            // 直接重命名GameObject
                            comp.gameObject.name = newNodeName;
                            
                            highlightedComponents[componentID] = true;
                            iconHandler.AddComponentRef(newKey, comp);
                            
                            Debug.Log($"[UITool] 自动重命名节点: '{originalNodeName}' -> '{newNodeName}' (Key: {newKey})");
                        }
                        else
                        {
                            // 没有冲突，直接添加
                            highlightedComponents[componentID] = true;
                            iconHandler.AddComponentRef(key, comp);
                            Debug.Log($"[UITool] 添加组件引用: {key}");
                        }
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
        string nodeName = component.gameObject.name.Replace(" ", "_");
        return $"{nodeName}_{component.GetType().Name}";
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

        // 检查自身或任意父级对象是否继承了ShowComponentIconsBase
        Transform current = gameObject.transform;
        while (current != null)
        {
            if (current.GetComponent<ShowComponentIconsBase>() != null)
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
                Debug.LogWarning("[UITool] HierarchyWindowItemOnGUI: Event.current is null");
                return;
            }

            if (selectionRect.width <= 0 || selectionRect.height <= 0)
            {
                Debug.LogWarning($"[UITool] HierarchyWindowItemOnGUI: Invalid selectionRect - Width: {selectionRect.width}, Height: {selectionRect.height}, Position: ({selectionRect.x}, {selectionRect.y})");
                return;
            }

            GameObject gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (gameObject == null)
                return;

            // 检查该GameObject是否应该显示图标
            if (!ShouldShowIcons(gameObject))
                return;

            // 检查是否有ShowComponentIconsBase组件（用于判断是否显示图标）
            var baseComponent = gameObject.GetComponent<ShowComponentIconsBase>();
            
            // 如果当前节点有ShowComponentIconsBase组件，绘制背景色标识
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

                // 如果组件被高亮，使用其关联的ShowComponentIconsBase的颜色绘制高亮背景
                if (highlightedComponents.ContainsKey(componentID) && highlightedComponents[componentID])
                {
                    // 查找该组件关联的ShowComponentIconsBase
                    ShowComponentIconsBase handler = null;
                    Transform current = component.transform;
                    while (current != null)
                    {
                        handler = current.GetComponent<ShowComponentIconsBase>();
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
            // 获取该组件关联的ShowComponentIconsBase
            ShowComponentIconsBase iconHandler = FindIconHandler(component);

            // 绘制组件图标
            GUI.DrawTexture(rect, content.image);

            // 如果找到了ShowComponentIconsBase，绘制层级指示器
            if (iconHandler != null)
            {
                string key = GetNodeComponentKey(component);
                bool isReferenced = iconHandler.ComponentRefs.ContainsKey(key);
                
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
                    // 如果是高亮状态但未绑定，显示黄色边框
                    Color originalColor = GUI.color;
                    GUI.color = new Color(1f, 1f, 0f, 0.5f);
                    EditorGUI.DrawRect(new Rect(rect.x - 1, rect.y - 1, rect.width + 2, rect.height + 2), GUI.color);
                    GUI.color = originalColor;
                }
                else
                {
                    // 即使没有被引用，也显示潜在的handler颜色指示器（半透明）
                    float indicatorSize = 3f;
                    Rect indicatorRect = new Rect(
                        rect.x + rect.width - indicatorSize, 
                        rect.y + rect.height - indicatorSize, 
                        indicatorSize, 
                        indicatorSize
                    );
                    
                    // 绘制半透明的颜色指示器
                    Color dimmedColor = new Color(handlerColor.r, handlerColor.g, handlerColor.b, 0.3f);
                    EditorGUI.DrawRect(indicatorRect, dimmedColor);
                }
            }
        }
    }
    // 添加新方法：清理其他ShowComponentIconsBase中的引用
    private static void CleanupExistingReference(Component comp, ShowComponentIconsBase currentHandler)
    {
        if (comp == null || currentHandler == null) return;

        // 获取当前预制体的根节点
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage == null || stage.prefabContentsRoot == null) return;

        // 查找所有ShowComponentIconsBase
        var allHandlers = stage.prefabContentsRoot.GetComponentsInChildren<ShowComponentIconsBase>(true);
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

        Debug.Log("[UITool] OnHierarchyChanged 被触发");

        // 获取所有ShowComponentIconsBase组件
        var allHandlers = stage.prefabContentsRoot.GetComponentsInChildren<ShowComponentIconsBase>(true);
        Debug.Log($"[UITool] 找到 {allHandlers.Length} 个ShowComponentIconsBase组件");
        
        foreach (var handler in allHandlers)
        {
            ValidateComponentReferences(handler);
        }
        
        // 清理孤儿组件（失去父级ShowComponentIconsBase的组件）
        CleanupOrphanComponents(stage.prefabContentsRoot);
    }

    private static void ValidateComponentReferences(ShowComponentIconsBase handler)
    {
        if (handler == null || handler.ComponentRefs == null) return;

        var keysToRemove = new List<string>();
        var componentsToUnhighlight = new List<Component>();

        foreach (var kvp in handler.ComponentRefs)
        {
            var component = kvp.Value;
            if (component == null) continue;

            // 如果引用的是ShowComponentIconsBase组件，则允许保留
            if (component is ShowComponentIconsBase)
                continue;

            // 检查组件所在的GameObject是否有更近的ShowComponentIconsBase
            Transform current = component.transform;
            bool shouldRemove = false;

            while (current != null && current != handler.transform)
            {
                if (current.GetComponent<ShowComponentIconsBase>() != null)
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
    
    // 清理孤儿组件（失去父级ShowComponentIconsBase的组件）
    private static void CleanupOrphanComponents(GameObject prefabRoot)
    {
        if (prefabRoot == null) return;
        
        Debug.Log($"[UITool] 开始清理孤儿组件，当前高亮组件数量: {highlightedComponents.Count}");
        
        var componentsToUnhighlight = new List<int>();
        
        // 检查所有当前高亮的组件
        foreach (var kvp in highlightedComponents.ToList())
        {
            int componentID = kvp.Key;
            Component comp = EditorUtility.InstanceIDToObject(componentID) as Component;
            
            Debug.Log($"[UITool] 检查组件ID {componentID}: {(comp != null ? $"{comp.gameObject.name}.{comp.GetType().Name}" : "null")}");
            
            // 如果组件已被删除，直接移除高亮状态
            if (comp == null)
            {
                componentsToUnhighlight.Add(componentID);
                Debug.Log($"[UITool] 组件已删除，标记移除高亮状态: {componentID}");
                continue;
            }
            
            // 检查该组件是否还有有效的父级ShowComponentIconsBase
            ShowComponentIconsBase iconHandler = FindIconHandler(comp);
            Debug.Log($"[UITool] 组件 {comp.gameObject.name}.{comp.GetType().Name} 的iconHandler: {(iconHandler != null ? iconHandler.gameObject.name : "null")}");
            
            if (iconHandler == null)
            {
                // 没有找到父级ShowComponentIconsBase，移除高亮状态
                componentsToUnhighlight.Add(componentID);
                Debug.Log($"[UITool] 清理孤儿组件高亮状态: {comp.gameObject.name}.{comp.GetType().Name}");
            }
        }
        
        Debug.Log($"[UITool] 需要移除高亮状态的组件数量: {componentsToUnhighlight.Count}");
        
        // 移除孤儿组件的高亮状态
        foreach (var componentID in componentsToUnhighlight)
        {
            highlightedComponents.Remove(componentID);
            Debug.Log($"[UITool] 已移除组件 {componentID} 的高亮状态");
        }
        
        // 如果有组件被清理，刷新界面
        if (componentsToUnhighlight.Count > 0)
        {
            Debug.Log($"[UITool] 清理完成，刷新界面");
            RequestRepaint();
        }
    }

    
    // 获取唯一的节点名称
    private static string GetUniqueNodeName(ShowComponentIconsBase handler, string originalName)
    {
        if (handler == null || string.IsNullOrEmpty(originalName)) return "";
        
        // 检查原名称是否与现有的节点名称冲突
        var existingNodeNames = new HashSet<string>();
        foreach (var kvp in handler.ComponentRefs)
        {
            if (kvp.Value != null)
            {
                existingNodeNames.Add(kvp.Value.gameObject.name);
            }
        }
        
        // 如果原名称没有冲突，直接返回
        if (!existingNodeNames.Contains(originalName))
            return originalName;
        
        // 如果冲突，尝试添加数字后缀
        int counter = 1;
        string uniqueName;
        do
        {
            uniqueName = $"{originalName}_{counter}";
            counter++;
        } 
        while (existingNodeNames.Contains(uniqueName) && counter < 100); // 防止无限循环
        
        return uniqueName;
    }
    
    // 为ShowComponentIconsBase生成唯一颜色
    private static Color GetHandlerColor(ShowComponentIconsBase handler)
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
            
            Debug.Log($"[UITool] 为 '{handler.gameObject.name}' 生成颜色: {color} (Key: {uniqueKey})");
        }
        
        return handlerColors[handlerID];
    }
    
    // 生成handler的唯一标识符
    private static string GetHandlerUniqueKey(ShowComponentIconsBase handler)
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
    
    // 查找组件对应的ShowComponentIconsBase
    private static ShowComponentIconsBase FindIconHandler(Component component)
    {
        if (component == null) return null;
        
        ShowComponentIconsBase iconHandler = null;
        
        // 检查当前组件是否是ShowComponentIconsBase
        bool isShowComponentIconsBase = component is ShowComponentIconsBase;
        
        if (isShowComponentIconsBase)
        {
            // 如果是ShowComponentIconsBase，查找最近的父级ShowComponentIconsBase
            var parent = component.gameObject.transform.parent;
            while (parent != null)
            {
                iconHandler = parent.GetComponent<ShowComponentIconsBase>();
                if (iconHandler != null)
                    break;
                parent = parent.parent;
            }
        }
        else
        {
            // 如果不是ShowComponentIconsBase，先查找自身，然后查找最近的父级
            iconHandler = component.gameObject.GetComponent<ShowComponentIconsBase>();
            if (iconHandler == null)
            {
                var parent = component.transform.parent;
                while (parent != null)
                {
                    iconHandler = parent.GetComponent<ShowComponentIconsBase>();
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
            var handler = EditorUtility.InstanceIDToObject(kvp.Key) as ShowComponentIconsBase;
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
}
} 