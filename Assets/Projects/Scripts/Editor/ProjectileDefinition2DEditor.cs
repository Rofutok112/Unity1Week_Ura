using Projects.Scripts.Characters;
using UnityEditor;

namespace Projects.Scripts.Editor
{
    [CustomEditor(typeof(ProjectileDefinition2D))]
    public sealed class ProjectileDefinition2DEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawProperty("movementType");
            DrawProperty("impactEffectPrefab");
            DrawProperty("speed");
            DrawProperty("lifetime");
            DrawProperty("maxDistance");
            DrawProperty("damage");
            DrawProperty("hitMask");

            SerializedProperty movementType = serializedObject.FindProperty("movementType");

            if ((ProjectileMovementType)movementType.enumValueIndex == ProjectileMovementType.DelayedHoming)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Delayed Homing Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("delayedHoming"), true);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawProperty(string propertyName)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(propertyName));
        }
    }
}
