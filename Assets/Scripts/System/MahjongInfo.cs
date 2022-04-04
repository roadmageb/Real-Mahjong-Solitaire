using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ScoreRules = System.Collections.Generic.List<System.Func<MahjongPattern, MahjongPlayer, Score>>;
using ScoreRule = System.Func<MahjongPattern, MahjongPlayer, Score>;
using System.Linq;

public static class MahjongInfo
{
    private static List<MahjongCard> _cardList;
    public static List<MahjongCard> cardList
    {
        get
        {
            if (_cardList == null)
            {
                _cardList = new List<MahjongCard>();
                Loop.N(3, 9, (i, j) => _cardList.Add(new MahjongCard((CardColor)i + 1, j + 1)));
                Loop.N(4, i => _cardList.Add(new MahjongCard(CardColor.Wind, i)));
                Loop.N(3, i => _cardList.Add(new MahjongCard(CardColor.Dragon, i)));
            }
            return _cardList;
        }
    }
    public static ScoreRules _rule;
    public static ScoreRules rule
    {
        get
        {
            if (_rule == null)
            {
                _rule = typeof(MahjongInfo).GetMethods().Where(x => x.Name[0] == 'S').Select(x => (ScoreRule)((pt, pl) => (Score)x.Invoke(null, new object[] { pt, pl }))).ToList();
            }
            return _rule;
        }
    }
    public static Score CalculateScore(MahjongPattern pt, MahjongPlayer pl) => rule.Select(x => x(pt, pl)).Aggregate((a, b) => a + b);

    #region 1Fan
    public static Score STsumo(MahjongPattern pt, MahjongPlayer pl) => !pt.isRon && pt.isMenzen ? 1 : 0;
    public static Score SPeace(MahjongPattern pt, MahjongPlayer pl)
    {
        var buCheck = new ScoreRules(new ScoreRule[] { SHeadBu, SBodyBu, SWaitBu });
        return pt.isMenzen && 
            buCheck
            .Select(f => f(pt, pl))
            .Aggregate((a, b) => a + b)
            .bu == 0 ? 1 : 0;
    }
    public static Score SStraightPairs(MahjongPattern pt, MahjongPlayer pl)
    {
        if (!pt.isMenzen) return (0, 0);
        int pair = pt.sets
            .Where(x => x is MahjongSetBody body && body.bodyType == BodyType.Straight)
            .Select(x => (x.color, x.minNum))
            .GroupBy(x => x).Count(g => g.Count() > 1);
        return pair switch { 1 => 1, 2 => 3, _ => 0 };
    }
    public static Score SHonor(MahjongPattern pt, MahjongPlayer pl)
    {
        int fan = 0;
        int dragonMask = 0;
        int windMask = 0;
        foreach (var set in pt.sets.Where(x => x is MahjongSetBody body && body.bodyType == BodyType.Triple))
        {
            if (set.color == CardColor.Dragon)
            {
                fan += 1;
                dragonMask |= 1 << set.minNum;
            }
            if (set.color == CardColor.Wind)
            {
                if (set[0].num == pl.myWind) fan += 1;
                if (set[0].num == 0) fan += 1;
                windMask |= 1 << set.minNum;
            }
        }
        if (dragonMask == 0b111) return Score.Yakuman(1);
        else {
            if (dragonMask is 0b011 or 0b101 or 0b110)
            {
                foreach (int num in pt.sets.Where(x => x is MahjongSetHead && x.color == CardColor.Dragon).Select(x => x.minNum)) dragonMask |= num;
                if (dragonMask == 0b111) fan += 2;
            }
        }
        if (windMask == 0b1111) return Score.Yakuman(2);
        else
        {
            if (dragonMask is 0b0111 or 0b101 or 0b1101 or 0b1110)
            {
                foreach (int num in pt.sets.Where(x => x is MahjongSetHead && x.color == CardColor.Wind).Select(x => x.minNum)) windMask |= num;
                if (dragonMask == 0b1111) Score.Yakuman(1);
            }
        }
        return fan;
    }
    public static Score SNoTerminalHonor(MahjongPattern pt, MahjongPlayer pl) => pt.sets.Count(x => x.containsTerminalHonor) == 0 ? 1 : 0;
    #endregion

