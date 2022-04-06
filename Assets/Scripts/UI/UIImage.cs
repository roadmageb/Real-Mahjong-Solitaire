using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIImage : MonoBehaviour
{
    public Sprite[] sprites;
    public Color[] colors;
    public void SetImage(Sprite sprite) => GetComponent<Image>().sprite = sprite;
    public void SetImage(int idx) => GetComponent<Image>().sprite = sprites[idx];

    public void SetColor(Color color) => GetComponent<Image>().color = color;
    public void SetColor(int idx) => GetComponent<Image>().color = colors[idx];

    public void Set(int sprtIdx, int colorIdx)
    {
        SetImage(sprtIdx);
        SetColor(colorIdx);
    }
    public void Set(Sprite sprite, int colorIdx)
    {
        SetImage(sprite);
        SetColor(colorIdx);
    }
}
