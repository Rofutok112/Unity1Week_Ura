using Projects.Scripts.Characters;
using UnityEditor;

namespace Projects.Scripts.Editor
{
    [CustomEditor(typeof(WeaponDefinition2D))]
    public sealed class WeaponDefinition2DEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawProperty("projectileDefinition");
            DrawProperty("projectilePrefab");
            DrawProperty("muzzleLocalOffset");
            DrawProperty("aimConstraintMode");

            var aimConstraintMode = serializedObject.FindProperty("aimConstraintMode");

            if ((AimConstraintMode)aimConstraintMode.enumValueIndex == AimConstraintMode.ClampAngle)
            {
                DrawProperty("maxAimAngle");
            }
            else if ((AimConstraintMode)aimConstraintMode.enumValueIndex == AimConstraintMode.SnapDirections)
            {
                DrawProperty("snapStepAngle");
            }

            DrawProperty("launchDirectionMode");

            var launchDirectionMode = serializedObject.FindProperty("launchDirectionMode");

            if ((WeaponLaunchDirectionMode)launchDirectionMode.enumValueIndex == WeaponLaunchDirectionMode.FixedLocalDirection)
            {
                DrawProperty("launchDirectionLocal");
            }

            DrawProperty("fireAudioClip");
            DrawProperty("fireAudioVolume");
            DrawProperty("fireMode");
            DrawProperty("fireInterval");
            DrawProperty("pelletsPerShot");
            DrawProperty("spreadAngle");
            DrawProperty("accuracy");

            var fireMode = serializedObject.FindProperty("fireMode");

            if ((WeaponFireMode)fireMode.enumValueIndex == WeaponFireMode.Burst)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Burst Settings", EditorStyles.boldLabel);
                DrawProperty("burstCount");
                DrawProperty("burstInterval");
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawProperty(string propertyName)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(propertyName));
        }
    }
}
