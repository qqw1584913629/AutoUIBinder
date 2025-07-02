using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(SerializableDictionary<,>))]
public class SerializableDictionaryDrawer : PropertyDrawer
{
    private const float LineHeight = 20f;
    private const float Spacing = 2f;
    private const float HeaderHeight = 22f;
    private bool isExpanded = true;

    // 自定义颜色
    private static readonly Color HeaderColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    private static readonly Color HeaderHoverColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    private static readonly Color HeaderTextColor = new Color(0.9f, 0.9f, 0.9f);
    private static readonly Color SeparatorColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    private static readonly Color ItemBackgroundColor = new Color(0.85f, 0.85f, 0.85f, 0.1f);
    private static readonly Color HighlightColor = new Color(0.2f, 0.4f, 0.8f, 0.2f);
    private static readonly Color LabelColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 获取自定义显示名称
        string displayName = label.text;
        var parentObject = property.serializedObject.targetObject;
        var field = parentObject.GetType().GetField(property.name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            var attr = System.Attribute.GetCustomAttribute(field, typeof(DictionaryDisplayNameAttribute)) as DictionaryDisplayNameAttribute;
            if (attr != null)
            {
                displayName = attr.DisplayName;
            }
        }

        EditorGUI.BeginProperty(position, label, property);

        // 绘制标题区域
        var headerRect = new Rect(position.x, position.y, position.width, HeaderHeight);
        var headerBorderRect = new Rect(headerRect.x, headerRect.y + headerRect.height - 1, headerRect.width, 1);

        // 检查鼠标是否悬停在标题上
        bool headerHovering = headerRect.Contains(Event.current.mousePosition);
        EditorGUI.DrawRect(headerRect, headerHovering ? HeaderHoverColor : HeaderColor);
        EditorGUI.DrawRect(headerBorderRect, new Color(0, 0, 0, 0.4f));

        // 自定义折叠箭头
        var arrowRect = new Rect(position.x + 4, position.y + (HeaderHeight - 13) / 2, 13, 13);
        var arrowColor = HeaderTextColor;
        if (Event.current.type == EventType.Repaint)
        {
            // 绘制圆形背景
            if (headerHovering)
            {
                EditorGUI.DrawRect(new Rect(arrowRect.x - 2, arrowRect.y - 2, arrowRect.width + 4, arrowRect.height + 4),
                    new Color(1, 1, 1, 0.1f));
            }
            
            // 绘制箭头
            var arrowPath = new Vector3[3];
            if (isExpanded)
            {
                arrowPath[0] = new Vector3(arrowRect.x + 2, arrowRect.y + 4);
                arrowPath[1] = new Vector3(arrowRect.x + arrowRect.width - 2, arrowRect.y + 4);
                arrowPath[2] = new Vector3(arrowRect.x + arrowRect.width / 2, arrowRect.y + arrowRect.height - 2);
            }
            else
            {
                arrowPath[0] = new Vector3(arrowRect.x + 4, arrowRect.y + 2);
                arrowPath[1] = new Vector3(arrowRect.x + 4, arrowRect.y + arrowRect.height - 2);
                arrowPath[2] = new Vector3(arrowRect.x + arrowRect.width - 2, arrowRect.y + arrowRect.height / 2);
            }
            
            Handles.color = arrowColor;
            Handles.DrawAAConvexPolygon(arrowPath);
        }

        // 处理箭头点击
        if (Event.current.type == EventType.MouseDown && headerRect.Contains(Event.current.mousePosition))
        {
            isExpanded = !isExpanded;
            Event.current.Use();
            GUI.changed = true;
        }

