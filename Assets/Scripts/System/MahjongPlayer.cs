using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MahjongPlayer
{
    public WindCounter myWind;
    public bool isRon;
    public List<MahjongCard> hand = new List<MahjongCard>();
    public List<MahjongCard> closedHand = new List<MahjongCard>();
    public List<MahjongSet> openHand = new List<MahjongSet>();

    public int leftCard => 14 - openHand.Count * 3 - closedHand.Count;
    public void ResetPlayer()
    {
        hand = new List<MahjongCard>();
        closedHand = new List<MahjongCard>();
        openHand = new List<MahjongSet>();
    }
    public virtual List<MahjongPattern> InitAvailablePatterns()
    {
        var ret = new List<MahjongPattern>();
        ret.Add(new MahjongPatternStandard());
        ret.Add(new MahjongPatternSevenPairs());
        ret.Add(new MahjongPatternThirteenOrphans());
        return ret;
    }
    public virtual (Score score, int point) CalculateBestScore(MahjongCard lastCard)
    {
        var patterns = InitAvailablePatterns();

        var bestPatternCandidate = new List<MahjongPattern>();

        foreach (var pattern in patterns)
        {
            int removeCount = 0;
            pattern.isRon = isRon;
            pattern.lastCard = lastCard;
            foreach (var openSet in openHand)
            {
                for (int i = 0; i < pattern.sets.Count; i++)
                {
                    if (pattern.sets[i].GetType() == openSet.GetType())
                    {
                        pattern.sets.RemoveAt(i);
                        removeCount++;
                        break;
                    }
                }
            }
            if (removeCount != openHand.Count) continue;

            foreach (var openSet in openHand) pattern.sets.Add(openSet);

            PerfectSetRecursive(pattern, closedHand, 0, bestPatternCandidate);
        }
        var best = bestPatternCandidate.Select(x => MahjongInfo.CalculateScore(x, this)).OrderByDescending(x => x.CalcSum(isRon, myWind == 0)).FirstOrDefault();

        return (best, best.CalcSum(isRon, myWind == 0));
    }
    protected void PerfectSetRecursive(MahjongPattern pattern, List<MahjongCard> hand, int num, List<MahjongPattern> perfectPatterns)
    {
        if (hand.Count <= num)
        {
            perfectPatterns.Add(pattern.Clone());
            return;
        }
        int mask = 0;
        foreach (var set in pattern.sets)
        {
            int afterMask = set.MaskCode(hand[num]);
            bool suit = set.CheckSuitability(hand[num]);
            if ((mask & (1 << (int)set.type)) == 0 && set.CheckSuitability(hand[num]))
            {
                set.AddCard(hand[num]);
                PerfectSetRecursive(pattern, hand, num + 1, perfectPatterns);
                set.RemoveCard(hand[num]);
            }
            mask |= afterMask;
        }
        return;
    }

    public void AddCard(MahjongCard card)
    {
        hand.Add(card);
        closedHand.Add(card);
    }

    public void MakeQuad(MahjongCard target)
    {
        var quad = new MahjongSetBody();
        quad.bodyType = BodyType.Quad;
        for (int i = 0; i < closedHand.Count; i++)
        {
            if (closedHand[i] * target == 0)
            {
                quad.cardSet.Add(closedHand[i]);
                closedHand.RemoveAt(i--);
            }
        }
        openHand.Add(quad);
    }

}
