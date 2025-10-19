using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace GizmosDrawer
{
    [AddComponentMenu("GizmosDrawer")]
    public class GizmosDrawer : MonoBehaviour
    {
        public enum Shape { SphereWire, Sphere, BoxWire, Box }

        [Header("Basic Settings")]
        [Tooltip("If true, use the attached or assigned Collider bounds instead of the manual shape.")]
        public bool useColliderBounds = false;
        public Shape gizmoShape = Shape.Sphere;

        [Min(0f)]
        public float gizmoScale = 1f;
        public Color gizmoColor = Color.yellow;

        [Header("Axis Settings")]
        public bool showLocalAxes = false;
        [Min(0f)]
        public float axisLength = 0.6f;

        [Header("Connection Line")]
        public bool drawConnectionLine = false;
        public Transform targetConnection;

        [Header("Box Shape Settings")]
        public Vector3 boxSize = Vector3.one;

        [Header("Behavior / Runtime Settings")]
        [Tooltip("If true, gizmos will also be drawn in Play Mode (not only in Editor Scene view).")]
        public bool showInPlayMode = false;

        [SerializeField, Tooltip("Optional custom collider reference. If not set, will default to GetComponent<Collider>().")]
        private Collider cachedCollider;

        /// <summary>
        /// Returns cached collider (or attempts to get one from the GameObject).
        /// Public accessor so editor can inspect safely.
        /// </summary>
        public Collider GetActiveCollider()
        {
            if (cachedCollider == null)
            {
                cachedCollider = GetComponent<Collider>();
                if (cachedCollider == null)
                {
                    useColliderBounds = false;
                }
            }
            return cachedCollider;
        }

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null)
            {
                cachedCollider = col;
                useColliderBounds = true;
            }
            gizmoColor = new Color(Random.value, Random.value, Random.value,
                                    Mathf.Clamp01(Random.Range(0.5f, 1f)));
        }

        private void OnValidate()
        {
            gizmoScale = Mathf.Max(0f, gizmoScale);
            axisLength = Mathf.Max(0f, axisLength);
            boxSize = new Vector3(Mathf.Max(0f, boxSize.x),
                                  Mathf.Max(0f, boxSize.y),
                                  Mathf.Max(0f, boxSize.z));
        }

#if UNITY_EDITOR
        private bool ShouldDrawGizmos()
        {
            bool inspectorExpanded = InternalEditorUtility.GetIsInspectorExpanded(this);
            if (!EditorApplication.isPlaying)
                return inspectorExpanded;
            else
                return showInPlayMode;
        }

        private void OnDrawGizmos()
        {
            if (!ShouldDrawGizmos() || !enabled) return;

            Matrix4x4 prevMatrix = Gizmos.matrix;
            Color prevColor = Gizmos.color;

            DrawConnection();

            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            DrawAxes();

            if (useColliderBounds && GetActiveCollider() != null)
                DrawColliderBounds(selected: false);
            else
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                DrawShape(gizmoShape);
                DrawShapeLabel();
            }

            Gizmos.matrix = prevMatrix;
            Gizmos.color = prevColor;
        }

        private void OnDrawGizmosSelected()
        {
            if (!enabled) return;

            Matrix4x4 prevMatrix = Gizmos.matrix;
            Color prevColor = Gizmos.color;

            Gizmos.color = Color.yellow;
            DrawAxes();

            if (useColliderBounds && GetActiveCollider() != null)
                DrawColliderBounds(selected: true);
            else
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                DrawShape(gizmoShape);
                DrawShapeLabel();
            }

            Gizmos.matrix = prevMatrix;
            Gizmos.color = prevColor;
        }
#endif

        private void DrawConnection()
        {
            if (!drawConnectionLine || targetConnection == null) return;
            Gizmos.color = gizmoColor;
            Gizmos.DrawLine(transform.position, targetConnection.position);
        }

        private void DrawAxes()
        {
            if (!showLocalAxes) return;

            float maxScale = Mathf.Max(transform.lossyScale.x,
                                      Mathf.Max(transform.lossyScale.y, transform.lossyScale.z));
            float arrowSize = Mathf.Max(0.0001f, axisLength * Mathf.Max(1f, maxScale));

#if UNITY_EDITOR
            Handles.color = gizmoColor;

            // X
            Handles.ArrowHandleCap(0, transform.position,
                                   transform.rotation * Quaternion.Euler(0f, 0f, -90f),
                                   arrowSize, EventType.Repaint);
            // Y
            Handles.ArrowHandleCap(1, transform.position,
                                   transform.rotation * Quaternion.Euler(90f, 0f, 0f),
                                   arrowSize, EventType.Repaint);
            // Z
            Handles.ArrowHandleCap(2, transform.position,
                                   transform.rotation,
                                   arrowSize, EventType.Repaint);

            var prevMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawLine(Vector3.zero, Vector3.right * axisLength);
            Gizmos.DrawLine(Vector3.zero, Vector3.up * axisLength);
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * axisLength);
            Gizmos.matrix = prevMatrix;
