using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextController : MonoBehaviour
{
    RectTransform rectTransform;
    public Transform mount;
    public Text text;
    void Awake(){
        rectTransform = (RectTransform)transform;
    }

    public void PositionAt( Vector3 worldPos ){
        var viewportPoint =  Camera.main.WorldToViewportPoint(worldPos);
        rectTransform.anchorMax = viewportPoint;
        rectTransform.anchorMin = viewportPoint;
        rectTransform.anchoredPosition = Vector2.zero;
    }

    // Update is called once per frame
    void Update()
    {
        if (mount != null)
            PositionAt(mount.position);
    }
}
