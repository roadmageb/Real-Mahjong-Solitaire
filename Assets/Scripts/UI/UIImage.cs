using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIImage : MonoBehaviour
{
    public Sprite[] sprites;
    public void Set(Sprite sprite) => GetComponent<Image>().sprite = sprite;
    public void Set(int idx) => GetComponent<Image>().sprite = sprites[idx];
}
