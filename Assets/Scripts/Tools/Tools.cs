using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Loop
{
    public static void N(int i, Action<int> action)
    {
        for (int a = 0; a < i; a++) action(a);
    }
    public static void N(int i, int j, Action<int, int> action)
    {
        for (int a = 0; a < i; a++) for (int b = 0; b < j; b++) action(a, b);
    }
}