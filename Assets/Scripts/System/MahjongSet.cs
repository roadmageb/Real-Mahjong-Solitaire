using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MahjongSet
{
    public List<MahjongCard> cardSet = new List<MahjongCard>();
    public (MahjongCard card, int from)? cardFrom = null;
    public virtual SetType type => SetType.Unknown;
    public virtual int leftCard => 10;

    public MahjongCard this[int idx] => cardSet[idx];

    public bool isHuro => cardFrom != null;
    public int count => cardSet.Count;
    public int minNum => cardSet.Min(x => x.num);
    public bool containsTerminalHonor => cardSet.Count(x => x.isTerminalHonor) > 0;
    public bool isColorNumCard => color is CardColor.Crak or CardColor.Dot or CardColor.Bam;
    public bool isColorHonorCard => color is CardColor.Dragon or CardColor.Wind;
    public CardColor color
    {
        get
        {
            if (count == 0 || cardSet.Count(x => x.color != cardSet[0].color) > 0) return CardColor.Unknown;
            return cardSet[0].color;
        }
    }
    public virtual int MaskCode(MahjongCard card) => 0;
    public virtual bool CheckSuitability(MahjongCard card) => false;
    public virtual void AddCard(MahjongCard card) => cardSet.Add(card);
    public virtual void RemoveCard(MahjongCard card) => cardSet.Remove(card);
    public override string ToString()
    {
        string ret = "";
        foreach (var card in cardSet) ret += card;
        return $"{{{ret}}}";
    }
    public virtual MahjongSet Clone()
    {
        MahjongSet ret = Activator.CreateInstance(GetType()) as MahjongSet;
        foreach (var card in cardSet) ret.cardSet.Add(card);
        ret.cardFrom = cardFrom;
        return ret;
    }
}

public class MahjongSetBody : MahjongSet
{
    public BodyType bodyType = BodyType.Unknown;
    public override int leftCard => bodyType == BodyType.Quad ? 4 - count : 3 - count;
    public override SetType type => SetType.Body;

    public bool isDup => bodyType is BodyType.Triple or BodyType.Quad;
    public override int MaskCode(MahjongCard card) => count == 0 ? 1 << (int)SetType.Body : 0;
    public override bool CheckSuitability(MahjongCard card)
    {
        if (count == 0) return true;
        else if (count == 1) return this[0] * card < 3;
        else if (count == 2)
        {
            if (this[0] * this[1] == 0) return this[0] * card == 0;
            else if (this[0] * this[1] == 1) return this[0] * card + this[1] * card == 3;
            else if (this[0] * this[1] == 2) return this[0] * card == 1 && this[1] * card == 1;
        }
        return false;
    }

    public override void AddCard(MahjongCard card)
    {
        cardSet.Add(card);
        if (count == 4) bodyType = BodyType.Quad;
        else if (count > 2)
        {
            if (this[0] * this[1] == 0) bodyType = BodyType.Triple;
            else bodyType = BodyType.Straight;
        }
    }

    public override void RemoveCard(MahjongCard card)
    {
        cardSet.Remove(card);
        if (count == 4) bodyType = BodyType.Quad;
        else if (count > 2)
        {
            if (this[0] * this[1] == 0) bodyType = BodyType.Triple;
            else bodyType = BodyType.Straight;
        }
        else bodyType = BodyType.Unknown;
    }

    public override MahjongSet Clone()
    {
        MahjongSetBody ret = base.Clone() as MahjongSetBody;
        ret.bodyType = bodyType;
        return ret;
    }
}
public class MahjongSetHead : MahjongSet
{
    public override int leftCard => 2 - count;
    public override SetType type => SetType.Head;
    public override int MaskCode(MahjongCard card) => (count == 0 || this[0] * card == 0) ? 1 << (int)SetType.Head : 0;
    public override bool CheckSuitability(MahjongCard card) => count == 0 || this[0] * card == 0 && leftCard > 0;
}
public class MahjongSetTerminalHonor : MahjongSet
{
    public override int leftCard => 1 - count;
    public override SetType type => SetType.TerminalHonor;
    public override int MaskCode(MahjongCard card) => (count == 0 || this[0] * card == 0) ? 1 << (int)SetType.TerminalHonor: 0;
    public override bool CheckSuitability(MahjongCard card) => card.isTerminalHonor && count == 0;
}

public class MahjongSetTerminalHonorPair : MahjongSet
{
    public override int leftCard => 2 - count;
    public override SetType type => SetType.TerminalHonorPair;
    public override int MaskCode(MahjongCard card) => (count == 0 || this[0] * card == 0) ? 1 << (int)SetType.TerminalHonorPair : 0;
    public override bool CheckSuitability(MahjongCard card) => card.isTerminalHonor && count == 0 || count == 1 && this[0] * card == 0;
}
public enum SetType
{
    Unknown,
    Head,
    Body,
    TerminalHonor,
    TerminalHonorPair,
}

public enum BodyType
{
    Unknown,
    Straight,
    Triple,
    Quad,
}