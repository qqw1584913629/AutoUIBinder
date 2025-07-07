using UnityEngine;
using UnityEditor;
using AutoUIBinder;
using AutoUIBinder.Editor;

[CustomEditor(typeof(AutoUIBinderBase), true)]
public class AutoUIBinderBaseEditor : Editor
{
    // 模块化管理器
    private UIEventManager eventManager;
    private ComponentBindingManager bindingManager;
    private CodeGenerator codeGenerator;
    private PrefabManager prefabManager;
    
    // UI绘制器
    private InfoSectionDrawer infoDrawer;
    private EventSectionDrawer eventDrawer;
    private ActionButtonsDrawer actionDrawer;
    
    private void OnEnable()
    {
        // 初始化管理器
        eventManager = new UIEventManager();
        bindingManager = new ComponentBindingManager();
        codeGenerator = new CodeGenerator();
        prefabManager = new PrefabManager();
        
        // 初始化绘制器
        infoDrawer = new InfoSectionDrawer(bindingManager, prefabManager);
        eventDrawer = new EventSectionDrawer(eventManager, bindingManager, codeGenerator);
        actionDrawer = new ActionButtonsDrawer(bindingManager, eventManager, codeGenerator);
    }
    
    public override void OnInspectorGUI()
    {
        var autoUIBinderBase = target as AutoUIBinderBase;
        if (autoUIBinderBase == null) return;
        
        // 首先绘制默认的Inspector内容
        base.OnInspectorGUI();
        
        EditorGUILayout.Space(10);
        
        // 使用模块化绘制器
        infoDrawer.DrawInfoSection(autoUIBinderBase);
        
        EditorGUILayout.Space(5);

        eventDrawer.DrawEventInfo(autoUIBinderBase);
        
        EditorGUILayout.Space(5);
        
        actionDrawer.DrawActionButtons(autoUIBinderBase);
    }
}