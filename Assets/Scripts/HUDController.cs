using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    public CanvasGroup Scrim;
    public Text ScrimLabel;
    public PlayerController Player;
    public TextController ChatText;
    bool isEnding;
    public bool IsEnding => isEnding;
    private Transform textMount;

    static HUDController instance;
    public static HUDController Instance => instance;

    void Awake()
    {
        if (instance != null) {
            GameObject.DestroyImmediate(this);
            return;
        }
        instance = this;
        GameObject.DontDestroyOnLoad(this.gameObject);
        
    }

    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public void ShowText(Transform mount, string text)
    {
        this.ChatText.mount = mount;
        this.ChatText.text.text = text;
    }

    // called second
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("OnSceneLoaded: " + scene.name);
        StartCoroutine(WaitToHide(0.1f));
    }

    IEnumerator WaitToHide(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        Hide();
    }
    
    public void Hide()
    {
        Scrim.alpha = 0;
        isEnding = false;
    }

    public void TriggerTheEndSequence()
    {
        isEnding = true;
        Scrim.alpha = 0;
        ScrimLabel.text = "The End";
        ScrimLabel.gameObject.SetActive(false);
        StartCoroutine(TheEndSequence());
    }

    IEnumerator TheEndSequence()
    {
        while (Scrim.alpha < 1)
        {
            Scrim.alpha += 0.05f;
            yield return 0;
        }

        ScrimLabel.gameObject.SetActive(true);

        yield return new WaitForSeconds(1.3f);
        ScrimLabel.text = "";
        yield return new WaitForSeconds(0.3f);
        ScrimLabel.text = "...";
        yield return new WaitForSeconds(0.5f);
        ScrimLabel.text = "or not...";

        yield return new WaitForSeconds(0.5f);
        Player.Reset(true);
    }

}
