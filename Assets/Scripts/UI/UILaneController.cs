using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UILaneController : MonoBehaviour
{
    public UIMouseInteraction button;
    public RectTransform cardParent;
    [HideInInspector] public UIImage[] cardImage;
    [HideInInspector] public UIMouseInteraction[] quadButton;
    public bool isReady;
    public Text propertyText, readyScoreText;
}
