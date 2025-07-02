using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UITool;

[CustomEditor(typeof(AutoUIBinderBase), true)]
public class AutoUIBinderBaseEditor : Editor
{
    private bool showInfoFoldout = true;  // 添加折叠状态变量
    
    public override void OnInspectorGUI()
    {
        // 首先绘制默认的Inspector内容
        base.OnInspectorGUI();
        
        EditorGUILayout.Space(10);
        
        // 绘制信息区域
        DrawInfoSection();
        
        EditorGUILayout.Space(10);
        
        // 绘制操作按钮区域
        DrawActionButtons();
        
        EditorGUILayout.Space(5);
        
        // 绘制状态信息
        DrawStatusInfo();
    }
    
    private void DrawInfoSection()
    {
        var autoUIBinderBase = target as AutoUIBinderBase;
        if (autoUIBinderBase == null) return;
        
        // 折叠标题样式
        var foldoutStyle = new GUIStyle(EditorStyles.foldout);
        foldoutStyle.fontSize = 12;
        foldoutStyle.fontStyle = FontStyle.Bold;
        
        // 使用EditorGUILayout.Foldout来创建可折叠的标题
        showInfoFoldout = EditorGUILayout.Foldout(showInfoFoldout, "数据信息", true, foldoutStyle);
        
        if (showInfoFoldout)
        {
            // 信息区域
            var infoAreaStyle = new GUIStyle(EditorStyles.helpBox);
            infoAreaStyle.padding = new RectOffset(10, 10, 8, 8);
            EditorGUILayout.BeginVertical(infoAreaStyle);
            
            // 使用表格式布局显示信息
            var labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.fontSize = 12;
            var valueStyle = new GUIStyle(EditorStyles.label);
            valueStyle.fontSize = 12;
            valueStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.8f, 0.8f, 0.8f) : new Color(0.2f, 0.2f, 0.2f);
            
            // 组件信息
            int componentCount = autoUIBinderBase.ComponentRefs.Count;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("已绑定组件", labelStyle, GUILayout.Width(120));
            EditorGUILayout.LabelField($"{componentCount} 个", valueStyle);
            EditorGUILayout.EndHorizontal();
            
            // 类名信息
            string className = target.GetType().Name;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("类名", labelStyle, GUILayout.Width(120));
            EditorGUILayout.LabelField(className, valueStyle);
            EditorGUILayout.EndHorizontal();
            
            // 预制体状态
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            bool inPrefabMode = stage != null;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("预制体状态", labelStyle, GUILayout.Width(120));
            var statusStyle = new GUIStyle(valueStyle);
            statusStyle.normal.textColor = inPrefabMode ? 
                new Color(0.4f, 0.8f, 0.4f) : new Color(0.8f, 0.4f, 0.4f);
            EditorGUILayout.LabelField(inPrefabMode ? "预制体编辑模式" : "非预制体模式", statusStyle);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            // 预制体模式提示信息和按钮
            if (!inPrefabMode)
            {
                EditorGUILayout.Space(5);
                
                // 获取当前选中的游戏对象
                GameObject selectedObject = Selection.activeGameObject;
                if (selectedObject != null)
                {
                    // 检查是否是预制体实例
                    GameObject prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(selectedObject);
                    if (prefabRoot != null)
                    {
                        // 获取预制体资源
                        Object prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(prefabRoot);
                        if (prefabAsset != null)
                        {
                            EditorGUILayout.BeginHorizontal();
                            // 提示信息
                            EditorGUILayout.HelpBox("请进入预制体编辑模式来使用组件绑定功能", MessageType.Info);
                            
                            // 自定义按钮样式
                            var buttonStyle = new GUIStyle(GUI.skin.button);
                            buttonStyle.fontSize = 12;
                            buttonStyle.fontStyle = FontStyle.Bold;
                            buttonStyle.normal.textColor = Color.white;
                            buttonStyle.hover.textColor = Color.white;
                            buttonStyle.padding = new RectOffset(10, 10, 5, 5);
                            
                            // 设置按钮背景色为淡黄色
                            GUI.backgroundColor = new Color(1f, 0.92f, 0.7f);
                            
                            if (GUILayout.Button(new GUIContent("编辑预制体", EditorGUIUtility.IconContent("Prefab Icon").image), 
                                buttonStyle, GUILayout.Width(100), GUILayout.Height(38)))
                            {
                                AssetDatabase.OpenAsset(prefabAsset);
                            }
                            
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
            }
        }
    }
    
    private void DrawSeparatorLine()
    {
        var rect = EditorGUILayout.GetControlRect(false, 1);
        rect.height = 1;
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
    }
    
    private void DrawActionButtons()
    {
        var autoUIBinderBase = target as AutoUIBinderBase;
        if (autoUIBinderBase == null) return;
        
        // 检查是否在预制体模式
        var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        bool inPrefabMode = stage != null;
        
        // 如果不在预制体模式，不显示按钮
        if (!inPrefabMode) return;
        
        EditorGUILayout.BeginVertical();
        
        // 保存当前的GUI背景色
        Color originalColor = GUI.backgroundColor;
        
        // 主要操作按钮
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f); // 绿色
        if (GUILayout.Button("🚀 生成 UI 代码", GUILayout.Height(35)))
        {
            // 生成代码前先自动验证绑定
            ValidateBindings(false); // false表示自动调用
            GenerateUICode();
        }
        
        EditorGUILayout.Space(5);
        
        // 辅助按钮
        EditorGUILayout.BeginHorizontal();
        
        GUI.backgroundColor = new Color(0.6f, 0.6f, 0.8f); // 淡蓝色
        if (GUILayout.Button("📋 清空绑定", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("确认清空", "确定要清空所有组件绑定吗？", "确定", "取消"))
            {
                ClearAllBindings();
            }
        }
        
        GUI.backgroundColor = new Color(0.8f, 0.6f, 0.4f); // 橙色
        if (GUILayout.Button("🔍 验证绑定", GUILayout.Height(25)))
        {
            ValidateBindings();
        }
        
        EditorGUILayout.EndHorizontal();
        
        // 恢复原来的GUI背景色
        GUI.backgroundColor = originalColor;
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawStatusInfo()
    {
        var autoUIBinderBase = target as AutoUIBinderBase;
        if (autoUIBinderBase == null) return;
        
        // 检查是否在预制体模式
        var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        bool inPrefabMode = stage != null;
        
        if (!inPrefabMode)
            return;
        
        if (autoUIBinderBase.ComponentRefs.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "📝 使用指南:在预制体编辑模式下，点击Hierarchy窗口中的组件图标来绑定组件。", 
                MessageType.Info
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                $"✨ 已绑定 {autoUIBinderBase.ComponentRefs.Count} 个组件，点击'生成UI代码'按钮来生成代码。", 
                MessageType.None
            );
        }
    }

    private void GenerateUICode()
    {
        try
        {
            // 验证目标对象
            if (target == null)
            {
                EditorUtility.DisplayDialog("错误", "无法获取目标对象！", "确定");
                return;
            }
            
            // 获取目标脚本的类名
            var targetType = target.GetType();
            string className = targetType.Name;
            
            // 验证类名
            if (string.IsNullOrEmpty(className) || !IsValidClassName(className))
            {
                EditorUtility.DisplayDialog("错误", $"无效的类名: {className}", "确定");
                return;
            }

            // 检查并修改为partial类
            var script = MonoScript.FromMonoBehaviour(target as MonoBehaviour);
            if (script != null)
            {
                string scriptPath = AssetDatabase.GetAssetPath(script);
                string content = File.ReadAllText(scriptPath);
                
                // 检查是否已经是partial类
                if (!content.Contains("partial class " + className))
                {
                    // 替换类声明为partial
                    content = content.Replace("class " + className, "partial class " + className);
                    File.WriteAllText(scriptPath, content);
                    AssetDatabase.Refresh();
                    Debug.Log($"[UITool] 已自动将 {className} 修改为partial类");
                }
            }

            // 获取GlobalConfig
            var globalConfig = Resources.Load<UIPathConfig>("GlobalConfig");
            if (globalConfig == null)
            {
                EditorUtility.DisplayDialog("错误", "未找到GlobalConfig配置文件！\n请确保在Resources文件夹中存在GlobalConfig.asset文件。", "确定");
                return;
            }
            
            // 验证路径配置
            if (string.IsNullOrEmpty(globalConfig.Paths))
            {
                EditorUtility.DisplayDialog("错误", "GlobalConfig中的路径配置为空！", "确定");
                return;
            }

            // 构建生成文件的路径
            string genFolderPath = Path.Combine(globalConfig.Paths, "Gen");
            string classGenFolderPath = Path.Combine(genFolderPath, className);
            string genFilePath = Path.Combine(classGenFolderPath, $"{className}Gen.cs");
            string absoluteGenFolderPath = Path.Combine(Application.dataPath, "..", genFolderPath);
            string absoluteClassGenFolderPath = Path.Combine(Application.dataPath, "..", classGenFolderPath);
            string absoluteFilePath = Path.Combine(Application.dataPath, "..", genFilePath);

            // 获取当前预制体中的所有组件
            var autoUIBinderBase = target as AutoUIBinderBase;
            if (autoUIBinderBase == null) return;

            var componentRefs = autoUIBinderBase.ComponentRefs;
            
            // 生成代码
            StringBuilder codeBuilder = new StringBuilder();
            codeBuilder.AppendLine("//------------------------------------------------------------------------------");
            codeBuilder.AppendLine("// <auto-generated>");
            codeBuilder.AppendLine("//     此代码由工具自动生成。");
            codeBuilder.AppendLine("//     运行时版本:" + UnityEngine.Application.unityVersion);
            codeBuilder.AppendLine($"//     生成时间: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            codeBuilder.AppendLine($"//     组件数量: {componentRefs.Count}");
            codeBuilder.AppendLine($"//     预制体路径: {AssetDatabase.GetAssetPath(PrefabStageUtility.GetCurrentPrefabStage()?.prefabContentsRoot)}");
            codeBuilder.AppendLine($"//     脚本路径: {AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(target as MonoBehaviour))}");
            codeBuilder.AppendLine($"//     生成路径: {genFilePath}");
            codeBuilder.AppendLine("//");
            codeBuilder.AppendLine("//     对此文件的更改可能会导致不正确的行为，并且如果");
            codeBuilder.AppendLine("//     重新生成代码，这些更改将会丢失。");
            codeBuilder.AppendLine("// </auto-generated>");
            codeBuilder.AppendLine("//------------------------------------------------------------------------------");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("using UnityEngine;");
            codeBuilder.AppendLine("using UnityEngine.UI;");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine($"public partial class {className}");
            codeBuilder.AppendLine("{");

            // 检查是否有组件引用
            if (componentRefs.Count == 0)
            {
                codeBuilder.AppendLine("    // 暂无组件引用，请在预制体编辑模式下点击组件图标来添加引用");
            }
            else
            {
                // 验证和生成组件属性
                var validComponents = new List<KeyValuePair<string, Component>>();
                var invalidComponents = new List<string>();
                
                foreach (var kvp in componentRefs)
                {
                    if (kvp.Value == null)
                    {
                        invalidComponents.Add(kvp.Key);
                        continue;
                    }
                    
                    // 处理变量名 - 移除括号和空格
                    string variableName = kvp.Key;
                    variableName = System.Text.RegularExpressions.Regex.Replace(variableName, @"[\(\)]", "");
                    
                    if (!IsValidVariableName(variableName))
                    {
                        invalidComponents.Add($"{kvp.Key} (无效变量名)");
                        continue;
                    }
                    
                    validComponents.Add(new KeyValuePair<string, Component>(variableName, kvp.Value));
                }
                
                // 如果有无效组件，警告用户
                if (invalidComponents.Count > 0)
                {
                    string invalidList = string.Join("\n- ", invalidComponents);
                    Debug.LogWarning($"[UITool] 检测到 {invalidComponents.Count} 个无效组件引用，将被跳过:\n- {invalidList}");
                }
                
                // 生成有效组件的属性
                foreach (var kvp in validComponents)
                {
                    string variableName = kvp.Key;
                    string componentType = kvp.Value.GetType().Name;
    
                    codeBuilder.AppendLine($"    /// <summary>");
                    codeBuilder.AppendLine($"    /// 获取{componentType}组件: {variableName}");
                    codeBuilder.AppendLine($"    /// </summary>");
                    codeBuilder.AppendLine($"    public {componentType} {variableName}");
                    codeBuilder.AppendLine("    {");
                    codeBuilder.AppendLine("        get");
                    codeBuilder.AppendLine("        {");
                    codeBuilder.AppendLine($"            return this.GetComponentRef<{componentType}>(\"{variableName}\");");
                    codeBuilder.AppendLine("        }");
                    codeBuilder.AppendLine("    }");
                    codeBuilder.AppendLine();
                }
            }
        
        codeBuilder.AppendLine("}");

            // 验证路径是否存在
            if (!Directory.Exists(absoluteGenFolderPath))
            {
                try
                {
                    Directory.CreateDirectory(absoluteGenFolderPath);
                }
                catch (System.Exception ex)
                {
                    EditorUtility.DisplayDialog("错误", $"无法创建目录 {absoluteGenFolderPath}:\n{ex.Message}", "确定");
                    return;
                }
            }
            
            // 创建类特定目录结构
            try
            {
                Directory.CreateDirectory(absoluteClassGenFolderPath);
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("错误", $"无法创建类目录 {absoluteClassGenFolderPath}:\n{ex.Message}", "确定");
                return;
            }
    
            // 如果文件存在，直接删除
            if (File.Exists(absoluteFilePath))
            {
                try
                {
                    File.Delete(absoluteFilePath);
                }
                catch (System.Exception ex)
                {
                    EditorUtility.DisplayDialog("错误", $"无法删除现有文件 {absoluteFilePath}:\n{ex.Message}", "确定");
                    return;
                }
            }
    
            // 写入文件
            try
            {
                File.WriteAllText(absoluteFilePath, codeBuilder.ToString(), System.Text.Encoding.UTF8);
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("错误", $"无法写入文件 {absoluteFilePath}:\n{ex.Message}", "确定");
                return;
            }
            
            // 刷新资源
            AssetDatabase.Refresh();
            
            // 保存当前预制体的修改
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                EditorSceneManager.MarkSceneDirty(stage.scene);
            }
            
            // 显示成功消息
            EditorUtility.DisplayDialog("成功", $"UI代码已生成到:\n{genFilePath}\n\n包含 {componentRefs.Count} 个组件引用。", "确定");
            Debug.Log($"[UITool] UI代码生成成功: {genFilePath} (包含 {componentRefs.Count} 个组件)");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UITool] UI代码生成失败: {ex.Message}\n{ex.StackTrace}");
            EditorUtility.DisplayDialog("错误", $"UI代码生成失败:\n{ex.Message}", "确定");
        }
    }
    
