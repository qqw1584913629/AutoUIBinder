# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

AutoUIBinder是一个Unity编辑器工具，旨在简化UI开发流程，主要功能包括：
- 在Hierarchy窗口中为UI组件显示图标和高亮状态
- 通过可视化方式绑定UI组件引用
- 自动生成UI代码以减少手动编写重复代码
- UI事件绑定系统，自动连接UI事件到代码方法

## Unity版本和依赖

- Unity编辑器版本：2021.3.39f1c1或更高版本
- 主要使用Unity内置模块（UGUI, TextMeshPro等）
- 包含自定义Cursor IDE插件支持

## 核心架构

### 主要组件类
1. **AutoUIBinderBase** - 抽象基类，所有需要UI绑定功能的组件都应继承此类
2. **HierarchyComponentIcons** - Unity编辑器扩展，负责在Hierarchy窗口显示组件图标和处理交互
3. **AutoUIBinderBaseEditor** - 自定义Inspector编辑器，提供UI代码生成功能
4. **SerializableDictionary** - 可序列化字典实现，用于存储组件引用
5. **UIEventBinder** - UI事件绑定工具，自动连接UI事件到标记方法
6. **UIEventBinderWindow** - 可视化事件绑定窗口

### 代码生成系统
- 配置路径通过UIPathConfig ScriptableObject管理
- 生成的代码存放在`{配置路径}/Gen/{类名}/`目录下
- 使用partial class模式，生成的代码文件命名为`{类名}Gen.cs`
- 自动将目标类修改为partial类
- 生成的代码包含详细的XML文档注释

### 事件绑定系统（新增）
- 使用`UIEventAttribute`标记需要绑定的方法
- 支持Button点击事件、Toggle值改变事件等
- 自动匹配UI组件名称与方法名
- 在生成代码时自动添加事件绑定逻辑

### 编辑器功能
- 只在预制体编辑模式下激活图标显示功能
- 支持点击组件图标来添加/移除组件引用
- 不同类型的AutoUIBinderBase会用不同颜色高亮显示
- 自动处理组件引用的层级关系和冲突检测
- 智能检测无效组件引用并提供清理功能

## 开发工作流

### 创建新UI组件
1. 创建继承自AutoUIBinderBase的新类
2. 在预制体编辑模式下，点击需要绑定的组件图标
3. 使用`UIEventAttribute`标记事件处理方法
4. 在Inspector中点击"🚀 生成 UI 代码"按钮自动生成组件引用代码

### 事件绑定工作流（新增）
```csharp
public class MyUIPanel : AutoUIBinderBase
{
    [UIEvent]
    private void OnStartButtonClick()
    {
        // 自动绑定到名为"Button_Start"的按钮点击事件
    }
    
    [UIEvent]
    private void OnSettingsToggleChanged(bool value)
    {
        // 自动绑定到名为"Toggle_Settings"的开关值改变事件
    }
}
```

### 配置生成路径
1. 在Resources文件夹中找到GlobalConfig资源
2. 通过UIPathConfig的自定义编辑器选择目标文件夹
3. 确保目标路径具有写入权限

### 构建和测试
- 使用Unity标准构建流程
- 主要在Unity编辑器中开发和测试
- 生成的代码会自动被Unity编译系统识别
- 使用菜单"AutoUIBinder/运行编辑器测试"来验证功能

## 项目结构

```
Assets/
├── AutoUIBinder/                    # 工具核心模块
│   ├── Core/                        # 核心功能
│   │   ├── Runtime/                 # 运行时代码
│   │   │   ├── Attributes/          # 特性定义
│   │   │   │   ├── DictionaryDisplayNameAttribute.cs
│   │   │   │   └── UIEventAttribute.cs
│   │   │   ├── Base/                # 基础类
│   │   │   │   └── AutoUIBinderBase.cs
│   │   │   └── Utils/               # 工具类
│   │   │       ├── SerializableDictionary.cs
│   │   │       └── UIEventBinder.cs
│   │   └── Editor/                  # 编辑器代码
│   │       ├── Config/              # 配置
│   │       │   └── UIPathConfig.cs
│   │       ├── Core/                # 核心编辑器功能
│   │       │   ├── AutoUIBinderBaseEditor.cs
│   │       │   ├── HierarchyComponentIcons.cs
│   │       │   └── UIEventBinderWindow.cs
│   │       └── Drawers/             # 自定义绘制器
│   │           └── SerializableDictionaryDrawer.cs
│   └── Examples/                    # 示例代码
│       └── Runtime/
│           ├── Prefabs/
│           │   └── TestUIPanel.prefab
│           └── Scripts/
│               └── ExampleUIPanel.cs
├── Scripts/                         # 项目脚本
│   └── Gen/                        # 生成的代码
│       └── ExampleUIPanel/
│           └── ExampleUIPanelGen.cs
└── Resources/                       # 资源文件
    └── GlobalConfig.asset          # 全局配置
```

## 代码约定

- 使用中文注释和UI文本
- 生成的组件属性使用`{节点名}_{组件类型}`命名格式
- 所有UI相关类都应继承AutoUIBinderBase
- 编辑器代码需要用`#if UNITY_EDITOR`条件编译指令包围
- 事件处理方法命名约定：`On{组件名}{事件类型}`
- 所有日志输出使用`[AutoUIBinder]`前缀

## 重要提醒

### 重构完成
项目已完成从UITool到AutoUIBinder的重命名重构：
- 所有类名、命名空间已统一使用AutoUIBinder
- 日志输出统一使用`[AutoUIBinder]`前缀
- 菜单项路径已更新为AutoUIBinder
- 代码注释和字符串已统一更新

### 测试和验证
- 使用菜单"AutoUIBinder/验证组件绑定"检查绑定状态
- 生成代码后务必测试所有组件引用是否正确
- 确保事件绑定方法的参数类型与UI组件事件匹配

## 注意事项

- 该工具主要用于预制体编辑，不支持场景中的GameObject
- 组件引用存储在SerializableDictionary中，确保Unity序列化兼容性
- 编辑器扩展会监听多个Unity事件来保持状态同步
- 代码生成会覆盖现有生成文件，建议使用版本控制系统
- 事件绑定要求方法名与UI组件名称匹配特定模式