namespace LoadManager.Editor
{
    using UnityEditor;
    using UnityEngine;
    using System.Linq;

    [CustomPropertyDrawer(typeof(SceneInfoAttribute))]
    public class SceneInfoProperyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();

            if(property.propertyType != SerializedPropertyType.String)
            {
                return;
            }
 
            EditorGUI.BeginProperty(position, label, property);

            var scenes = EditorBuildSettings.scenes.Where(_ => _.enabled).Select(_ => System.IO.Path.GetFileNameWithoutExtension(_.path)).ToList();
   
            if(scenes.Count == 0)
            {
                EditorGUILayout.HelpBox("Zero scenes active", MessageType.Info);
                return;
            }

            int index = scenes.IndexOf(scenes.Find(_ => _ == property.stringValue));

            int newIndex = EditorGUI.Popup(position, label.text, index, scenes.ToArray());

            if (newIndex == -1)
            {
                EditorGUILayout.HelpBox("Select scene", MessageType.Warning);
                return;
            }

            if (newIndex < 0 || newIndex >= scenes.Count)
            {
                EditorGUILayout.HelpBox("Something wrong with scenes", MessageType.Warning);
                return;
            }

            string selectedScene = scenes[newIndex];

            if(!property.stringValue.Equals(selectedScene, System.StringComparison.Ordinal))
            {
                property.stringValue = selectedScene;
            }

            EditorGUI.EndProperty();
        }
    }
}
