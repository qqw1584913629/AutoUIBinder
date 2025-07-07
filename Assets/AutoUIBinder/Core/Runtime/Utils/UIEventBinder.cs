using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System;
using System.Linq;
using UnityEngine.Events;
using System.Collections.Generic;

namespace AutoUIBinder
{
    /// <summary>
    /// UI事件绑定器 - 使用反射缓存优化性能
    /// </summary>
    public static class UIEventBinder
    {
        public static void BindEvents(AutoUIBinderBase target)
        {
            if (target == null) return;

            // 使用缓存的事件方法
            var eventMethods = ReflectionCache.GetEventMethods(target.GetType());
            
            foreach (var method in eventMethods)
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
                Debug.LogError($"[AutoUIBinder] 找不到组件: {attr.ComponentName}");
                return;
            }

            Debug.Log($"[AutoUIBinder] 正在绑定组件 {attr.ComponentName} ({component.GetType().Name})");

            // 使用缓存获取方法参数类型
            var methodParamTypes = ReflectionCache.GetMethodParameterTypes(method);
            Type expectedEventType;
            Type expectedParamType = null;
            
            if (methodParamTypes.Length == 0)
            {
                expectedEventType = typeof(UnityEvent);
                Debug.Log($"[AutoUIBinder] 期望的事件类型: UnityEvent (无参数)");
            }
            else if (methodParamTypes.Length == 1)
            {
                expectedParamType = methodParamTypes[0];
                expectedEventType = typeof(UnityEvent<>).MakeGenericType(expectedParamType);
                Debug.Log($"[AutoUIBinder] 期望的事件类型: UnityEvent<{ReflectionCache.GetFriendlyTypeName(expectedParamType)}>");
            }
            else
            {
                Debug.LogError($"[AutoUIBinder] 方法 {method.Name} 的参数数量不正确，UnityEvent只支持0个或1个参数");
                return;
            }

            // 使用缓存获取组件上的UnityEvent字段
            var eventFields = ReflectionCache.GetUnityEventFields(component.GetType());

            Debug.Log($"[AutoUIBinder] 在组件上找到 {eventFields.Length} 个UnityEvent字段:");
            foreach (var field in eventFields)
            {
                var fieldType = field.FieldType;
                var fieldParams = fieldType.GetMethod("Invoke")?.GetParameters();
                string paramInfo = fieldParams != null && fieldParams.Length > 0 
                    ? $"<{string.Join(", ", fieldParams.Select(p => ReflectionCache.GetFriendlyTypeName(p.ParameterType)))}>"
                    : "(无参数)";
                Debug.Log($"[AutoUIBinder] - {field.Name}: {fieldType.Name} {paramInfo}");
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
                
                Debug.Log($"[AutoUIBinder] 比较字段 {field.Name}:");
                Debug.Log($"[AutoUIBinder]   字段类型: {fieldType.Name}");
                string paramInfo = fieldParams != null && fieldParams.Length > 0 
                    ? $"<{string.Join(", ", fieldParams.Select(p => ReflectionCache.GetFriendlyTypeName(p.ParameterType)))}>"
                    : "(无参数)";
                Debug.Log($"[AutoUIBinder]   字段参数: {paramInfo}");
                Debug.Log($"[AutoUIBinder]   期望参数: {(expectedParamType != null ? ReflectionCache.GetFriendlyTypeName(expectedParamType) : "(无参数)")}");

                bool paramsMatch = false;
                if (fieldParams != null && methodParamTypes.Length > 0)
                {
                    if (fieldParams.Length == methodParamTypes.Length)
                    {
                        paramsMatch = true;
                        for (int i = 0; i < fieldParams.Length; i++)
                        {
                            var fieldParamType = fieldParams[i].ParameterType;
                            var methodParamType = methodParamTypes[i];
                            
                            // 检查类型是否兼容（考虑继承关系）
                            if (!fieldParamType.IsAssignableFrom(methodParamType))
                            {
                                Debug.Log($"[AutoUIBinder]   参数类型不匹配: {ReflectionCache.GetFriendlyTypeName(fieldParamType)} != {ReflectionCache.GetFriendlyTypeName(methodParamType)}");
                                paramsMatch = false;
                                break;
                            }
                        }
                    }
                }
                else if (fieldParams == null && methodParamTypes.Length == 0)
                {
                    paramsMatch = true;
                }
                
                if (paramsMatch)
                {
                    matchingField = field;
                    Debug.Log($"[AutoUIBinder] 找到匹配的字段: {field.Name}");
                    break;
                }
                else
                {
                    Debug.Log($"[AutoUIBinder]   字段不匹配");
                }
            }

            if (matchingField == null)
            {
                Debug.LogError($"[AutoUIBinder] 在组件 {attr.ComponentName} 上找不到事件 {attr.EventType}");
                return;
            }

            // 获取事件实例并添加监听器
            var eventInstance = matchingField.GetValue(component) as UnityEventBase;
            if (eventInstance == null)
            {
                Debug.LogError($"[AutoUIBinder] 无法获取事件实例");
                return;
            }

            var addListenerMethod = eventInstance.GetType().GetMethod("AddListener");
            if (addListenerMethod != null)
            {
                try
                {
                    var methodDelegate = Delegate.CreateDelegate(addListenerMethod.GetParameters()[0].ParameterType, target, method);
                    addListenerMethod.Invoke(eventInstance, new object[] { methodDelegate });
                    Debug.Log($"[AutoUIBinder] 成功绑定事件 {matchingField.Name} 到方法 {method.Name}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AutoUIBinder] 绑定事件时发生错误: {ex.Message}");
                    Debug.LogError($"[AutoUIBinder] 详细错误: {ex}");
                }
            }
        }
    }
}