using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MahjongCard
{
    public readonly int id;
    public CardColor color;
    public int num;
    public bool isRed;

    public bool isNum => color is >= CardColor.Crak and <= CardColor.Bam;
    public bool isHonor => color is CardColor.Dragon or CardColor.Wind;
    public bool isTerminal => isNum && (num is 1 or 9);
    public bool isTerminalHonor => isTerminal || isHonor;

    public bool isScoreHonor(MahjongPlayer pl) => color == CardColor.Dragon || color == CardColor.Wind && (num == pl.myWind || num == 0);

    public MahjongCard(CardColor cardColor, int cardNum, bool red = false)
    {
        color = cardColor;
        num = cardNum;
        isRed = red;
    }

    public MahjongCard(int idNum, CardColor cardColor, int cardNum, bool red = false)
    {
        id = idNum;
        color = cardColor;
        num = cardNum;
        isRed = red;
    }

    public override int GetHashCode() => color switch
    {
        CardColor.Crak      => num - 1,
        CardColor.Dot       => num + 8,
        CardColor.Bam       => num + 17,
        CardColor.Dragon    => num + 27,
        CardColor.Wind      => num + 30,
        _                   => 40,
    } * 2 + (isRed ? 1 : 0);
    public override string ToString() => (color, num) switch
    {
        (CardColor.Crak, _)     => $"[Crak {num}]",
        (CardColor.Dot, _)      => $"[Dot {num}]",
        (CardColor.Bam, _)      => $"[Bam {num}]",
        (CardColor.Dragon, 0)   => "[White]",
        (CardColor.Dragon, 1)   => "[Green]",
        (CardColor.Dragon, 2)   => "[Red]",
        (CardColor.Wind, 0)     => "[East]",
        (CardColor.Wind, 1)     => "[South]",
        (CardColor.Wind, 2)     => "[West]",
        (CardColor.Wind, 3)     => "[North]",
        _ => "[]",
    };
    public static int operator *(MahjongCard a, MahjongCard b)
    {
        if (a.color != b.color) return 10;
        else return a.color switch
        {
            CardColor.Dragon or CardColor.Wind => a.num == b.num ? 0 : 10,
            CardColor.Bam or CardColor.Dot or CardColor.Crak => Mathf.Abs(a.num - b.num),
            _ => 10
        };
    }
    public bool Equals(CardColor color, int num, bool isRed) => color == this.color && num == this.num && isRed == this.isRed;
}

public enum CardColor
{
    Unknown = 0,
    Crak = 1,
    Dot = 2,
    Bam = 3,
    Wind = 4,
    Dragon = 5,
    Etc = 6,
}

public static class IDGenerator
{
    private static int id = 0;
    public static int NewID()
    {
        id++;
        id %= 10000;
        return id;
    }
}