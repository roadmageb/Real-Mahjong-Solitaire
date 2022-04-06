using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIMouseInteraction : MonoBehaviour, IPointerClickHandler
{
    protected Dictionary<MouseAction, List<Action>> actionDict = new Dictionary<MouseAction, List<Action>>();
    public int ActionCount(MouseAction mouse) => actionDict.ContainsKey(mouse) ? actionDict[mouse].Count : 0;
    public void ExecuteAction(MouseAction mouse)
    {
        if (actionDict.ContainsKey(mouse)) foreach (var action in actionDict[mouse].ToArray()) action();
    }

    private float lastClickTime = -1;
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (Time.time - lastClickTime <= .2f && ActionCount(MouseAction.DoubleClick) > 0)
            {
                lastClickTime = -1;
                ExecuteAction(MouseAction.DoubleClick);
            }
            else
            {
                lastClickTime = Time.time;
                ExecuteAction(MouseAction.LeftClick);
            }
        }
    }

    public void AddAction(MouseAction mouse, Action action)
    {
        if (!actionDict.ContainsKey(mouse)) actionDict.Add(mouse, new List<Action>());
        actionDict[mouse].Add(action);
    }

    public void RemoveAction(MouseAction mouse)
    {
        if (actionDict.ContainsKey(mouse)) actionDict[mouse].Clear();
    }

    public void ReplaceAction(MouseAction mouse, Action action)
    {
        RemoveAction(mouse);
        AddAction(mouse, action);
    }
}

public enum MouseAction
{
    LeftClick,
    DoubleClick,
}