    public static Score STerminalHonor(MahjongPattern pt, MahjongPlayer pl)
    {
        if (pt is MahjongPatternThirteenOrphans)
        {
            if (pt.sets.Where(x => x.cardSet.Contains(pt.lastCard)).ToList()[0] is MahjongSetTerminalHonorPair) return Score.Yakuman(2);
            else return Score.Yakuman(1);
        }
        if (pt.sets.Count(x => x.isColorHonorCard) == pt.sets.Count)
        {
            return pt switch
            {
                MahjongPatternStandard => Score.Yakuman(1),
                MahjongPatternSevenPairs => Score.Yakuman(1),
                _ => 0
            };
        }
        if (pt.sets.Sum(x => x.cardSet.Count(y => !y.isTerminal)) == 0) return Score.Yakuman(1);
        if (pt.sets.Count(x => !(x.containsTerminalHonor && x.isColorNumCard)) == 0) return pt.isMenzen ? 3 : 2;
        if (pt.sets.Sum(x => x.cardSet.Count(y => !(y.isTerminal || y.isHonor))) == 0) return 2;
        if (pt.sets.Count(x => !x.containsTerminalHonor) == 0) return pt.isMenzen ? 2 : 1;
        return 0;
    }
    public static Score STriples(MahjongPattern pt, MahjongPlayer pl)
    {
        if (pt is not MahjongPatternStandard) return 0;
        Score ret = 0;
        ret += pt.sets.Count(x => x is MahjongSetBody body && body.bodyType == BodyType.Quad) switch { 4 => Score.Yakuman(1), 3 => 2, _ => 0 };
        int hideCount = pt.sets.Count(x => x is MahjongSetBody body && body.bodyType is BodyType.Quad or BodyType.Triple && !body.isHuro && !(body.cardSet.Contains(pt.lastCard) && pt.isRon));
        if (hideCount == 4)
        {
            if (pt.sets.Count(x => x is MahjongSetHead && x.cardSet.Contains(pt.lastCard)) > 0) ret += Score.Yakuman(2);
            else Score.Yakuman(1);
        }
        ret += pt.sets.Count(x => x is MahjongSetBody body && body.bodyType is BodyType.Quad or BodyType.Triple) == 4 ? 2 : 0;
        return ret;
    }
    public static Score SStraight(MahjongPattern pt, MahjongPlayer pl)
    {
        int mask = 0;
        foreach (int m in pt.sets
            .Where(x => x is MahjongSetBody body && body.bodyType == BodyType.Straight && x.minNum % 3 == 1)
            .Select(x => ((int)x.color - (int)CardColor.Crak) * 3 + x.minNum / 3))
        {
            mask |= 1 << m;
        }
        if (new int[] { 0700, 0070, 0007 }.Count(x => (mask & x) == x) > 0) return pt.isMenzen ? 2 : 1;
        if (new int[] { 0111, 0222, 0444 }.Count(x => (mask & x) == x) > 0) return pt.isMenzen ? 2 : 1;
        return 0;
    }
    public static Score SSevenHead(MahjongPattern pt, MahjongPlayer pl) => pt is MahjongPatternSevenPairs ? (2, 25) : 0;
    public static Score SSameNumCard(MahjongPattern pt, MahjongPlayer pl)
    {
        int mask = 0;
        foreach (var set in pt.sets)
        {
            foreach (var card in set.cardSet)
            {
                if (card.isNum) mask |= 1 << (int)card.color;
                if (card.isHonor) mask |= 1;
            }
        }
        if (mask is 0b0010 or 0b0100 or 0b1000) return pt.isMenzen ? 6 : 5;
        if (mask is 0b0011 or 0b0101 or 0b1001) return pt.isMenzen ? 3 : 2;
        return 0;
    }

