using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GizmosDrawer
{
    [AddComponentMenu("GizmosDrawer")]
    public class GizmosDrawer : MonoBehaviour
    {
        public enum Shape { SphereWire, Sphere, BoxWire, Box }

        [Header("Basic Settings")]
        public bool useColliderBounds = false;
        public Shape gizmoShape = Shape.Sphere;

        [Min(0)] public float gizmoScale = 1f;
        public Color gizmoColor = Color.yellow;

        [Header("Axis Settings")]
        public bool showLocalAxes = false;
        [Min(0)] public float axisLength = 0.6f;

        [Header("Connection Line")]
        public bool drawConnectionLine = false;
        public Transform targetConnection;

        [Header("Box Shape Settings")]
        public Vector3 boxSize = Vector3.one;

        [SerializeField, Tooltip("Optional custom collider reference. If not set, will default to GetComponent<Collider>().")]
        private Collider cachedCollider;

        private Collider ActiveCollider
        {
            get
            {
                if (cachedCollider == null)
                {
                    cachedCollider = GetComponent<Collider>();
                    if (!cachedCollider) useColliderBounds = false;
                }
                return cachedCollider;
            }
        }

        private void Reset()
        {
            if (ActiveCollider) useColliderBounds = true;
            gizmoColor = new Color(Random.value, Random.value, Random.value, Random.Range(0.5f, 1f));
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!UnityEditorInternal.InternalEditorUtility.GetIsInspectorExpanded(this) || !enabled)
                return;

            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);

            DrawConnection();
            Gizmos.matrix = transform.localToWorldMatrix;
            DrawAxes();

            if (useColliderBounds && ActiveCollider)
                DrawColliderBounds(selected: false);
            else
                DrawShape(shape: gizmoShape);
        }

        private void OnDrawGizmosSelected()
        {
            if (!enabled) return;

            Gizmos.color = Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;

            if (useColliderBounds && ActiveCollider)
                DrawColliderBounds(selected: true);
            else
                DrawShape(shape: gizmoShape);
        }

        private void DrawAxes()
        {
            if (!showLocalAxes) return;

            UnityEditor.Handles.color = gizmoColor;
            UnityEditor.Handles.ArrowHandleCap(0, transform.position, transform.rotation, axisLength, EventType.Repaint);
        }
#endif

        private void DrawConnection()
        {
            if (!drawConnectionLine || targetConnection == null) return;

            Gizmos.color = gizmoColor;
            Gizmos.DrawLine(transform.position, targetConnection.position);
        }

        private void DrawShape(Shape shape)
        {
            Vector3 center = Vector3.zero;

            switch (shape)
            {
                case Shape.Sphere:
                    Gizmos.color = gizmoColor;
                    Gizmos.DrawSphere(center, gizmoScale);
                    Gizmos.DrawWireSphere(center, gizmoScale);
                    break;

                case Shape.Box:
                    Gizmos.color = gizmoColor;
                    Gizmos.DrawCube(center, boxSize * gizmoScale);
                    Gizmos.DrawWireCube(center, boxSize * gizmoScale);
                    break;
                
                case Shape.SphereWire:
                    Gizmos.color = gizmoColor;
                    Gizmos.DrawWireSphere(center, gizmoScale);
                    break;
                
                case Shape.BoxWire:
                    Gizmos.color = gizmoColor;
                    Gizmos.DrawWireCube(center, boxSize * gizmoScale);
                    break;
            }
        }

        private void DrawColliderBounds(bool selected)
        {
            if (!ActiveCollider || !ActiveCollider.enabled) return;

            Color outline = selected ? Color.yellow : new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Color fill = selected ? Color.clear : gizmoColor;

            Gizmos.color = outline;

            if (ActiveCollider is BoxCollider box)
            {
                Gizmos.DrawWireCube(box.center, box.size);
                if (fill.a > 0f)
                {
                    Gizmos.color = fill;
                    Gizmos.DrawCube(box.center, box.size);
                }
            }
            else if (ActiveCollider is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
                if (fill.a > 0f)
                {
                    Gizmos.color = fill;
                    Gizmos.DrawSphere(sphere.center, sphere.radius);
                }
            }
        }
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(GizmosDrawer))]
    public class GizmoDrawerEditor : Editor
    {
        SerializedProperty useColliderBounds, gizmoShape, gizmoScale, gizmoColor;
        SerializedProperty showLocalAxes, axisLength, drawConnectionLine, targetConnection, boxSize;

        private void OnEnable()
        {
            useColliderBounds = serializedObject.FindProperty(nameof(GizmosDrawer.useColliderBounds));
            gizmoShape = serializedObject.FindProperty(nameof(GizmosDrawer.gizmoShape));
            gizmoScale = serializedObject.FindProperty(nameof(GizmosDrawer.gizmoScale));
            gizmoColor = serializedObject.FindProperty(nameof(GizmosDrawer.gizmoColor));
            showLocalAxes = serializedObject.FindProperty(nameof(GizmosDrawer.showLocalAxes));
            axisLength = serializedObject.FindProperty(nameof(GizmosDrawer.axisLength));
            drawConnectionLine = serializedObject.FindProperty(nameof(GizmosDrawer.drawConnectionLine));
            targetConnection = serializedObject.FindProperty(nameof(GizmosDrawer.targetConnection));
            boxSize = serializedObject.FindProperty(nameof(GizmosDrawer.boxSize));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(useColliderBounds);
            EditorGUILayout.PropertyField(gizmoColor);

            EditorGUILayout.PropertyField(showLocalAxes);
            if (showLocalAxes.boolValue)
                EditorGUILayout.PropertyField(axisLength);

            if (!useColliderBounds.boolValue)
            {
                EditorGUILayout.PropertyField(gizmoShape);
                EditorGUILayout.PropertyField(gizmoScale);

                if ((GizmosDrawer.Shape)gizmoShape.enumValueIndex == GizmosDrawer.Shape.Box || (GizmosDrawer.Shape)gizmoShape.enumValueIndex == GizmosDrawer.Shape.BoxWire)
                    EditorGUILayout.PropertyField(boxSize);
            }

            EditorGUILayout.PropertyField(drawConnectionLine);
            if (drawConnectionLine.boolValue)
                EditorGUILayout.PropertyField(targetConnection);

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
