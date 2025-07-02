<h1 align="center">✨ AutoUIBinder</h1>
<p align="center">
    <a href="README.md">English</a> •
    <a href="README_CN.md">中文</a>
</p>

---

AutoUIBinder是一个强大的Unity编辑器扩展工具，旨在简化UI开发流程，提供可视化的组件绑定和自动代码生成功能。

## ✨ 主要功能

- **可视化组件绑定** - 在Hierarchy窗口中直接点击组件图标来绑定UI元素
- **自动代码生成** - 一键生成组件引用代码，减少重复编写
- **智能命名冲突处理** - 自动检测并处理重名组件
- **预制体编辑支持** - 专为预制体编辑模式优化
- **性能优化** - 高效的重绘机制，不影响编辑器性能

## 🚀 快速开始

### 安装要求

- Unity 2021.3.39f1c1 或更高版本
- 支持所有Unity内置UI系统（UGUI）

### 基本使用步骤

1. **创建UI脚本**
   ```csharp
   using UnityEngine;
   
   public class MyUIPanel : AutoUIBinderBase
   {
       void Start()
       {
           // 你的初始化代码
       }
   }
   ```

2. **绑定组件**
   - 将脚本添加到预制体的根对象
   - 进入预制体编辑模式
   - 点击需要绑定的组件图标（会高亮显示）
   - 再次点击可取消绑定

3. **生成代码**
   - 在Inspector中点击"生成UI"按钮
   - 代码将自动生成到配置的路径

4. **使用生成的代码**
   ```csharp
   void Start()
   {
       // 自动生成的属性可以直接使用
       Button_Start.onClick.AddListener(OnStartClick);
       Text_Title.text = "欢迎使用AutoUIBinder";
   }
   ```

## 📁 项目结构

```
Assets/
├── AutoUIBinder/                # 核心工具
│   ├── Core/                    # 核心实现
│   │   ├── Runtime/            # 运行时代码
│   │   │   ├── Attributes/     # 特性定义
│   │   │   ├── Base/          # 基础类
│   │   │   └── Utils/         # 工具类
│   │   └── Editor/             # 编辑器代码
│   │       ├── Config/         # 配置相关
│   │       ├── Core/          # 核心编辑器功能
│   │       └── Drawers/       # 自定义绘制器
│   └── Examples/               # 示例代码
│       ├── Runtime/            # 运行时示例
│       │   ├── Prefabs/       # 预制体示例
│       │   └── Scripts/       # 示例脚本
│       └── Scenes/            # 示例场景
├── Scripts/                    # 项目脚本
│   └── Gen/                   # 生成的代码
└── Resources/                 # 资源文件
    └── GlobalConfig.asset     # 全局配置
```

## ⚙️ 配置说明

### 全局配置

在`Resources/GlobalConfig.asset`中配置代码生成路径：

1. 在Inspector中点击"选择文件夹"
2. 选择你的脚本目录（通常是`Assets/Scripts`）
3. 生成的代码将保存在`{路径}/Gen/{类名}/`目录下

### 代码生成规则

- 生成的文件命名格式：`{类名}Gen.cs`
- 使用partial class模式，不会覆盖你的主要代码
- 组件属性命名格式：`{节点名}_{组件类型}`
- 自动添加XML文档注释

## 🎨 界面说明

### Hierarchy窗口增强

- **背景色高亮** - 不同类型的AutoUIBinderBase用不同颜色区分
- **组件图标** - 显示所有组件的图标
- **绑定状态** - 已绑定的组件会有特殊标识
- **智能交互** - 点击图标进行绑定/解绑操作

### Inspector增强

- **组件引用列表** - 以表格形式显示所有绑定的组件
- **一键生成** - 醒目的"生成UI"按钮
- **实时验证** - 自动检测无效引用

## 🔧 高级功能

### 智能命名处理

工具会自动处理以下情况：

- **重名检测** - 自动为重名组件添加数字后缀
- **非法字符** - 自动替换空格为下划线
- **关键字冲突** - 避免使用C#关键字作为变量名
- **用户确认** - 重名时会弹出确认对话框

### 性能优化特性

- **按需重绘** - 只在必要时刷新Hierarchy窗口
- **智能缓存** - 缓存颜色和状态信息
- **事件驱动** - 基于Unity事件系统，响应及时

### 错误处理

- **友好提示** - 所有错误都有详细的中文提示
- **文件备份** - 覆盖文件前自动创建备份
- **异常恢复** - 编辑器重启后自动恢复绑定状态

## 🐛 故障排除

### 常见问题

**Q: 生成代码时提示路径错误**
A: 检查GlobalConfig中的路径设置，确保目录存在且有写入权限

**Q: 预制体编辑模式下看不到图标**
A: 确保预制体根对象或其子对象有AutoUIBinderBase组件

**Q: 绑定的组件在代码中访问为null**
A: 确保预制体已保存，并且生成代码后重新编译了项目

**Q: Hierarchy窗口性能卡顿**
A: 工具已优化性能，如仍有问题，请检查是否有其他插件冲突

### 调试信息

工具提供详细的日志信息，所有日志都以`[AutoUIBinder]`前缀标识：

- **Info级别** - 正常操作记录
- **Warning级别** - 潜在问题提醒  
- **Error级别** - 错误详情和堆栈

## 📈 最佳实践

### 推荐的项目结构

```
Scripts/
├── UI/                        # UI相关脚本
│   ├── Panels/               # 面板脚本
│   │   ├── MainMenuPanel.cs
│   │   └── SettingsPanel.cs
│   └── Gen/                  # 生成的代码目录
│       ├── MainMenuPanel/
│       └── SettingsPanel/
├── Gameplay/                 # 游戏逻辑
└── Common/                   # 通用组件
```

### 命名约定

- **预制体名称** - 使用PascalCase，如`MainMenuPanel`
- **节点名称** - 使用有意义的名称，如`Button_Start`、`Text_Title`
- **脚本类名** - 与预制体名称保持一致

### 性能建议

- 避免在单个面板中绑定过多组件（建议<50个）
- 大型UI可以拆分为多个子面板
- 生成代码后及时提交版本控制

## 📄 许可证

本项目采用MIT许可证，详见[LICENSE](LICENSE)文件。

---

**AutoUIBinder** - 让Unity UI开发更高效！ 