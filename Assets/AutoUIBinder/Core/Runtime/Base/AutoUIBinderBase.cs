using UnityEngine;
using System.Collections.Generic;

namespace AutoUIBinder
{
    /// <summary>
    /// 抽象基类：继承此类的GameObject及其子对象将在Hierarchy窗口中显示组件图标
    /// </summary>
    public abstract class AutoUIBinderBase : MonoBehaviour
    {
        // 用于存储组件引用的字典
        [SerializeField]
        [DictionaryDisplayName("UI节点绑定")]
        protected SerializableDictionary<string, Component> componentRefs = new SerializableDictionary<string, Component>();

        public Dictionary<string, Component> ComponentRefs => componentRefs;

        protected virtual void Awake()
        {
            // UI组件绑定基类，子类可以重写此方法添加初始化逻辑
        }

        public void AddComponentRef(string key, Component component)
        {
            if (!componentRefs.ContainsKey(key))
            {
                componentRefs[key] = component;
            }
        }

        public void RemoveComponentRef(string key)
        {
            if (componentRefs.ContainsKey(key))
            {
                componentRefs.Remove(key);
            }
        }

        // 获取指定类型的组件
        public T GetComponentRef<T>(string key) where T : Component
        {
            if (componentRefs.TryGetValue(key, out Component component))
            {
                return component as T;
            }
            return null;
        }
    }
}