using UnityEngine;
using UnityEngine.UI;

namespace AutoUIBinder
{
    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false)]
    public class UIEventAttribute : PropertyAttribute
{
    public string ComponentName { get; private set; }
    public string EventType { get; private set; }
    
        /// <summary>
        /// 为UI组件添加事件监听
        /// </summary>
        /// <param name="componentName">组件引用名称</param>
        /// <param name="eventType">事件类型（如：onClick, onValueChanged等）</param>
        public UIEventAttribute(string componentName, string eventType)
        {
            ComponentName = componentName;
            EventType = eventType;
        }
    }
} 