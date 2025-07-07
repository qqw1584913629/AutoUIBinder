<h1 align="center">✨ AutoUIBinder</h1>
<p align="center">
    <strong>🚀 Unity UI开发的终极利器</strong>
</p>
<p align="center">
    <a href="README.md">English</a> •
    <a href="README_CN.md">中文</a>
</p>

---

> **告别繁琐的UI绑定代码！** AutoUIBinder通过可视化组件绑定和自动代码生成，彻底改变你的Unity UI开发方式。

## 🎯 为什么选择AutoUIBinder？

**使用AutoUIBinder之前：**
```csharp
// 手动绑定 - 繁琐且容易出错
public Button startButton;
public Text titleText;
public Image backgroundImage;
// ... 还有更多组件

void Awake() {
    startButton = transform.Find("Button_Start").GetComponent<Button>();
    titleText = transform.Find("Text_Title").GetComponent<Text>();
    backgroundImage = transform.Find("Image_Background").GetComponent<Image>();
    // ... 每个组件都要手动绑定
}
```

**使用AutoUIBinder之后：**
```csharp
// 只需继承并点击 - 就这么简单！
public partial class MyUIPanel : AutoUIBinderBase 
{
    void Start() {
        // 所有组件自动生成，直接使用
        Button_Start.onClick.AddListener(OnStartClick);
        Text_Title.text = "欢迎使用！";
    }
}
```

## ✨ 核心特性

🎨 **可视化绑定** - 点击Hierarchy中的组件图标即可瞬间绑定  
⚡ **一键生成** - 自动生成所有组件引用代码  
🧠 **智能命名** - 智能处理冲突和非法字符  
🎯 **事件绑定** - 自动连接UI事件到你的方法  
🔧 **零配置** - 开箱即用，无需复杂设置  

## 🚀 5分钟快速上手

### 系统要求
- Unity 2021.3.39f1c1+ 
- 支持任意Unity UI系统（UGUI、TextMeshPro）

### 3步完成UI绑定

#### 1️⃣ 创建UI脚本
```csharp
public class MainMenuPanel : AutoUIBinderBase 
{
    // 就这么简单！无需手动声明组件
}
```

#### 2️⃣ 可视化绑定
- 将脚本挂载到预制体根对象
- 进入预制体编辑模式  
- **点击Hierarchy中的组件图标** - 绑定时会高亮显示！
- 实时视觉反馈，所见即所得

#### 3️⃣ 生成代码并使用
```csharp
// 点击Inspector中的"🚀 生成UI代码"按钮
// 然后立即使用你的组件：

void Start() {
    Button_Play.onClick.AddListener(() => StartGame());
    Text_PlayerName.text = PlayerPrefs.GetString("name");
    Slider_Volume.value = AudioListener.volume;
}
```

## 🎯 事件绑定神器

**全新功能：自动事件绑定！**
```csharp
public class GamePanel : AutoUIBinderBase 
{
    [UIEvent] // 🔥 魔法就在这里
    void OnPlayButtonClick() {
        // 自动连接到Button_Play.onClick
        StartGame();
    }
    
    [UIEvent]
    void OnVolumeSliderChanged(float value) {
        // 自动连接到Slider_Volume.onValueChanged
        AudioListener.volume = value;
    }
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

## 🤝 参与贡献

**我们欢迎社区贡献**
- 🐛 报告Bug和建议新功能
- 📝 完善项目文档
- 🔧 提交Pull Request
- ⭐ 给项目加Star表示支持

## 📄 开源协议

MIT协议 - 详见[LICENSE](LICENSE)文件。

---

<p align="center">
    <strong>⚡ AutoUIBinder - 让Unity UI开发如丝般顺滑！ ⚡</strong>
</p>
<p align="center">
    用❤️制作 by AutoUIBinder团队
</p> 