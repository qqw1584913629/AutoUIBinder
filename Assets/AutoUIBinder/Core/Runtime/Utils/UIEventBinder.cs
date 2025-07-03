using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System;
using System.Linq;
using UnityEngine.Events;
using System.Collections.Generic;

public static class UIEventBinder
{
    public static void BindEvents(AutoUIBinderBase target)
    {
        var methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<UIEventAttribute>();
            if (attr != null)
            {
                BindComponentEvent(target, method, attr);
            }
        }
    }

    private static void BindComponentEvent(AutoUIBinderBase target, MethodInfo method, UIEventAttribute attr)
    {
        var component = target.GetComponentRef<Component>(attr.ComponentName);
        if (component == null)
        {
            Debug.LogError($"找不到组件: {attr.ComponentName}");
            return;
        }

        Debug.Log($"正在绑定组件 {attr.ComponentName} ({component.GetType().Name})");

        // 获取方法的参数信息
        var methodParams = method.GetParameters();
        Type expectedEventType;
        Type expectedParamType = null;
        
        if (methodParams.Length == 0)
        {
            expectedEventType = typeof(UnityEvent);
            Debug.Log($"期望的事件类型: UnityEvent (无参数)");
        }
        else if (methodParams.Length == 1)
        {
            expectedParamType = methodParams[0].ParameterType;
            expectedEventType = typeof(UnityEvent<>).MakeGenericType(expectedParamType);
            Debug.Log($"期望的事件类型: UnityEvent<{expectedParamType.Name}>");
        }
        else
        {
            Debug.LogError($"方法 {method.Name} 的参数数量不正确，UnityEvent只支持0个或1个参数");
            return;
        }

        // 获取组件上所有UnityEvent类型的字段
        var eventFields = component.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => typeof(UnityEventBase).IsAssignableFrom(f.FieldType))
            .ToList();

        Debug.Log($"在组件上找到 {eventFields.Count} 个UnityEvent字段:");
        foreach (var field in eventFields)
        {
            var fieldType = field.FieldType;
            var fieldParams = fieldType.GetMethod("Invoke")?.GetParameters();
            string paramInfo = fieldParams != null && fieldParams.Length > 0 
                ? $"<{string.Join(", ", fieldParams.Select(p => p.ParameterType.Name))}>" 
                : "(无参数)";
            Debug.Log($"- {field.Name}: {fieldType.Name} {paramInfo}");
        }

        // 查找匹配的事件字段
        FieldInfo matchingField = null;
        foreach (var field in eventFields)
        {
            // 首先检查事件名称是否匹配
            if (!field.Name.Equals(attr.EventType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var fieldType = field.FieldType;
            var fieldParams = fieldType.GetMethod("Invoke")?.GetParameters();
            
            Debug.Log($"比较字段 {field.Name}:");
            Debug.Log($"  字段类型: {fieldType.Name}");
            string paramInfo = fieldParams != null && fieldParams.Length > 0 
                ? $"<{string.Join(", ", fieldParams.Select(p => p.ParameterType.Name))}>" 
                : "(无参数)";
            Debug.Log($"  字段参数: {paramInfo}");
            Debug.Log($"  期望参数: {(expectedParamType != null ? expectedParamType.Name : "(无参数)")}");

            bool paramsMatch = false;
            if (fieldParams != null && methodParams != null)
            {
                if (fieldParams.Length == methodParams.Length)
                {
                    paramsMatch = true;
                    for (int i = 0; i < fieldParams.Length; i++)
                    {
                        var fieldParamType = fieldParams[i].ParameterType;
                        var methodParamType = methodParams[i].ParameterType;
                        
                        // 检查类型是否兼容（考虑继承关系）
                        if (!fieldParamType.IsAssignableFrom(methodParamType))
                        {
                            Debug.Log($"  参数类型不匹配: {fieldParamType.Name} != {methodParamType.Name}");
                            paramsMatch = false;
                            break;
                        }
                    }
                }
            }
            else if (fieldParams == null && methodParams.Length == 0)
            {
                paramsMatch = true;
            }
            
            if (paramsMatch)
            {
                matchingField = field;
                Debug.Log($"找到匹配的字段: {field.Name}");
                break;
            }
            else
            {
                Debug.Log($"  字段不匹配");
            }
        }

        if (matchingField == null)
        {
            Debug.LogError($"在组件 {attr.ComponentName} 上找不到事件 {attr.EventType}");
            return;
        }

        // 获取事件实例并添加监听器
        var eventInstance = matchingField.GetValue(component) as UnityEventBase;
        if (eventInstance == null)
        {
            Debug.LogError($"无法获取事件实例");
            return;
        }

        var addListenerMethod = eventInstance.GetType().GetMethod("AddListener");
        if (addListenerMethod != null)
        {
            try
            {
                var methodDelegate = Delegate.CreateDelegate(addListenerMethod.GetParameters()[0].ParameterType, target, method);
                addListenerMethod.Invoke(eventInstance, new object[] { methodDelegate });
                Debug.Log($"成功绑定事件 {matchingField.Name} 到方法 {method.Name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"绑定事件时发生错误: {ex.Message}");
                Debug.LogError($"详细错误: {ex.ToString()}");
            }
        }
    }
} 