    private bool IsValidClassName(string className)
    {
        if (string.IsNullOrEmpty(className)) return false;
        
        // 检查是否以字母或下划线开头
        if (!char.IsLetter(className[0]) && className[0] != '_') return false;
        
        // 检查其余字符是否是字母、数字或下划线
        for (int i = 1; i < className.Length; i++)
        {
            if (!char.IsLetterOrDigit(className[i]) && className[i] != '_')
                return false;
        }
        
        return true;
    }
    
    private bool IsValidVariableName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        
        // 检查是否以字母或下划线开头
        if (!char.IsLetter(name[0]) && name[0] != '_') return false;
        
        // 检查其余字符是否是字母、数字或下划线
        for (int i = 1; i < name.Length; i++)
        {
            if (!char.IsLetterOrDigit(name[i]) && name[i] != '_')
                return false;
        }
        
        // 检查是否是C#关键字
        string[] keywords = { "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while" };
        
        return !System.Array.Exists(keywords, k => k.Equals(name, System.StringComparison.OrdinalIgnoreCase));
    }

    private void ClearAllBindings()
    {
        var autoUIBinderBase = target as AutoUIBinderBase;
        if (autoUIBinderBase == null) return;
        
        autoUIBinderBase.ComponentRefs.Clear();
        EditorUtility.SetDirty(target);
        
        Debug.Log("[UITool] 已清空所有组件绑定");
        EditorUtility.DisplayDialog("成功", "已清空所有组件绑定", "确定");
    }
    
    private void ValidateBindings()
    {
        ValidateBindings(true);
    }
    
    private void ValidateBindings(bool isManualCall = true)
    {
        var autoUIBinderBase = target as AutoUIBinderBase;
        if (autoUIBinderBase == null) return;
        
        int validCount = 0;
        int invalidCount = 0;
        var invalidKeys = new System.Collections.Generic.List<string>();
        
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
        
        string prefix = isManualCall ? "[UITool] 手动验证" : "[UITool] 自动验证";
        
        if (invalidCount > 0)
        {
            // 直接清理无效绑定
            foreach (var key in invalidKeys)
            {
                autoUIBinderBase.RemoveComponentRef(key);
            }
            EditorUtility.SetDirty(target);
            
            string message = $"{prefix} - 清理了 {invalidCount} 个无效绑定：\n";
            foreach (var key in invalidKeys)
            {
                message += $"- {key}\n";
            }
            Debug.Log(message);
            Debug.Log($"{prefix} - 有效绑定: {validCount} 个，已清理无效绑定: {invalidCount} 个");
        }
        else
        {
            Debug.Log($"{prefix} - 所有 {validCount} 个绑定都是有效的！");
        }
    }
    
    private string GetRelativePath(Transform target, Transform root)
    {
        if (target == root)
            return "";

        List<string> path = new List<string>();
        Transform current = target;

        while (current != root && current != null)
        {
            path.Insert(0, current.name);
            current = current.parent;
        }

        return string.Join("/", path);
    }
} 