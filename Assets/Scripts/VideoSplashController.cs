using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class VideoSplashController : MonoBehaviour
{
    private VideoPlayer videoPlayer;
    private bool sceneLoadingStarted = false;

    void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer != null)
        {
            videoPlayer.Play();
            StartCoroutine(TimeBasedFallback());
        }
    }

    IEnumerator TimeBasedFallback()
    {
        float timeout = 0f;

        while (videoPlayer.length == 0 && timeout < 3f)
        {
            timeout += Time.unscaledDeltaTime;
            yield return null;
        }

        float videoDuration = (float)videoPlayer.length;
        if (videoDuration <= 0)
            videoDuration = 5f;

        float elapsedTime = 0f;

        while (elapsedTime < videoDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        yield return new WaitForSecondsRealtime(2.5f);
        LoadNextScene();
    }

    void LoadNextScene()
    {
        if (!sceneLoadingStarted)
        {
            sceneLoadingStarted = true;
            Debug.Log("O tempo do vídeo acabou! A tentar carregar a cena: SampleScene");

            try
            {
                SceneManager.LoadScene("SampleScene");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Erro ao carregar a cena: " + e.Message);
            }
        }
    }
}