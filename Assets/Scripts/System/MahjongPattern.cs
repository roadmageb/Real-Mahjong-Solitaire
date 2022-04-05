using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MahjongPattern
{
    public List<MahjongSet> sets = new List<MahjongSet>();
    public MahjongCard lastCard;
    public bool isRon;

    public bool isMenzen
    {
        get
        {
            foreach (var set in sets) if (set.isHuro) return false;
            return true;
        }
    }
    public int leftCardsCount
    {
        get
        {
            int n = 0;
            foreach (var set in sets) n += set.leftCard;
            return n;
        }
    }
    public int cardsCount => sets.Sum(x => x.cardSet.Count);
    public override string ToString()
    {
        string ret = "";
        foreach (var set in sets) ret += set;
        return ret;
    }
    public MahjongPattern Clone()
    {
        MahjongPattern ret = Activator.CreateInstance(GetType()) as MahjongPattern;
        ret.sets = new List<MahjongSet>();
        foreach (var set in sets) ret.sets.Add(set.Clone());
        ret.lastCard = lastCard;
        ret.isRon = isRon;
        return ret;
    }
}

public class MahjongPatternStandard : MahjongPattern
{
    public MahjongPatternStandard()
    {
        sets.Add(new MahjongSetHead());
        Loop.N(4, i => sets.Add(new MahjongSetBody()));
    }
}
public class MahjongPatternSevenPairs : MahjongPattern
{
    public MahjongPatternSevenPairs()
    {
        Loop.N(7, i => sets.Add(new MahjongSetHead()));
    }
}
public class MahjongPatternThirteenOrphans : MahjongPattern
{
    public MahjongPatternThirteenOrphans()
    {
        sets.Add(new MahjongSetTerminalHonorPair());
        Loop.N(12, i => sets.Add(new MahjongSetTerminalHonor()));
    }
}