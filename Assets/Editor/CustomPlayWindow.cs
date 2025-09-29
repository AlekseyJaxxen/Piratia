using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class CustomPlayWindow : EditorWindow
{
    [MenuItem("Custom/Play Window")]
    static void Open()
    {
        GetWindow<CustomPlayWindow>("Custom Play");
    }

    void OnGUI()
    {
        if (GUILayout.Button("Run"))
        {
            EditorSceneManager.OpenScene("Assets/MainMenu.unity"); // ѕуть к сцене
            EditorApplication.isPlaying = true;
        }
    }
}