using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "UIPathConfig", menuName = "AutoUIBinder/Create UI Path Config")]
public class UIPathConfig : ScriptableObject
{
    [SerializeField, ReadOnly]
    private string paths = "Assets/Scripts/";

    public string Paths => paths;  // 只读属性

#if UNITY_EDITOR
    [CustomEditor(typeof(UIPathConfig))]
    public class UIPathConfigEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            UIPathConfig config = (UIPathConfig)target;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("路径", config.paths);
                EditorGUI.EndDisabledGroup();
                
                if (GUILayout.Button("选择文件夹", GUILayout.Width(100)))
                {
                    string selectedPath = EditorUtility.OpenFolderPanel("选择文件夹", "Assets", "");
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        string relativePath = GetRelativePath(selectedPath);
                        if (!string.IsNullOrEmpty(relativePath))
                        {
                            SerializedObject serializedObject = new SerializedObject(config);
                            SerializedProperty pathProperty = serializedObject.FindProperty("paths");
                            pathProperty.stringValue = relativePath;
                            serializedObject.ApplyModifiedProperties();
                        }
                    }
                }
            }
        }

        private string GetRelativePath(string absolutePath)
        {
            string projectPath = System.IO.Path.GetFullPath(Application.dataPath + "/..");
            projectPath = projectPath.Replace("\\", "/");
            absolutePath = absolutePath.Replace("\\", "/");

            if (absolutePath.StartsWith(projectPath))
            {
                string relativePath = absolutePath.Substring(projectPath.Length + 1);
                return relativePath;
            }

            EditorUtility.DisplayDialog("错误", "请选择项目内的文件夹！", "确定");
            return "";
        }
    }
#endif
}

// 自定义只读特性
public class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginDisabledGroup(true);
        EditorGUI.PropertyField(position, property, label);
        EditorGUI.EndDisabledGroup();
    }
}
#endif