#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace InspectorSpriteDrawer.Scripts
{
    [CustomPropertyDrawer(typeof(Sprite))]
    public class InspectorSpriteDrawer : PropertyDrawer
    {
        private const float SIZE_IMAGE = 24;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect objectFieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.objectReferenceValue = EditorGUI.ObjectField(objectFieldRect, label, property.objectReferenceValue, typeof(Sprite), false);

            if (property.objectReferenceValue != null)
            {
                Sprite sprite = property.objectReferenceValue as Sprite;
                if (sprite != null)
                {
                    Texture2D tex = sprite.texture;
                    Rect spriteRect = sprite.textureRect;
                    Rect uv = new Rect(
                        spriteRect.x / tex.width,
                        spriteRect.y / tex.height,
                        spriteRect.width / tex.width,
                        spriteRect.height / tex.height
                    );
                    Rect previewRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, SIZE_IMAGE, SIZE_IMAGE);
                    GUI.DrawTextureWithTexCoords(previewRect, tex, uv, true);
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (property.objectReferenceValue != null)
            {
                height += 2 + SIZE_IMAGE;
            }
            return height;
        }
    }
}
#endif