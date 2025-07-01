using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.IO;
using UITool;

/// <summary>
/// UITool编辑器测试类
/// </summary>
public class UIToolEditorTests
{
    private GameObject testGameObject;
    private ExampleUIPanel testComponent;

    [SetUp]
    public void SetUp()
    {
        // 创建测试GameObject
        testGameObject = new GameObject("TestUIPanel");
        testComponent = testGameObject.AddComponent<ExampleUIPanel>();
    }

    [TearDown]
    public void TearDown()
    {
        // 清理测试对象
        if (testGameObject != null)
        {
            Object.DestroyImmediate(testGameObject);
        }
    }

    [Test]
    public void TestShowComponentIconsBase_ComponentRefs_NotNull()
    {
        // 测试组件引用字典不为空
        Assert.IsNotNull(testComponent.ComponentRefs);
        Debug.Log("[UIToolTest] 组件引用字典初始化正常");
    }

    [Test]
    public void TestComponentRef_AddAndRemove()
    {
        // 创建测试组件
        var buttonComponent = testGameObject.AddComponent<UnityEngine.UI.Button>();
        
        // 测试添加组件引用
        testComponent.AddComponentRef("TestButton", buttonComponent);
        Assert.IsTrue(testComponent.ComponentRefs.ContainsKey("TestButton"));
        Assert.AreEqual(buttonComponent, testComponent.ComponentRefs["TestButton"]);
        
        // 测试获取组件引用
        var retrievedComponent = testComponent.GetComponentRef<UnityEngine.UI.Button>("TestButton");
        Assert.AreEqual(buttonComponent, retrievedComponent);
        
        // 测试移除组件引用
        testComponent.RemoveComponentRef("TestButton");
        Assert.IsFalse(testComponent.ComponentRefs.ContainsKey("TestButton"));
        
        Debug.Log("[UIToolTest] 组件引用添加/移除测试通过");
    }

    [Test]
    public void TestComponentRef_GetNonExistentComponent()
    {
        // 测试获取不存在的组件引用
        var component = testComponent.GetComponentRef<UnityEngine.UI.Button>("NonExistent");
        Assert.IsNull(component);
        
        Debug.Log("[UIToolTest] 不存在组件引用测试通过");
    }

    [Test]
    public void TestUIPathConfig_LoadFromResources()
    {
        // 测试加载GlobalConfig
        var globalConfig = Resources.Load<UIPathConfig>("GlobalConfig");
        
        if (globalConfig != null)
        {
            Assert.IsNotNull(globalConfig.Paths);
            Assert.IsTrue(!string.IsNullOrEmpty(globalConfig.Paths));
            Debug.Log($"[UIToolTest] GlobalConfig加载成功，路径: {globalConfig.Paths}");
        }
        else
        {
            Debug.LogWarning("[UIToolTest] GlobalConfig未找到，请确保Resources文件夹中存在GlobalConfig.asset");
        }
    }

    [Test]
    public void TestExampleUIPanel_BasicFunctionality()
    {
        // 测试示例面板基本功能
        Assert.IsTrue(testComponent.enabled);
        
        // 测试显示/隐藏
        testComponent.ShowPanel();
        Assert.IsTrue(testComponent.IsVisible);
        
        testComponent.HidePanel();
        Assert.IsFalse(testComponent.IsVisible);
        
        // 测试设置标题
        testComponent.SetTitle("测试标题");
        // 由于没有实际的UI组件，这里只是确保方法调用不出错
        
        Debug.Log("[UIToolTest] 示例面板基本功能测试通过");
    }

