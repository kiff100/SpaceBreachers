using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    [Header("Persistent Scenes")]
    [SerializeField] private string uiSceneName = "PersistentUI";

    [Header("Sectors")]
    [SerializeField] private string startingSector = "CollectionTest";

    private string _currentSectorName;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start()
    {
        // 1. Load the UI scene additively
        if (!IsSceneLoaded(uiSceneName))
        {
            yield return SceneManager.LoadSceneAsync(uiSceneName, LoadSceneMode.Additive);
        }

        // 2. Load the first gameplay sector if we aren't already in one
        // If we have more than 2 scenes (Bootstrap + PersistentUI), it means a sector was already loaded in editor
        if (SceneManager.sceneCount <= 2) 
        {
            yield return LoadSectorRoutine(startingSector);
        }
        else
        {
            // Find which sector is currently loaded (for Editor testing)
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name != uiSceneName && scene.name != gameObject.scene.name)
                {
                    _currentSectorName = scene.name;
                    SceneManager.SetActiveScene(scene);
                    break;
                }
            }
        }
    }

    public void SwitchSector(string newSectorName)
    {
        StartCoroutine(LoadSectorRoutine(newSectorName));
    }

    private IEnumerator LoadSectorRoutine(string newSectorName)
    {
        // Unload current sector if it exists
        if (!string.IsNullOrEmpty(_currentSectorName))
        {
            yield return SceneManager.UnloadSceneAsync(_currentSectorName);
        }

        // Load new sector additively
        yield return SceneManager.LoadSceneAsync(newSectorName, LoadSceneMode.Additive);
        
        _currentSectorName = newSectorName;
        
        // Set the sector as active so new objects spawn there
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(newSectorName));
        
        Debug.Log($"Loaded Sector: {newSectorName}");
    }

    private bool IsSceneLoaded(string name)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).name == name) return true;
        }
        return false;
    }
}
