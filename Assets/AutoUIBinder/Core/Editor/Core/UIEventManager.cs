using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoUIBinder;

namespace AutoUIBinder.Editor
{
    /// <summary>
    /// UI事件管理器 - 负责事件绑定相关的逻辑
    /// </summary>
    public class UIEventManager
    {
        public class EventBinding
        {
            public string ComponentName;
            public string ComponentType;
            public string EventName;
            public string MethodName;
            public System.Type ParameterType;
            public bool IsSelected;
            public bool AlreadyBound;
            public bool AlreadyExists;
            public Component Component;
            
            public string DisplayName => GetCleanEventName(EventName);
            public string ParameterText => ParameterType != null ? GetFriendlyTypeName(ParameterType) : "无参数";
            public string StatusText => AlreadyBound ? "已绑定" : (AlreadyExists ? "方法存在" : "待生成");
            public Color StatusColor => AlreadyBound ? Color.green : (AlreadyExists ? Color.yellow : Color.gray);
            
            private static string GetCleanEventName(string eventName)
            {
                if (eventName.StartsWith("m_On"))
                    return eventName.Substring(4);
                if (eventName.StartsWith("on"))
                    return eventName.Substring(2);
                return eventName;
            }
            
            private static string GetFriendlyTypeName(System.Type type)
            {
                using (var provider = new Microsoft.CSharp.CSharpCodeProvider())
                {
                    var typeReference = new System.CodeDom.CodeTypeReference(type);
                    string typeName = provider.GetTypeOutput(typeReference);
                    int lastDot = typeName.LastIndexOf('.');
                    if (lastDot >= 0)
                        typeName = typeName.Substring(lastDot + 1);
                    return typeName;
                }
            }
        }
        
        private class EventInfo
        {
            public string Name;
            public System.Type EventType;
            public System.Type ParameterType;
        }
        
        private List<EventBinding> eventBindings = new List<EventBinding>();
        private int lastComponentCount = -1;
        private string lastComponentHash = "";
        
        public List<EventBinding> EventBindings => eventBindings;
        
        /// <summary>
        /// 刷新事件绑定列表
        /// </summary>
        public void RefreshEventBindings(AutoUIBinderBase target)
        {
            var previousSelections = new Dictionary<string, bool>();
            foreach (var evt in eventBindings)
            {
                string key = $"{evt.ComponentName}.{evt.EventName}";
                previousSelections[key] = evt.IsSelected;
            }
            
            eventBindings.Clear();
            
            foreach (var pair in target.ComponentRefs)
            {
                if (pair.Value == null) continue;
                
                var events = GetAvailableEvents(pair.Value);
                
                foreach (var eventInfo in events)
                {
                    string methodName = GetMethodName(pair.Key, eventInfo.Name);
                    bool isAlreadyBound = IsEventBound(target, pair.Key, eventInfo.Name);
                    bool isMethodExists = DoesMethodExist(target, methodName);
                    
                    string key = $"{pair.Key}.{eventInfo.Name}";
                    bool isSelected = previousSelections.ContainsKey(key) 
                        ? previousSelections[key] 
                        : (isAlreadyBound || isMethodExists);
                    
                    eventBindings.Add(new EventBinding
                    {
                        ComponentName = pair.Key,
                        ComponentType = pair.Value.GetType().Name,
                        EventName = eventInfo.Name,
                        MethodName = methodName,
                        ParameterType = eventInfo.ParameterType,
                        IsSelected = isSelected,
                        AlreadyBound = isAlreadyBound,
                        AlreadyExists = isMethodExists,
                        Component = pair.Value
                    });
                }
            }
        }
        
        /// <summary>
        /// 检查是否需要刷新事件绑定
        /// </summary>
        public bool NeedsRefresh(AutoUIBinderBase target)
        {
            int currentComponentCount = target.ComponentRefs.Count;
            string currentComponentHash = GetComponentHash(target);
            
            return eventBindings.Count == 0 || 
                   lastComponentCount != currentComponentCount || 
                   lastComponentHash != currentComponentHash;
        }
        
        /// <summary>
        /// 更新缓存状态
        /// </summary>
        public void UpdateCacheState(AutoUIBinderBase target)
        {
            lastComponentCount = target.ComponentRefs.Count;
            lastComponentHash = GetComponentHash(target);
        }
        
        /// <summary>
        /// 全选事件
        /// </summary>
        public void SelectAllEvents()
        {
            foreach (var evt in eventBindings)
                evt.IsSelected = true;
        }
        
        /// <summary>
        /// 全不选事件
        /// </summary>
        public void DeselectAllEvents()
        {
            foreach (var evt in eventBindings)
                evt.IsSelected = false;
        }
        
        /// <summary>
        /// 智能选择事件
        /// </summary>
        public void SmartSelectEvents()
        {
            var commonEvents = new[] { "onClick", "onValueChanged", "onEndEdit", "onSubmit" };
            
            foreach (var evt in eventBindings)
            {
                evt.IsSelected = !evt.AlreadyBound && !evt.AlreadyExists && 
                               commonEvents.Any(common => evt.EventName.ToLower().Contains(common.ToLower()));
            }
        }
        
        /// <summary>
        /// 获取选中的事件数量
        /// </summary>
        public int GetSelectedCount()
        {
            return eventBindings.Count(e => e.IsSelected);
        }
        
        /// <summary>
        /// 获取已绑定的事件数量
        /// </summary>
        public int GetBoundCount()
        {
            return eventBindings.Count(e => e.AlreadyBound);
        }
        
        private EventInfo[] GetAvailableEvents(Component component)
        {
            var events = new List<EventInfo>();
            var type = component.GetType();

            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => typeof(UnityEngine.Events.UnityEventBase).IsAssignableFrom(f.FieldType));

            foreach (var field in fields)
            {
                var eventType = field.FieldType;
                System.Type parameterType = null;

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

                bool isSerializable = field.IsPublic || field.GetCustomAttribute<SerializeField>() != null;
                
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

            return events.OrderBy(e => e.Name).ToArray();
        }
        
        private bool IsEventBound(AutoUIBinderBase target, string componentName, string eventName)
        {
            var methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<UIEventAttribute>();
                if (attr != null && attr.ComponentName == componentName && attr.EventType == eventName)
                {
                    return true;
                }
            }
            return false;
        }
        
        private bool DoesMethodExist(AutoUIBinderBase target, string methodName)
        {
            var methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return methods.Any(m => m.Name == methodName && m.GetCustomAttribute<UIEventAttribute>() != null);
        }
        
        private string GetMethodName(string componentName, string eventName)
        {
            string cleanEventName = eventName.StartsWith("m_On") ? eventName.Substring(4) : eventName;
            return $"On{char.ToUpper(componentName[0])}{componentName.Substring(1)}{cleanEventName}";
        }
        
        private string GetComponentHash(AutoUIBinderBase target)
        {
            var sb = new System.Text.StringBuilder();
            var sortedKeys = target.ComponentRefs.Keys.OrderBy(k => k).ToList();
            
            foreach (var key in sortedKeys)
            {
                var component = target.ComponentRefs[key];
                if (component != null)
                {
                    sb.Append($"{key}:{component.GetType().Name}:{component.GetInstanceID()};");
                }
                else
                {
                    sb.Append($"{key}:null;");
                }
            }
            
            return sb.ToString();
        }
    }
}