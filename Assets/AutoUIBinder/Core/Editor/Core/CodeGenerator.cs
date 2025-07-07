using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using AutoUIBinder;

namespace AutoUIBinder.Editor
{
    /// <summary>
    /// 代码生成器 - 负责事件方法和UI代码生成
    /// </summary>
    public class CodeGenerator
    {
        /// <summary>
        /// 生成选中的事件方法
        /// </summary>
        public int GenerateSelectedEvents(AutoUIBinderBase target, List<UIEventManager.EventBinding> selectedEvents)
        {
            var eventsToGenerate = selectedEvents.Where(e => e.IsSelected && !e.AlreadyExists).ToList();
            
            if (eventsToGenerate.Count == 0)
            {
                return 0;
            }
            
            int generated = 0;
            foreach (var evt in eventsToGenerate)
            {
                AddEventHandlerToOriginalClass(target, evt.ComponentName, evt.EventName, evt.MethodName, evt.ParameterType);
                generated++;
            }
            
            return generated;
        }
        
        /// <summary>
        /// 生成UI代码
        /// </summary>
        public void GenerateUICode(AutoUIBinderBase target)
        {
            string className = target.GetType().Name;
            Debug.Log($"[AutoUIBinder] 为 {className} 生成UI代码，包含 {target.ComponentRefs.Count} 个组件");
            
            try
            {
                // 1. 确保目标类是partial类
                EnsurePartialClass(target);
                
                // 2. 获取生成路径配置
                var globalConfig = Resources.Load<UIPathConfig>("GlobalConfig");
                if (globalConfig == null)
                {
                    Debug.LogError("[AutoUIBinder] 未找到GlobalConfig配置文件");
                    return;
                }
                
                // 3. 创建生成目录
                string genFolderPath = System.IO.Path.Combine(globalConfig.Paths, "Gen");
                string classGenFolderPath = System.IO.Path.Combine(genFolderPath, className);
                
                if (!System.IO.Directory.Exists(classGenFolderPath))
                {
                    System.IO.Directory.CreateDirectory(classGenFolderPath);
                }
                
                // 4. 生成代码文件
                string genFilePath = System.IO.Path.Combine(classGenFolderPath, $"{className}Gen.cs");
                string absoluteFilePath = System.IO.Path.GetFullPath(genFilePath);
                
                GenerateUICodeFile(target, absoluteFilePath);
                
                // 5. 刷新资源
                AssetDatabase.Refresh();
                
                Debug.Log($"[AutoUIBinder] UI代码生成完成: {genFilePath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AutoUIBinder] UI代码生成失败: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }
        
        /// <summary>
        /// 清空指定组件的事件方法
        /// </summary>
        public int ClearComponentEventMethods(AutoUIBinderBase target, string componentName)
        {
            var script = MonoScript.FromMonoBehaviour(target as MonoBehaviour);
            var path = AssetDatabase.GetAssetPath(script);
            
            var lines = File.ReadAllLines(path);
            var newLines = new List<string>();
            
            bool skipMethod = false;
            int methodBraceCount = 0;
            int removedCount = 0;
            
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmedLine = line.Trim();
                
                if (trimmedLine.StartsWith("[UIEvent"))
                {
                    // 检查是否是指定组件的事件
                    if (trimmedLine.Contains($"\"{componentName}\""))
                    {
                        skipMethod = true;
                        methodBraceCount = 0;
                        removedCount++;
                        continue;
                    }
                }
                
                if (skipMethod)
                {
                    foreach (char c in trimmedLine)
                    {
                        if (c == '{') methodBraceCount++;
                        else if (c == '}') methodBraceCount--;
                    }
                    
                    if (methodBraceCount <= 0 && trimmedLine.Contains("}"))
                    {
                        skipMethod = false;
                        continue;
                    }
                    
                    continue;
                }
                
                newLines.Add(line);
            }
            
            File.WriteAllLines(path, newLines);
            AssetDatabase.Refresh();
            
            Debug.Log($"[AutoUIBinder] 已删除组件 '{componentName}' 的 {removedCount} 个事件方法");
            return removedCount;
        }
        
        /// <summary>
        /// 清空所有事件方法
        /// </summary>
        public int ClearAllEventMethods(AutoUIBinderBase target)
        {
            var script = MonoScript.FromMonoBehaviour(target as MonoBehaviour);
            var path = AssetDatabase.GetAssetPath(script);
            
            var lines = File.ReadAllLines(path);
            var newLines = new List<string>();
            
            bool skipMethod = false;
            int methodBraceCount = 0;
            int removedCount = 0;
            
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmedLine = line.Trim();
                
                if (trimmedLine.StartsWith("[UIEvent"))
                {
                    skipMethod = true;
                    methodBraceCount = 0;
                    removedCount++;
                    continue;
                }
                
                if (skipMethod)
                {
                    foreach (char c in trimmedLine)
                    {
                        if (c == '{') methodBraceCount++;
                        else if (c == '}') methodBraceCount--;
                    }
                    
                    if (methodBraceCount <= 0 && trimmedLine.Contains("}"))
                    {
                        skipMethod = false;
                        continue;
                    }
                    
                    continue;
                }
                
                newLines.Add(line);
            }
            
            File.WriteAllLines(path, newLines);
            AssetDatabase.Refresh();
            
            return removedCount;
        }
        
        private void AddEventHandlerToOriginalClass(AutoUIBinderBase target, string componentName, string eventName, string methodName, System.Type parameterType)
        {
            var script = MonoScript.FromMonoBehaviour(target as MonoBehaviour);
            var path = AssetDatabase.GetAssetPath(script);
            
            var lines = File.ReadAllLines(path);
            var insertIndex = lines.Length - 1;
            
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                if (lines[i].Trim() == "}")
                {
                    insertIndex = i;
                    break;
                }
            }

            var newMethod = new StringBuilder();
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
            newMethod.AppendLine("        ");
            newMethod.Append("    }");

            var newLines = lines.ToList();
            newLines.Insert(insertIndex, newMethod.ToString());
            
            File.WriteAllLines(path, newLines);
            AssetDatabase.Refresh();
            Debug.Log($"[AutoUIBinder] 已在原始类文件中生成事件方法: {methodName}");
        }
        
