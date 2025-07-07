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

            // 查找匹配的事件字段 - 使用智能匹配
            FieldInfo matchingField = FindEventField(component, attr.EventType, methodParamTypes);

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

        /// <summary>
        /// 智能查找事件字段 - 通过多种匹配策略查找
        /// </summary>
        private static FieldInfo FindEventField(Component component, string eventName, Type[] expectedParamTypes)
        {
            var componentType = component.GetType();
            var eventFields = ReflectionCache.GetUnityEventFields(componentType);
            
            Debug.Log($"[AutoUIBinder] 查找事件 '{eventName}'，在 {componentType.Name} 组件中");
            
            // 策略1: 尝试通过属性名直接查找对应的私有字段
            var possibleFieldNames = GeneratePossibleFieldNames(eventName);
            foreach (var fieldName in possibleFieldNames)
            {
                var field = eventFields.FirstOrDefault(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
                if (field != null && IsParameterCompatible(field, expectedParamTypes))
                {
                    Debug.Log($"[AutoUIBinder] 通过字段名匹配找到: {field.Name}");
                    return field;
                }
            }
            
            // 策略2: 通过反射查找公开属性，然后找到背后的字段
            var property = componentType.GetProperty(eventName, BindingFlags.Public | BindingFlags.Instance);
            if (property != null && typeof(UnityEventBase).IsAssignableFrom(property.PropertyType))
            {
                // 通过属性getter方法的IL代码分析找到背后的字段（复杂但准确）
                var backingField = FindBackingFieldForProperty(componentType, property);
                if (backingField != null && IsParameterCompatible(backingField, expectedParamTypes))
                {
                    Debug.Log($"[AutoUIBinder] 通过属性反射找到背后字段: {backingField.Name}");
                    return backingField;
                }
            }
            
            // 策略3: 模糊匹配 - 比较事件名称的相似度
            foreach (var field in eventFields)
            {
                if (IsEventNameSimilar(field.Name, eventName) && IsParameterCompatible(field, expectedParamTypes))
                {
                    Debug.Log($"[AutoUIBinder] 通过模糊匹配找到: {field.Name}");
                    return field;
                }
            }
            
            Debug.LogWarning($"[AutoUIBinder] 无法找到匹配的事件字段，尝试过的字段名: {string.Join(", ", possibleFieldNames)}");
            return null;
        }
        
        /// <summary>
        /// 根据公开事件名生成可能的私有字段名
        /// </summary>
        private static string[] GeneratePossibleFieldNames(string eventName)
        {
            var possibilities = new List<string>();
            
            // 直接匹配
            possibilities.Add(eventName);
            
            // Unity常见模式: onClick -> m_OnClick
            if (eventName.StartsWith("on") && eventName.Length > 2)
            {
                string withoutOn = eventName.Substring(2);
                possibilities.Add($"m_On{withoutOn}");
                possibilities.Add($"_on{withoutOn}");
                possibilities.Add($"on{withoutOn}");
            }
            
            // 其他可能的命名模式
            possibilities.Add($"m_{eventName}");
            possibilities.Add($"_{eventName}");
            possibilities.Add($"m_{char.ToUpper(eventName[0])}{eventName.Substring(1)}");
            
            return possibilities.ToArray();
        }
        
        /// <summary>
        /// 通过属性查找背后的私有字段
        /// </summary>
        private static FieldInfo FindBackingFieldForProperty(Type type, PropertyInfo property)
        {
            // 简化版本：通过命名约定查找
            var possibleNames = new[]
            {
                $"m_{property.Name}",
                $"_{property.Name}",
                $"m_On{property.Name.Substring(2)}", // onClick -> m_OnClick
                $"<{property.Name}>k__BackingField" // 自动属性的backing field
            };
            
            foreach (var name in possibleNames)
            {
                var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && field.FieldType == property.PropertyType)
                {
                    return field;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 检查事件名称是否相似
        /// </summary>
        private static bool IsEventNameSimilar(string fieldName, string eventName)
        {
            // 去除前缀后比较
            var cleanFieldName = fieldName;
            if (fieldName.StartsWith("m_"))
                cleanFieldName = fieldName.Substring(2);
            if (fieldName.StartsWith("_"))
                cleanFieldName = fieldName.Substring(1);
                
            return cleanFieldName.Equals(eventName, StringComparison.OrdinalIgnoreCase) ||
                   cleanFieldName.EndsWith(eventName, StringComparison.OrdinalIgnoreCase) ||
                   eventName.EndsWith(cleanFieldName, StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// 检查字段的参数类型是否兼容
        /// </summary>
        private static bool IsParameterCompatible(FieldInfo field, Type[] expectedParamTypes)
        {
            var fieldType = field.FieldType;
            var invokeMethod = fieldType.GetMethod("Invoke");
            if (invokeMethod == null) return false;
            
            var fieldParams = invokeMethod.GetParameters();
            
            if (expectedParamTypes.Length != fieldParams.Length)
                return false;
                
            for (int i = 0; i < expectedParamTypes.Length; i++)
            {
                if (!fieldParams[i].ParameterType.IsAssignableFrom(expectedParamTypes[i]))
                    return false;
            }
            
            return true;
        }
    }
}