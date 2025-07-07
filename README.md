<h1 align="center">✨ AutoUIBinder</h1>
<p align="center">
    <strong>🚀 The Ultimate Unity UI Development Tool</strong>
</p>
<p align="center">
    <a href="README.md">English</a> •
    <a href="README_CN.md">中文</a>
</p>

---

> **Stop writing repetitive UI binding code!** AutoUIBinder revolutionizes Unity UI development with visual component binding and automatic code generation.

## 🎯 Why AutoUIBinder?

**Before AutoUIBinder:**
```csharp
// Manual binding - tedious and error-prone
public Button startButton;
public Text titleText;
public Image backgroundImage;
// ... and many more

void Awake() {
    startButton = transform.Find("Button_Start").GetComponent<Button>();
    titleText = transform.Find("Text_Title").GetComponent<Text>();
    backgroundImage = transform.Find("Image_Background").GetComponent<Image>();
    // ... manual binding for every component
}
```

**After AutoUIBinder:**
```csharp
// Just inherit and click - that's it!
public partial class MyUIPanel : AutoUIBinderBase 
{
    void Start() {
        // All components auto-generated and ready to use
        Button_Start.onClick.AddListener(OnStartClick);
        Text_Title.text = "Welcome!";
    }
}
```

## ✨ Key Features

🎨 **Visual Binding** - Click component icons in Hierarchy to bind instantly  
⚡ **One-Click Generation** - Generate all component references automatically  
🧠 **Smart Naming** - Handles conflicts and invalid characters intelligently  
🎯 **Event Binding** - Auto-connect UI events to your methods  
🔧 **Zero Configuration** - Works out of the box with sensible defaults

## 🚀 Quick Start (5 Minutes)

### Requirements
- Unity 2021.3.39f1c1+ 
- Any Unity UI system (UGUI, TextMeshPro)

### 3 Steps to Success

#### 1️⃣ Create Your UI Script
```csharp
public class MainMenuPanel : AutoUIBinderBase 
{
    // That's it! No manual component declarations needed
}
```

#### 2️⃣ Visual Binding
- Attach script to your prefab root
- Enter prefab edit mode  
- **Click the component icons** in Hierarchy - they'll highlight when bound!
- Real-time visual feedback

#### 3️⃣ Generate & Use
```csharp
// Click "🚀 Generate UI Code" in Inspector
// Then use your components immediately:

void Start() {
    Button_Play.onClick.AddListener(() => StartGame());
    Text_PlayerName.text = PlayerPrefs.GetString("name");
    Slider_Volume.value = AudioListener.volume;
}
```

## 🎯 Event Binding Made Easy

**NEW: Automatic Event Binding!**
```csharp
public class GamePanel : AutoUIBinderBase 
{
    [UIEvent] // 🔥 Magic happens here
    void OnPlayButtonClick() {
        // Auto-connects to Button_Play.onClick
        StartGame();
    }
    
    [UIEvent]
    void OnVolumeSliderChanged(float value) {
        // Auto-connects to Slider_Volume.onValueChanged
        AudioListener.volume = value;
    }
}
```

## 📁 Project Structure

```
Assets/
├── AutoUIBinder/                # Core tool
│   ├── Core/                    # Core implementation
│   │   ├── Runtime/            # Runtime code
│   │   │   ├── Attributes/     # Attribute definitions
│   │   │   ├── Base/          # Base classes
│   │   │   └── Utils/         # Utility classes
│   │   └── Editor/             # Editor code
│   │       ├── Config/         # Configuration
│   │       ├── Core/          # Core editor functionality
│   │       └── Drawers/       # Custom drawers
│   └── Examples/               # Example code
│       ├── Runtime/            # Runtime examples
│       │   ├── Prefabs/       # Example prefabs
│       │   └── Scripts/       # Example scripts
│       └── Scenes/            # Example scenes
├── Scripts/                    # Project scripts
│   └── Gen/                   # Generated code
└── Resources/                 # Resource files
    └── GlobalConfig.asset     # Global configuration
```

## ⚙️ Configuration

### Global Settings

Configure code generation path in `Resources/GlobalConfig.asset`:

1. Click "Select Folder" in Inspector
2. Choose your script directory (usually `Assets/Scripts`)
3. Generated code will be saved in `{path}/Gen/{className}/`

### Code Generation Rules

- Generated file naming: `{className}Gen.cs`
- Uses partial class pattern, won't overwrite your main code
- Component property naming: `{NodeName}_{ComponentType}`
- Auto-adds XML documentation comments

## 🎨 Interface Guide

### Hierarchy Window Enhancement

- **Background Color** - Different colors for different AutoUIBinderBase types
- **Component Icons** - Shows icons for all components
- **Binding Status** - Special indicators for bound components
- **Smart Interaction** - Click icons to bind/unbind

### Inspector Enhancement

- **Component Reference List** - Shows all bound components in table format
- **One-Click Generation** - Prominent "Generate UI" button
- **Real-time Validation** - Automatically detects invalid references

## 🔧 Advanced Features

### Smart Naming

The tool automatically handles:

- **Duplicate Detection** - Auto-adds numeric suffixes for duplicate names
- **Invalid Characters** - Auto-replaces spaces with underscores
- **Keyword Conflicts** - Avoids using C# keywords as variable names
- **User Confirmation** - Shows confirmation dialog for duplicates

### Performance Features

- **On-Demand Redraw** - Only refreshes Hierarchy window when necessary
- **Smart Caching** - Caches colors and states
- **Event-Driven** - Based on Unity event system, responsive

### Error Handling

- **Friendly Tips** - All errors have detailed Chinese messages
- **File Backup** - Auto-creates backup before overwriting
- **Exception Recovery** - Auto-recovers binding state after editor restart

## 🤝 Contributing

**We welcome community contributions**
- 🐛 Report bugs and suggest features
- 📝 Improve documentation
- 🔧 Submit pull requests
- ⭐ Star the project to show your support

## 📄 License

MIT License - see [LICENSE](LICENSE) for details.

---

<p align="center">
    <strong>⚡ AutoUIBinder - Making Unity UI Development a Breeze! ⚡</strong>
</p>
<p align="center">
    Made with ❤️ by the AutoUIBinder Team
</p>