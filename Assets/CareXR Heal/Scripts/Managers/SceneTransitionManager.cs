using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public FadeScreen fadeScreen;
    public static SceneTransitionManager singleton;

    private void Awake()
    {
        if (singleton && singleton != this)
            Destroy(singleton);

        singleton = this;
    }

    public void GoToScene(string sceneName, Action onLoadComplete = null)
    {
        StartCoroutine(GoToSceneRoutine(sceneName, onLoadComplete));

    }

    IEnumerator GoToSceneRoutine(string sceneName, Action onLoadComplete = null)
    {
        fadeScreen.FadeOut();
        yield return new WaitForSeconds(fadeScreen.fadeDuration);

        //Launch the new scene
        SceneManager.LoadScene(sceneName);
        onLoadComplete?.Invoke();
    }

    public void GoToSceneAsync(string sceneName, Action onLoadComplete = null)
    {
        StartCoroutine(GoToSceneAsyncRoutine(sceneName, onLoadComplete));
    }

    IEnumerator GoToSceneAsyncRoutine(string sceneName, Action onLoadComplete = null)
    {
        fadeScreen.FadeOut();
        //Launch the new scene
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        SessionManager.InStartScene = sceneName == "Start";
        Debug.Log("Is in Start Scene? " + SessionManager.InStartScene);

        operation.allowSceneActivation = false;

        float timer = 0;
        while(timer <= fadeScreen.fadeDuration && !operation.isDone)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        operation.allowSceneActivation = true;
        onLoadComplete?.Invoke();
    }
}