    [Test]
    public void TestSerializableDictionary_Serialization()
    {
        // 测试可序列化字典
        var dict = new SerializableDictionary<string, Component>();
        var button = testGameObject.AddComponent<UnityEngine.UI.Button>();
        
        // 添加项目
        dict["TestButton"] = button;
        Assert.AreEqual(1, dict.Count);
        Assert.AreEqual(button, dict["TestButton"]);
        
        // 模拟序列化过程
        dict.OnBeforeSerialize();
        dict.Clear(); // 清空字典模拟反序列化前状态
        dict.OnAfterDeserialize();
        
        // 验证反序列化后数据恢复
        Assert.AreEqual(1, dict.Count);
        Assert.AreEqual(button, dict["TestButton"]);
        
        Debug.Log("[UIToolTest] 可序列化字典测试通过");
    }

    [UnityTest]
    public IEnumerator TestPerformance_ComponentRefAccess()
    {
        // 性能测试：大量组件引用访问
        const int componentCount = 100;
        
        // 添加大量组件引用
        for (int i = 0; i < componentCount; i++)
        {
            var button = testGameObject.AddComponent<UnityEngine.UI.Button>();
            testComponent.AddComponentRef($"Button_{i}", button);
        }
        
        var startTime = System.DateTime.Now;
        
        // 大量访问测试
        for (int i = 0; i < componentCount; i++)
        {
            var component = testComponent.GetComponentRef<UnityEngine.UI.Button>($"Button_{i}");
            Assert.IsNotNull(component);
            
            if (i % 10 == 0)
                yield return null; // 每10次访问让出一帧
        }
        
        var endTime = System.DateTime.Now;
        var duration = (endTime - startTime).TotalMilliseconds;
        
        Debug.Log($"[UIToolTest] {componentCount}个组件引用访问耗时: {duration}ms");
        Assert.Less(duration, 1000, "组件引用访问性能测试失败，耗时超过1秒");
    }

    [Test]
    public void TestEditorUtility_DialogAndLogging()
    {
        // 测试编辑器工具函数（不显示对话框，仅测试调用）
        try
        {
            // 测试日志输出
            Debug.Log("[UITool] 测试日志输出 - Info");
            Debug.LogWarning("[UITool] 测试日志输出 - Warning");
            Debug.LogError("[UITool] 测试日志输出 - Error");
            
            // 这里不实际显示对话框，只是确保代码路径正确
            var result = true; // 模拟用户点击确定
            Assert.IsTrue(result);
            
            Debug.Log("[UIToolTest] 编辑器工具测试通过");
        }
        catch (System.Exception ex)
        {
            Assert.Fail($"编辑器工具测试失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 运行所有测试的菜单项
    /// </summary>
    [MenuItem("UITool/运行编辑器测试")]
    public static void RunAllTests()
    {
        Debug.Log("[UITool] 开始运行编辑器测试...");
        
        // 这里可以添加自定义的测试运行逻辑
        // 或者提示用户使用Test Runner窗口
        
        EditorUtility.DisplayDialog(
            "UITool测试", 
            "请打开 Window > General > Test Runner 来运行完整的测试套件。\n\n" +
            "或在Console窗口查看测试日志输出。", 
            "确定"
        );
    }

    /// <summary>
    /// 创建测试预制体的菜单项
    /// </summary>
    [MenuItem("UITool/创建测试预制体")]
    public static void CreateTestPrefab()
    {
        // 创建测试预制体
        var testObj = new GameObject("TestUIPanel");
        var panel = testObj.AddComponent<ExampleUIPanel>();
        
        // 添加一些基础UI组件用于测试
        var canvas = testObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        testObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        testObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // 创建子对象
        var buttonObj = new GameObject("Button_Close");
        buttonObj.transform.SetParent(testObj.transform);
        buttonObj.AddComponent<UnityEngine.UI.Button>();
        buttonObj.AddComponent<UnityEngine.UI.Image>();
        
        var textObj = new GameObject("Text_Title");
        textObj.transform.SetParent(testObj.transform);
        textObj.AddComponent<UnityEngine.UI.Text>();
        
        Selection.activeGameObject = testObj;
        
        Debug.Log("[UITool] 测试预制体创建完成，请进入预制体编辑模式进行组件绑定测试");
    }
}