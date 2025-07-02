<h1 align="center">✨ AutoUIBinder</h1>
<p align="center">
    <a href="README.md">English</a> •
    <a href="README_CN.md">中文</a>
</p>

---

AutoUIBinder is a powerful Unity editor extension designed to simplify UI development workflow, providing visual component binding and automatic code generation capabilities.

## ✨ Features

- **Visual Component Binding** - Click component icons in the Hierarchy window to bind UI elements
- **Automatic Code Generation** - One-click to generate component reference code
- **Smart Naming Conflict Resolution** - Automatically detect and handle duplicate names
- **Prefab Edit Support** - Optimized for prefab editing mode
- **Performance Optimized** - Efficient redraw mechanism, no impact on editor performance

## 🚀 Quick Start

### Requirements

- Unity 2021.3.39f1c1 or higher
- Supports all Unity built-in UI systems (UGUI)

### Basic Usage

1. **Create UI Script**
   ```csharp
   using UnityEngine;
   
   public class MyUIPanel : AutoUIBinderBase
   {
       void Start()
       {
           // Your initialization code
       }
   }
   ```

2. **Bind Components**
   - Add the script to the root object of your prefab
   - Enter prefab edit mode
   - Click component icons to bind (will be highlighted)
   - Click again to unbind

3. **Generate Code**
   - Click "Generate UI" button in Inspector
   - Code will be generated to the configured path

4. **Use Generated Code**
   ```csharp
   void Start()
   {
       // Auto-generated properties can be used directly
       Button_Start.onClick.AddListener(OnStartClick);
       Text_Title.text = "Welcome to AutoUIBinder";
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

## 🐛 Troubleshooting

### Common Issues

**Q: Path error when generating code**
A: Check path settings in GlobalConfig, ensure directory exists and has write permission

**Q: Can't see icons in prefab edit mode**
A: Ensure prefab root or child objects have AutoUIBinderBase component

**Q: Bound components are null in code**
A: Make sure prefab is saved and project is recompiled after code generation

**Q: Hierarchy window performance lag**
A: Tool is optimized for performance, check for plugin conflicts if issues persist

### Debug Information

Tool provides detailed logs with `[AutoUIBinder]` prefix:

- **Info Level** - Normal operation records
- **Warning Level** - Potential issue alerts
- **Error Level** - Error details and stack traces

## 📈 Best Practices

### Recommended Project Structure

```
Scripts/
├── UI/                        # UI related scripts
│   ├── Panels/               # Panel scripts
│   │   ├── MainMenuPanel.cs
│   │   └── SettingsPanel.cs
│   └── Gen/                  # Generated code directory
│       ├── MainMenuPanel/
│       └── SettingsPanel/
├── Gameplay/                 # Game logic
└── Common/                   # Common components
```

### Naming Conventions

- **Prefab Names** - Use PascalCase, like `MainMenuPanel`
- **Node Names** - Use meaningful names, like `Button_Start`, `Text_Title`
- **Script Class Names** - Match with prefab names

### Performance Tips

- Avoid binding too many components in a single panel (<50 recommended)
- Split large UIs into sub-panels
- Commit to version control after code generation

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**AutoUIBinder** - Make Unity UI development more efficient!