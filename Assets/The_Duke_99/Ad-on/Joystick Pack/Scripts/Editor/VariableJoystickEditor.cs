using UnityEngine;

using UnityEditor;

[CustomEditor(typeof(VariableJoystick))]
public class VariableJoystickEditor : JoystickEditor
{
    private SerializedProperty moveThreshold;
    private SerializedProperty joystickType;
    private SerializedProperty alwaysShowJoystick;
    private SerializedProperty portraitPosition;
    private SerializedProperty landscapePosition;

    protected override void OnEnable()
    {
        base.OnEnable();

        moveThreshold = serializedObject.FindProperty("moveThreshold");
        joystickType = serializedObject.FindProperty("joystickType");
        alwaysShowJoystick = serializedObject.FindProperty("alwaysShowJoystick");
        portraitPosition = serializedObject.FindProperty("portraitPosition");
        landscapePosition = serializedObject.FindProperty("landscapePosition");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (background != null)
        {
            RectTransform backgroundRect = (RectTransform)background.objectReferenceValue;
            backgroundRect.pivot = center;
        }
    }

    protected override void DrawValues()
    {
        base.DrawValues();
        EditorGUILayout.PropertyField(moveThreshold, new GUIContent("Move Threshold", "The distance away from the center input has to be before the joystick begins to move."));
        EditorGUILayout.PropertyField(joystickType, new GUIContent("Joystick Type", "The type of joystick the variable joystick is current using."));

        int typeIndex = joystickType.enumValueIndex;

        if (typeIndex != 0) {
            EditorGUILayout.PropertyField(alwaysShowJoystick, new GUIContent("Always Show Joystick"));
        }

        EditorGUILayout.PropertyField(portraitPosition, new GUIContent("Portrait Position", "Percentage with pivot point at bottom left of the screen"));
        EditorGUILayout.PropertyField(landscapePosition, new GUIContent("Landscape Position", "Percentage with pivot point at bottom left of the screen"));
    }
}