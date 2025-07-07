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