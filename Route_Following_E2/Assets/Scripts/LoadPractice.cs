using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class LoadPractice : MonoBehaviour
{
    //public static int i = 0;


    //private EditorBuildSettingsScene[] gamescenes = GetSubID.scenes;
    //public static int[] sceneSequence = GetSubID.sceneorder;

    public void LoadPracticeScene ()
    {
        string sceneName = System.IO.Path.GetFileNameWithoutExtension("PracticeScene");
        SceneManager.LoadScene(sceneName);

        
    }
 


    
}
