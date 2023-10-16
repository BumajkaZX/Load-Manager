#if UNITY_EDITOR

namespace LoadManager.Editor
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public class SceneManagerWindow : EditorWindow
    {
        private List<ILoadingCondition> _loadingConditions = default;

        [MenuItem("Window/General/Scene Manager")]
        public static void ShowWindow() => GetWindow<SceneManagerWindow>("Scene Manager");
        private void FindConditions() => _loadingConditions = FindObjectsOfType<MonoBehaviour>(true).OfType<ILoadingCondition>().OrderBy(_ => _.Order).ToList();
        private void OnGUI()
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(sceneName, GUILayout.MaxWidth(120));
                bool isOpenScene = EditorSceneManager.GetSceneByName(sceneName).name != null;
                var color = new GUIStyle(GUI.skin.button);

                if (EditorSceneManager.GetActiveScene().name == sceneName && EditorSceneManager.sceneCount == 1)
                {
                    color.normal.textColor = Color.black;
                    GUI.backgroundColor = Color.grey;
                }
                if (GUILayout.Button("Open", color,  GUILayout.MaxWidth(120)))
                {
                    if (EditorSceneManager.GetActiveScene().name != sceneName || EditorSceneManager.sceneCount > 1)
                    {
                        EditorSceneManager.SaveOpenScenes();
                        EditorSceneManager.OpenScene(path);
                    }
                }

                GUI.backgroundColor = Color.white;

          

                if (isOpenScene && EditorSceneManager.sceneCount == 1)
                {
                    color.normal.textColor = Color.black;
                    GUI.backgroundColor = Color.grey;
                }

                if(isOpenScene && EditorSceneManager.sceneCount > 1)
                {
                    GUI.backgroundColor = Color.gray;
                }

                if (GUILayout.Button(isOpenScene ? "Close Additive" : "Open Additive", color, GUILayout.MaxWidth(120)))
                {
                    if (isOpenScene && EditorSceneManager.sceneCount != 1)
                    {
                        EditorSceneManager.SaveOpenScenes();
                        EditorSceneManager.CloseScene(EditorSceneManager.GetSceneByBuildIndex(i), true);
                    }
                    else
                    {
                        EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    }

                }

                EditorGUILayout.EndHorizontal();

                GUILayout.Space(5);

                GUI.backgroundColor = Color.white;
            }

            GUILayout.Space(20);

            GUILayout.Label("Loading orders");
            if(_loadingConditions == null || _loadingConditions.Count == 0)
            {
                EditorGUILayout.HelpBox("No orders", MessageType.Warning);
                if(GUILayout.Button("Find scripts"))
                {
                    FindConditions();
                }
                return;
            }
            for(int i = 0; i < _loadingConditions.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"{_loadingConditions[i].Name} : {_loadingConditions[i].Order}");
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Find scripts"))
            {
                FindConditions();
            }
        }

        private void OnEnable() => EditorApplication.hierarchyChanged += FindConditions;

        private void OnDisable() => EditorApplication.hierarchyChanged -= FindConditions;
    }
}

#endif