        // 绘制标题文本
        var titleStyle = new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = HeaderTextColor },
            fontStyle = FontStyle.Bold,
            fontSize = 12,
            padding = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 0)
        };
        var titleRect = new Rect(arrowRect.xMax + 4, position.y + (HeaderHeight - 16) / 2, 
            position.width - arrowRect.xMax - 8, 16);
        EditorGUI.LabelField(titleRect, displayName, titleStyle);

        if (isExpanded)
        {
            var pairsProperty = property.FindPropertyRelative("pairs");
            float yOffset = HeaderHeight + Spacing;

            // 绘制列标题
            var columnHeaderRect = new Rect(position.x, position.y + yOffset, position.width, LineHeight);
            EditorGUI.DrawRect(columnHeaderRect, new Color(0.3f, 0.3f, 0.3f, 0.2f));
            
            var labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.normal.textColor = LabelColor;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.fontSize = 11;

            EditorGUI.LabelField(
                new Rect(position.x + 4, columnHeaderRect.y + 2, 20, LineHeight),
                "#", labelStyle);
            EditorGUI.LabelField(
                new Rect(position.x + 28, columnHeaderRect.y + 2, position.width * 0.4f - 28, LineHeight),
                "Key (组件名称)", labelStyle);
            EditorGUI.LabelField(
                new Rect(position.x + position.width * 0.47f, columnHeaderRect.y + 2, position.width * 0.43f, LineHeight),
                "Value (组件引用)", labelStyle);
            EditorGUI.LabelField(
                new Rect(position.x + position.width - 20, columnHeaderRect.y + 2, 16, LineHeight),
                "状态", labelStyle);

            yOffset += LineHeight + Spacing;

            // 绘制所有键值对
            for (int i = 0; i < pairsProperty.arraySize; i++)
            {
                var pairProperty = pairsProperty.GetArrayElementAtIndex(i);
                var keyProperty = pairProperty.FindPropertyRelative("Key");
                var valueProperty = pairProperty.FindPropertyRelative("Value");

                // 绘制项背景
                var itemRect = new Rect(position.x, position.y + yOffset, position.width, LineHeight);
                EditorGUI.DrawRect(itemRect, i % 2 == 0 ? ItemBackgroundColor : Color.clear);

                // 鼠标悬停效果
                bool isHovering = itemRect.Contains(Event.current.mousePosition);
                if (isHovering)
                {
                    EditorGUI.DrawRect(itemRect, HighlightColor);
                }

                // 绘制索引编号
                var indexRect = new Rect(position.x + 4, position.y + yOffset + 2, 20, LineHeight - 4);
                var indexStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
                    fontSize = 10,
                    alignment = TextAnchor.MiddleCenter
                };
                EditorGUI.LabelField(indexRect, (i + 1).ToString(), indexStyle);

                // 绘制键（只读）
                using (new EditorGUI.DisabledScope(true))
                {
                    var keyRect = new Rect(position.x + 28, position.y + yOffset, position.width * 0.4f - 28, LineHeight);
                    EditorGUI.PropertyField(keyRect, keyProperty, GUIContent.none);
                }

                // 绘制箭头符号
                var itemArrowRect = new Rect(position.x + position.width * 0.42f, position.y + yOffset + 2, 20, LineHeight - 4);
                var arrowStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = new Color(0.5f, 0.5f, 0.5f) },
                    fontSize = 12,
                    alignment = TextAnchor.MiddleCenter
                };
                EditorGUI.LabelField(itemArrowRect, "→", arrowStyle);

                // 绘制值（只读）
                using (new EditorGUI.DisabledScope(true))
                {
                    var valueRect = new Rect(position.x + position.width * 0.47f, position.y + yOffset, position.width * 0.48f, LineHeight);
                    EditorGUI.PropertyField(valueRect, valueProperty, GUIContent.none);
                }
                
                // 绘制状态指示器
                var statusRect = new Rect(position.x + position.width - 20, position.y + yOffset + 2, 16, LineHeight - 4);
                var component = valueProperty.objectReferenceValue as Component;
                if (component != null)
                {
                    var statusStyle = new GUIStyle(EditorStyles.label)
                    {
                        normal = { textColor = Color.green },
                        fontSize = 12,
                        alignment = TextAnchor.MiddleCenter
                    };
                    EditorGUI.LabelField(statusRect, "✓", statusStyle);
                }
                else
                {
                    var statusStyle = new GUIStyle(EditorStyles.label)
                    {
                        normal = { textColor = Color.red },
                        fontSize = 12,
                        alignment = TextAnchor.MiddleCenter
                    };
                    EditorGUI.LabelField(statusRect, "✗", statusStyle);
                }

                yOffset += LineHeight + Spacing;
            }
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!isExpanded)
            return HeaderHeight;

        var pairsProperty = property.FindPropertyRelative("pairs");
        float height = HeaderHeight + Spacing;  // 标题高度
        height += LineHeight + Spacing;         // 列标题高度
        height += pairsProperty.arraySize * (LineHeight + Spacing);  // 所有项的高度
        return height;
    }
} 