    #region Bu
    public static Score SBaseBu(MahjongPattern pt, MahjongPlayer pl) => (0, pt.isRon && pt.isMenzen ? 30 : 20);
    public static Score SBodyBu(MahjongPattern pt, MahjongPlayer pl)
    {
        int bu = 0;
        foreach (MahjongSet set in pt.sets)
        {
            if (set is MahjongSetBody body)
            {
                int subBu = body.bodyType switch
                {
                    BodyType.Quad => 8,
                    BodyType.Triple => 2,
                    _ => 0
                };
                if (!set.isHuro && !(set.cardSet.Contains(pt.lastCard) && pt.isRon)) subBu *= 2;
                if (set[0].isTerminalHonor) subBu *= 2;
                bu += subBu;
            }
        }
        return (0, bu);
    }
    public static Score SHeadBu(MahjongPattern pt, MahjongPlayer pl)
    {
        if (pt is MahjongPatternSevenPairs) return (0, 0);
        int bu = 0;
        foreach (MahjongSet set in pt.sets.Where(x => x is MahjongSetHead))
        {
            if (set.color == CardColor.Dragon) bu += 2;
            if (set.color == CardColor.Wind)
            {
                if (set[0].num == pl.myWind) bu += 2;
                if (set[0].num == 0) bu += 2;
            }
        }
        return (0, bu);
    }
    public static Score SWaitBu(MahjongPattern pt, MahjongPlayer pl)
    {
        if (pt is not MahjongPatternStandard) return (0, 0);
        foreach (var set in pt.sets)
        {
            if (set.cardSet.Contains(pt.lastCard))
            {
                if (set is MahjongSetHead) return (0, 2);
                if (set is MahjongSetBody body && body.bodyType == BodyType.Straight)
                {
                    return (body.minNum, pt.lastCard.num) switch
                    {
                        (1, 3) or (7, 7) => (0, 2),
                        _ => (0, 0)
                    };
                }
                return (0, 0);
            }
        }
        return (0, 0);
    }
    #endregion
}

public struct Score
{
    public int fan;
    public int bu;
    public int yakuman;
    public bool isSevenHead;

    public static implicit operator Score((int fan, int bu)tuple) => new Score { fan = tuple.fan, bu = tuple.bu };
    public static implicit operator Score(int fan) => new Score { fan = fan, bu = 0 };
    public static Score operator +(Score a, Score b) => new Score { fan = a.fan + b.fan, bu = a.bu + b.bu, yakuman = a.yakuman + b.yakuman, isSevenHead = a.isSevenHead || b.isSevenHead};
    public static Score Yakuman(int num) => new Score() { yakuman = num };
    public int CalcSum(bool ron, bool oya)
    {
        var tmp = Calc(ron, oya);
        return (ron, oya) switch
        {
            (true, _) => tmp.other,
            (false, true) => tmp.other * 3,
            (false, false) => tmp.oya + tmp.other * 2,
        };
    }
    public (int oya, int other) Calc(bool ron, bool oya)
    {
        int reBu = bu + (bu % 10 != 0 && !isSevenHead ? 10 - bu % 10 : 0);
        int tmp = (fan, reBu) switch
        {
            ( >= 13, _)     => 8000,
            ( >= 11, _)     => 6000,
            ( >= 8, _)      => 4000,
            ( >= 6, _)      => 3000,
            (5, _) or
            (4, >= 40) or
            (3, >= 70)      => 2000,
            (0, _)          => 0,
            _               => bu * (1 << (fan + 2))
        };
        if (yakuman > 0) tmp = yakuman * 8000;

        return (ron, oya) switch
        {
            (true, true) => (0, Ceil100(tmp * 6)),
            (true, false) => (0, Ceil100(tmp * 4)),
            (false, true) => (0, Ceil100(tmp * 2)),
            (false, false) => (Ceil100(tmp * 2), Ceil100(tmp)),
        };
    }

    public int Ceil100(int point) => Mathf.CeilToInt(point / 100f) * 100;
}