#endif
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
            var active = GetActiveCollider();
            if (active == null || !active.enabled) return;

            Matrix4x4 prevMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            Color outline = selected ? Color.yellow :
                            new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Color fill = selected ? Color.clear : gizmoColor;

            Gizmos.color = outline;
            if (active is BoxCollider box)
            {
                Gizmos.DrawWireCube(box.center, box.size);
                if (fill.a > 0f)
                {
                    Gizmos.color = fill;
                    Gizmos.DrawCube(box.center, box.size);
                }
            }
            else if (active is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
                if (fill.a > 0f)
                {
                    Gizmos.color = fill;
                    Gizmos.DrawSphere(sphere.center, sphere.radius);
                }
            }
            else if (active is CapsuleCollider capsule)
            {
                Vector3 center = capsule.center;
                float radius = capsule.radius;
                float height = Mathf.Max(capsule.height, radius * 2f);
                int dir = capsule.direction;
                Vector3 axis = Vector3.up;
                if (dir == 0) axis = Vector3.right;
                else if (dir == 2) axis = Vector3.forward;
                float half = (height * 0.5f) - radius;
                Vector3 a = center + axis * half;
                Vector3 b = center - axis * half;
                Gizmos.DrawWireSphere(a, radius);
                Gizmos.DrawWireSphere(b, radius);
                Vector3 bodyCenter = (a + b) * 0.5f;
                Vector3 bodySize = Vector3.one * (radius * 2f);
                if (dir == 0) bodySize = new Vector3(height - radius * 2f, radius * 2f, radius * 2f);
                else if (dir == 1) bodySize = new Vector3(radius * 2f, height - radius * 2f, radius * 2f);
                else if (dir == 2) bodySize = new Vector3(radius * 2f, radius * 2f, height - radius * 2f);
                Gizmos.DrawWireCube(bodyCenter, bodySize);
            }
            else
            {
                // fallback to world bounds
                Gizmos.matrix = Matrix4x4.identity;
                var bounds = active.bounds;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }

            Gizmos.matrix = prevMatrix;
        }

        private void DrawShapeLabel()
        {
            if (useColliderBounds && GetActiveCollider() != null) return;

            Vector3 worldPos = transform.position;
            string label = "";
            switch (gizmoShape)
            {
                case Shape.Sphere:
                case Shape.SphereWire:
                    label = $"Radius: {gizmoScale:F2}";
                    break;
                case Shape.Box:
                case Shape.BoxWire:
                    label = $"Size: {boxSize.x * gizmoScale:F2}, {boxSize.y * gizmoScale:F2}, {boxSize.z * gizmoScale:F2}";
                    break;
            }

/*#if UNITY_EDITOR
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 12;
            Handles.Label(worldPos + Vector3.up * HandleUtility.GetHandleSize(worldPos) * 0.5f, label, style);
#endif*/
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(GizmosDrawer))]
    public class GizmoDrawerEditor : Editor
    {
        SerializedProperty cachedColliderProp;
        SerializedProperty useColliderBoundsProp, gizmoShapeProp, gizmoScaleProp, gizmoColorProp;
        SerializedProperty showLocalAxesProp, axisLengthProp;
        SerializedProperty drawConnectionLineProp, targetConnectionProp;
        SerializedProperty boxSizeProp, showInPlayModeProp;

        private void OnEnable()
        {
            cachedColliderProp = serializedObject.FindProperty("cachedCollider");
            useColliderBoundsProp = serializedObject.FindProperty(nameof(GizmosDrawer.useColliderBounds));
            gizmoShapeProp = serializedObject.FindProperty(nameof(GizmosDrawer.gizmoShape));
            gizmoScaleProp = serializedObject.FindProperty(nameof(GizmosDrawer.gizmoScale));
            gizmoColorProp = serializedObject.FindProperty(nameof(GizmosDrawer.gizmoColor));
            showLocalAxesProp = serializedObject.FindProperty(nameof(GizmosDrawer.showLocalAxes));
            axisLengthProp = serializedObject.FindProperty(nameof(GizmosDrawer.axisLength));
            drawConnectionLineProp = serializedObject.FindProperty(nameof(GizmosDrawer.drawConnectionLine));
            targetConnectionProp = serializedObject.FindProperty(nameof(GizmosDrawer.targetConnection));
            boxSizeProp = serializedObject.FindProperty(nameof(GizmosDrawer.boxSize));
            showInPlayModeProp = serializedObject.FindProperty(nameof(GizmosDrawer.showInPlayMode));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Collider Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(cachedColliderProp, new GUIContent("Cached Collider", "Optional custom collider to use for bounds."));
            EditorGUILayout.PropertyField(useColliderBoundsProp, new GUIContent("Use Collider Bounds", "If true, uses collider bounds instead of manual shape."));

            var drawer = (GizmosDrawer)target;
            if (drawer.useColliderBounds && (drawer.GetActiveCollider() == null || !drawer.GetActiveCollider().enabled))
            {
                EditorGUILayout.HelpBox("Collider bounds enabled but no valid collider found or collider is disabled.", MessageType.Warning);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Shape Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(gizmoColorProp, new GUIContent("Gizmo Color", "Color of the gizmo outline/fill."));
            EditorGUILayout.PropertyField(showInPlayModeProp, new GUIContent("Show In Play Mode", "Whether to draw gizmo in Play Mode as well."));

            EditorGUILayout.PropertyField(showLocalAxesProp, new GUIContent("Show Local Axes", "Draw local axes from object origin."));
            if (showLocalAxesProp.boolValue)
                EditorGUILayout.PropertyField(axisLengthProp, new GUIContent("Axis Length", "Length of the local axes lines."));

            if (!drawer.useColliderBounds)
            {
                EditorGUILayout.PropertyField(gizmoShapeProp, new GUIContent("Gizmo Shape", "Select manual shape to draw."));
                EditorGUILayout.PropertyField(gizmoScaleProp, new GUIContent("Gizmo Scale", "Scale factor for the shape."));

                if (drawer.gizmoScale <= 0f)
                {
                    EditorGUILayout.HelpBox("Gizmo Scale should be positive.", MessageType.Warning);
                }

                var shape = drawer.gizmoShape;
                if (shape == GizmosDrawer.Shape.Box || shape == GizmosDrawer.Shape.BoxWire)
                {
                    EditorGUILayout.PropertyField(boxSizeProp, new GUIContent("Box Size", "Dimensions of the box before scale."));
                    if (drawer.boxSize.x <= 0f || drawer.boxSize.y <= 0f || drawer.boxSize.z <= 0f)
                    {
                        EditorGUILayout.HelpBox("Box Size components should be positive.", MessageType.Warning);
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Using collider bounds; manual shape settings are ignored.", MessageType.Info);
            }

            EditorGUILayout.Space();
            //EditorGUILayout.LabelField("Connection Line", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(drawConnectionLineProp, new GUIContent("Draw Connection Line", "Draw a line from this object to targetConnection."));
            if (drawConnectionLineProp.boolValue)
                EditorGUILayout.PropertyField(targetConnectionProp, new GUIContent("Target Connection", "Transform to draw line to."));

            serializedObject.ApplyModifiedProperties();
        }

        // Scene GUI: handles for editing boxSize directly in Scene View.
        private void OnSceneGUI()
        {
            var drawer = (GizmosDrawer)target;
            if (drawer == null) return;
            if (!drawer.enabled) return;

            // Only edit boxSize when manual box shape is active (not using collider)
            if (drawer.useColliderBounds) return;
            if (drawer.gizmoShape != GizmosDrawer.Shape.Box && drawer.gizmoShape != GizmosDrawer.Shape.BoxWire) return;

            // World matrix for drawing handles relative to object transform:
            Matrix4x4 matrix = drawer.transform.localToWorldMatrix;
            using (new Handles.DrawingScope(matrix))
            {
                Handles.color = drawer.gizmoColor;

                // The ScaleHandle takes a size vector (local). Position at zero (local origin).
                // Compute handle size on screen:
                float handleSize = HandleUtility.GetHandleSize(drawer.transform.position) * 0.5f;

                Vector3 currentSize = drawer.boxSize;
                Vector3 newSize = Handles.ScaleHandle(currentSize, Vector3.zero, Quaternion.identity, handleSize);

                // If user changed size, apply with Undo
                if (newSize != currentSize)
                {
                    Undo.RecordObject(drawer, "Change Box Size");
                    // ensure no negative values
                    newSize = new Vector3(Mathf.Max(0f, newSize.x), Mathf.Max(0f, newSize.y), Mathf.Max(0f, newSize.z));
                    drawer.boxSize = newSize;
                    EditorUtility.SetDirty(drawer);
                }
            }
        }
    }
#endif
}
