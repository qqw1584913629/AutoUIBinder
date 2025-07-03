using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Events;
using Microsoft.CSharp;
using System.CodeDom;

public class UIEventBinderWindow : EditorWindow
{
    private AutoUIBinderBase target;
    private Vector2 scrollPosition;
    private Dictionary<string, bool> componentFoldouts = new Dictionary<string, bool>();
    private Dictionary<string, string> selectedEvents = new Dictionary<string, string>();
    private Dictionary<string, string> methodNames = new Dictionary<string, string>();

    [MenuItem("Window/UI/Event Binder")]
    public static void ShowWindow()
    {
        var window = GetWindow<UIEventBinderWindow>("UI事件绑定");
        window.minSize = new Vector2(400, 300);
    }

    public void OnSelectionChange()
    {
        var selection = Selection.activeGameObject;
        if (selection != null)
        {
            target = selection.GetComponent<AutoUIBinderBase>();
        }
        Repaint();
    }

    private class EventInfo
    {
        public string Name;
        public System.Type EventType;
        public System.Type ParameterType;

        public override string ToString()
        {
            if (ParameterType != null)
            {
                // 使用更友好的类型名称显示
                string typeName = GetFriendlyTypeName(ParameterType);
                return $"{Name} ({typeName})";
            }
            return $"{Name} (无参数)";
        }
    }

    private EventInfo[] GetAvailableEvents(Component component)
    {
        var events = new List<EventInfo>();
        var type = component.GetType();

        // 获取所有UnityEvent字段（包括私有字段，因为Unity的序列化系统会处理它们）
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => typeof(UnityEventBase).IsAssignableFrom(f.FieldType));

        foreach (var field in fields)
        {
            var eventType = field.FieldType;
            System.Type parameterType = null;

            // 获取事件参数类型
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
                // 对于非泛型UnityEvent，尝试从Invoke方法获取参数类型
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

            // 获取字段上的SerializeField特性
            bool isSerializable = field.IsPublic || field.GetCustomAttribute<SerializeField>() != null;
            
            // 只添加可序列化的字段
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

        // 按名称排序，让列表更容易浏览
        return events.OrderBy(e => e.Name).ToArray();
    }

    private void OnGUI()
    {
        if (target == null)
        {
            EditorGUILayout.HelpBox("请选择一个UI面板对象", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField($"当前对象: {target.name}", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // 显示所有已绑定组件
        foreach (var pair in target.ComponentRefs)
        {
            if (!componentFoldouts.ContainsKey(pair.Key))
            {
                componentFoldouts[pair.Key] = false;
                selectedEvents[pair.Key] = "";
                methodNames[pair.Key] = "";
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            componentFoldouts[pair.Key] = EditorGUILayout.Foldout(componentFoldouts[pair.Key], pair.Key, true);
            
            // 显示组件类型
            EditorGUILayout.LabelField(pair.Value.GetType().Name, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            if (componentFoldouts[pair.Key])
            {
                EditorGUI.indentLevel++;
                
                // 获取可用事件
                var events = GetAvailableEvents(pair.Value);
                var eventNames = events.Select(e => e.ToString()).ToArray();
                
                // 事件选择
                int selectedIndex = System.Array.IndexOf(eventNames, selectedEvents[pair.Key]);
                selectedIndex = EditorGUILayout.Popup("事件", selectedIndex, eventNames);
                if (selectedIndex >= 0 && selectedIndex < eventNames.Length)
                {
                    selectedEvents[pair.Key] = eventNames[selectedIndex];
                    
                    // 自动生成方法名
                    if (string.IsNullOrEmpty(methodNames[pair.Key]))
                    {
                        var eventInfo = events[selectedIndex];
                        string baseName = $"On{char.ToUpper(pair.Key[0])}{pair.Key.Substring(1)}";
                        string eventName = eventInfo.Name;
                        eventName = eventName.Substring(1);
                        baseName += eventName;
                        methodNames[pair.Key] = baseName;
                    }
                }

                // 方法名输入
                methodNames[pair.Key] = EditorGUILayout.TextField("方法名", methodNames[pair.Key]);

                // 添加事件按钮
                if (GUILayout.Button("添加事件处理"))
                {
                    if (selectedIndex >= 0 && !string.IsNullOrEmpty(methodNames[pair.Key]))
                    {
                        var selectedEvent = events[selectedIndex];
                        AddEventHandler(pair.Key, selectedEvent.Name, methodNames[pair.Key], selectedEvent.ParameterType);
                    }
                }

                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        // 显示现有事件绑定
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("已绑定事件", EditorStyles.boldLabel);
        
        var methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<UIEventAttribute>();
            if (attr != null)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"组件: {attr.ComponentName}");
                EditorGUILayout.LabelField($"事件: {attr.EventType}");
                EditorGUILayout.LabelField($"方法: {method.Name}");
                
                if (GUILayout.Button("删除", GUILayout.Width(60)))
                {
                    if (EditorUtility.DisplayDialog("确认删除", 
                        $"确定要删除事件处理方法 {method.Name} 吗？", "确定", "取消"))
                    {
                        RemoveEventHandler(method.Name);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void AddEventHandler(string componentName, string eventName, string methodName, System.Type parameterType)
    {
        // 获取脚本文件路径
        var script = MonoScript.FromMonoBehaviour(target);
        var path = AssetDatabase.GetAssetPath(script);
        
        // 读取文件内容
        var lines = System.IO.File.ReadAllLines(path);
        var insertIndex = lines.Length - 1; // 默认在类的末尾插入
        
        // 找到合适的插入位置
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("class") && lines[i].Contains(target.GetType().Name))
            {
                // 找到类定义后的第一个大括号
                while (i < lines.Length && !lines[i].Contains("{")) i++;
                insertIndex = i + 1;
                break;
            }
        }

        // 生成事件处理方法
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
        newMethod.AppendLine("    }");

        // 插入新方法
        var newLines = lines.ToList();
        newLines.Insert(insertIndex, newMethod.ToString());
        
        // 保存文件
        System.IO.File.WriteAllLines(path, newLines);
        
        AssetDatabase.Refresh();
    }

    private static string GetFriendlyTypeName(System.Type type)
    {
        // 使用CSharpCodeProvider来获取类型的C#友好名称
        using (var provider = new CSharpCodeProvider())
        {
            var typeReference = new CodeTypeReference(type);
            string typeName = provider.GetTypeOutput(typeReference);
            
            // 移除命名空间前缀（如果有）
            int lastDot = typeName.LastIndexOf('.');
            if (lastDot >= 0)
            {
                typeName = typeName.Substring(lastDot + 1);
            }
            
            return typeName;
        }
    }

    private void RemoveEventHandler(string methodName)
    {
        // 获取脚本文件路径
        var script = MonoScript.FromMonoBehaviour(target);
        var path = AssetDatabase.GetAssetPath(script);
        
        // 读取文件内容
        var lines = System.IO.File.ReadAllLines(path);
        var newLines = new List<string>();
        
        bool skipLines = false;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains($"private void {methodName}"))
            {
                // 跳过方法定义和方法体
                skipLines = true;
                i--; // 回退一行以删除特性标记
                continue;
            }
            
            if (skipLines && lines[i].Trim() == "}")
            {
                skipLines = false;
                continue;
            }
            
            if (!skipLines)
            {
                newLines.Add(lines[i]);
            }
        }
        
        // 保存文件
        System.IO.File.WriteAllLines(path, newLines);
        
        AssetDatabase.Refresh();
    }
} 