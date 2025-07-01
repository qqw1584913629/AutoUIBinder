# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

UITool是一个Unity编辑器工具，旨在简化UI开发流程，主要功能包括：
- 在Hierarchy窗口中为UI组件显示图标和高亮状态
- 通过可视化方式绑定UI组件引用
- 自动生成UI代码以减少手动编写重复代码

## Unity版本和依赖

- Unity编辑器版本：2021.3.39f1c1
- 主要使用Unity内置模块（UGUI, TextMeshPro等）
- 包含自定义Cursor IDE插件支持

## 核心架构

### 主要组件类
1. **ShowComponentIconsBase** - 抽象基类，所有需要UI绑定功能的组件都应继承此类
2. **HierarchyComponentIcons** - Unity编辑器扩展，负责在Hierarchy窗口显示组件图标和处理交互
3. **ShowComponentIconsEditor** - 自定义Inspector编辑器，提供UI代码生成功能
4. **SerializableDictionary** - 可序列化字典实现，用于存储组件引用

### 代码生成系统
- 配置路径通过UIPathConfig ScriptableObject管理
- 生成的代码存放在`{配置路径}/Gen/{类名}/`目录下
- 使用partial class模式，生成的代码文件命名为`{类名}Gen.cs`

### 编辑器功能
- 只在预制体编辑模式下激活图标显示功能
- 支持点击组件图标来添加/移除组件引用
- 不同类型的ShowComponentIconsBase会用不同颜色高亮显示
- 自动处理组件引用的层级关系和冲突检测

## 开发工作流

### 创建新UI组件
1. 创建继承自ShowComponentIconsBase的新类
2. 在预制体编辑模式下，点击需要绑定的组件图标
3. 在Inspector中点击"🚀 生成 UI 代码"按钮自动生成组件引用代码

### 配置生成路径
1. 在Resources文件夹中找到GlobalConfig资源
2. 通过UIPathConfig的自定义编辑器选择目标文件夹

### 构建和测试
- 使用Unity标准构建流程
- 主要在Unity编辑器中开发和测试
- 生成的代码会自动被Unity编译系统识别
- 使用菜单"UITool/运行编辑器测试"来验证功能

### 新增功能（v2.0优化版）
- **性能优化**: 按需重绘机制，避免不必要的UI刷新
- **智能错误处理**: 完善的文件操作和用户输入验证
- **友好UI界面**: 带图标的按钮、状态指示器和使用指南
- **一键验证**: 检测并清理无效的组件引用
- **编辑器测试**: 完整的测试套件确保功能稳定性

## 文件结构重点

### Assets/UITool/
- `UIPathConfig.cs` - 路径配置ScriptableObject

### Assets/Editor/
- `HierarchyComponentIcons.cs` - 主要编辑器扩展逻辑  
- `ShowComponentIconsEditor.cs` - 自定义Inspector
- `SerializableDictionaryDrawer.cs` - 字典的自定义PropertyDrawer

### Assets/Scripts/
- `ShowComponentIconsBase.cs` - 核心抽象基类
- `SerializableDictionary.cs` - 可序列化字典实现
- `DictionaryDisplayNameAttribute.cs` - 字典显示名称特性

### Assets/Resources/
- `GlobalConfig.asset` - 全局配置资源文件

## 代码约定

- 使用中文注释和UI文本
- 生成的组件属性使用`{节点名}_{组件类型}`命名格式
- 所有UI相关类都应继承ShowComponentIconsBase
- 编辑器代码需要用`#if UNITY_EDITOR`条件编译指令包围

## 注意事项

- 该工具主要用于预制体编辑，不支持场景中的GameObject
- 组件引用存储在SerializableDictionary中，确保Unity序列化兼容性
- 编辑器扩展会监听多个Unity事件来保持状态同步
- 代码生成会覆盖现有文件，建议使用版本控制系统