        /// <summary>
        /// 确保目标类是partial类
        /// </summary>
        private void EnsurePartialClass(AutoUIBinderBase target)
        {
            var script = MonoScript.FromMonoBehaviour(target as MonoBehaviour);
            var scriptPath = AssetDatabase.GetAssetPath(script);
            string content = File.ReadAllText(scriptPath);
            string className = target.GetType().Name;
            
            // 检查是否已经是partial类
            if (!content.Contains($"partial class {className}"))
            {
                // 将class修改为partial class
                content = content.Replace($"class {className}", $"partial class {className}");
                File.WriteAllText(scriptPath, content);
                AssetDatabase.Refresh();
                Debug.Log($"[AutoUIBinder] 已将 {className} 修改为 partial 类");
            }
        }
        
        /// <summary>
        /// 生成UI代码文件
        /// </summary>
        private void GenerateUICodeFile(AutoUIBinderBase target, string filePath)
        {
            string className = target.GetType().Name;
            var script = MonoScript.FromMonoBehaviour(target as MonoBehaviour);
            string scriptPath = AssetDatabase.GetAssetPath(script);
            
            var codeBuilder = new StringBuilder();
            
            // 生成文件头注释
            codeBuilder.AppendLine("//------------------------------------------------------------------------------");
            codeBuilder.AppendLine("// <auto-generated>");
            codeBuilder.AppendLine("//     此代码由工具自动生成。");
            codeBuilder.AppendLine($"//     运行时版本:{Application.unityVersion}");
            codeBuilder.AppendLine($"//     生成时间: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            codeBuilder.AppendLine($"//     组件数量: {target.ComponentRefs.Count}");
            codeBuilder.AppendLine($"//     预制体路径: ");
            codeBuilder.AppendLine($"//     脚本路径: {scriptPath}");
            codeBuilder.AppendLine($"//     生成路径: {filePath}");
            codeBuilder.AppendLine("//");
            codeBuilder.AppendLine("//     对此文件的更改可能会导致不正确的行为，并且如果");
            codeBuilder.AppendLine("//     重新生成代码，这些更改将会丢失。");
            codeBuilder.AppendLine("// </auto-generated>");
            codeBuilder.AppendLine("//------------------------------------------------------------------------------");
            codeBuilder.AppendLine();
            
            // 添加using语句
            codeBuilder.AppendLine("using UnityEngine;");
            codeBuilder.AppendLine("using UnityEngine.UI;");
            codeBuilder.AppendLine();
            
            // 生成partial类定义
            codeBuilder.AppendLine($"public partial class {className}");
            codeBuilder.AppendLine("{");
            
            // 为每个组件生成属性
            foreach (var kvp in target.ComponentRefs)
            {
                if (kvp.Value != null)
                {
                    string componentTypeName = kvp.Value.GetType().Name;
                    string propertyName = $"{kvp.Key}_{componentTypeName}";
                    
                    codeBuilder.AppendLine($"    /// <summary>");
                    codeBuilder.AppendLine($"    /// 获取{componentTypeName}组件: {propertyName}");
                    codeBuilder.AppendLine($"    /// </summary>");
                    codeBuilder.AppendLine($"    public {componentTypeName} {propertyName}");
                    codeBuilder.AppendLine($"    {{");
                    codeBuilder.AppendLine($"        get");
                    codeBuilder.AppendLine($"        {{");
                    codeBuilder.AppendLine($"            return this.GetComponentRef<{componentTypeName}>(\"{kvp.Key}\");");
                    codeBuilder.AppendLine($"        }}");
                    codeBuilder.AppendLine($"    }}");
                    codeBuilder.AppendLine();
                }
            }
            
            codeBuilder.AppendLine("}");
            
            // 写入文件
            File.WriteAllText(filePath, codeBuilder.ToString(), System.Text.Encoding.UTF8);
        }

        private string GetFriendlyTypeName(System.Type type)
        {
            using (var provider = new Microsoft.CSharp.CSharpCodeProvider())
            {
                var typeReference = new System.CodeDom.CodeTypeReference(type);
                string typeName = provider.GetTypeOutput(typeReference);
                
                int lastDot = typeName.LastIndexOf('.');
                if (lastDot >= 0)
                {
                    typeName = typeName.Substring(lastDot + 1);
                }
                
                return typeName;
            }
        }
    }
}