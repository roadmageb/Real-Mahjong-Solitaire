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
    public static Score CalculateScore(MahjongPattern pt, MahjongPlayer pl, List<MahjongCard> doraIndicators)
    {
        var ret = rule.Select(x => x(pt, pl)).Aggregate((a, b) => a + b);
        if (ret.fan > 0) ret += DoraScore(pt, doraIndicators);
        return ret;
    }

    #region 1Fan
    public static Score STsumo(MahjongPattern pt, MahjongPlayer pl) => !pt.isRon && pt.isMenzen ? (1, "Self-pick") : 0;
    public static Score SPeace(MahjongPattern pt, MahjongPlayer pl)
    {
        if (pt is MahjongPatternSevenPairs or MahjongPatternThirteenOrphans) return 0;
        var buCheck = new ScoreRules(new ScoreRule[] { SHeadBu, SBodyBu, SWaitBu });
        return pt.isMenzen && 
            buCheck
            .Select(f => f(pt, pl))
            .Aggregate((a, b) => a + b)
            .bu == 0 ? (1, "No-points Hand") : 0;
    }
    public static Score SStraightPairs(MahjongPattern pt, MahjongPlayer pl)
    {
        if (!pt.isMenzen) return (0, 0);
        int pair = pt.sets
            .Where(x => x is MahjongSetBody body && body.bodyType == BodyType.Straight)
            .Select(x => (x.color, x.minNum))
            .GroupBy(x => x).Count(g => g.Count() > 1);
        return pair switch { 1 => (1, "One Set of Identical Sequences"), 2 => (3, "Two Sets of Identical Sequences"), _ => 0 };
    }
    public static Score SHonor(MahjongPattern pt, MahjongPlayer pl)
    {
        Score ret = 0;
        int dragonMask = 0;
        int windMask = 0;
        foreach (var set in pt.sets.Where(x => x is MahjongSetBody body && body.bodyType == BodyType.Triple))
        {
            if (set.color == CardColor.Dragon)
            {
                ret += (1, "Honor Tiles");
                dragonMask |= 1 << set.minNum;
            }
            if (set.color == CardColor.Wind)
            {
                if (set[0].num == pl.myWind) ret += (1, "Honor Tiles");
                if (set[0].num == 0) ret += (1, "Honor Tiles");
                windMask |= 1 << set.minNum;
            }
        }
        if (dragonMask == 0b111) return Score.Yakuman(1, "Big Three Dragons");
        else {
            if (dragonMask is 0b011 or 0b101 or 0b110)
            {
                foreach (int num in pt.sets.Where(x => x is MahjongSetHead && x.color == CardColor.Dragon).Select(x => x.minNum)) dragonMask |= num;
                if (dragonMask == 0b111) ret += (2, "Little Three Dragons");
            }
        }
        if (windMask == 0b1111) return Score.Yakuman(2, "Big Four Winds");
        else
        {
            if (dragonMask is 0b0111 or 0b101 or 0b1101 or 0b1110)
            {
                foreach (int num in pt.sets.Where(x => x is MahjongSetHead && x.color == CardColor.Wind).Select(x => x.minNum)) windMask |= num;
                if (dragonMask == 0b1111) Score.Yakuman(1, "Little Four Winds");
            }
        }
        return ret;
    }
    public static Score SNoTerminalHonor(MahjongPattern pt, MahjongPlayer pl) => pt.sets.Count(x => x.containsTerminalHonor) == 0 ? (1, "All Simples") : 0;
    #endregion

    public static Score STerminalHonor(MahjongPattern pt, MahjongPlayer pl)
    {
        if (pt is MahjongPatternThirteenOrphans)
        {
            if (pt.sets.Where(x => x.cardSet.Contains(pt.lastCard)).ToList()[0] is MahjongSetTerminalHonorPair) return Score.Yakuman(2, "Pure Thirteen Orphans");
            else return Score.Yakuman(1, "Thirteen Orphans");
        }
        if (pt.sets.Count(x => x.isColorHonorCard) == pt.sets.Count)
        {
            return pt switch
            {
                MahjongPatternStandard => Score.Yakuman(1, "All Honors"),
                MahjongPatternSevenPairs => Score.Yakuman(1, "Seven Stars"),
                _ => 0
            };
        }
        if (pt.sets.Sum(x => x.cardSet.Count(y => !y.isTerminal)) == 0) return Score.Yakuman(1, "All Terminals");
        if (pt.sets.Count(x => !(x.containsTerminalHonor && x.isColorNumCard)) == 0) return pt.isMenzen ? (3, "Terminal in each set") : (2, "Terminal in each set");
        if (pt.sets.Sum(x => x.cardSet.Count(y => !(y.isTerminal || y.isHonor))) == 0) return (2, "All Terminals and Honors");
        if (pt.sets.Count(x => !x.containsTerminalHonor) == 0) return pt.isMenzen ? (2, "Terminal or Honor in each set") : (1, "Terminal or Honor in each set");
        return 0;
    }
    public static Score STriples(MahjongPattern pt, MahjongPlayer pl)
    {
        if (pt is not MahjongPatternStandard) return 0;
        Score ret = 0;
        ret += pt.sets.Count(x => x is MahjongSetBody body && body.bodyType == BodyType.Quad) switch { 4 => Score.Yakuman(1, "Four Quads"), 3 => (2, "Three Quads"), _ => 0 };
        int hideCount = pt.sets.Count(x => x is MahjongSetBody body && body.bodyType is BodyType.Quad or BodyType.Triple && !body.isHuro && !(body.cardSet.Contains(pt.lastCard) && pt.isRon));
        if (hideCount == 4)
        {
            if (pt.sets.Count(x => x is MahjongSetHead && x.cardSet.Contains(pt.lastCard)) > 0) ret += Score.Yakuman(2, "Pure Four Closed Triplets");
            else ret += Score.Yakuman(1, "Four Closed Triplets");
        }
        else if (hideCount == 3)
        {
            ret += (2, "Three Closed Triplets");
        }
        ret += pt.sets.Count(x => x is MahjongSetBody body && body.isDup) == 4 ? (2, "All Triple Hand") : 0;
        return ret;
    }
    public static Score SSameTriples(MahjongPattern pt, MahjongPlayer pl)
    {
        if (pt is not MahjongPatternStandard) return 0;
        int[] mask = new int[9];
        foreach (var set in pt.sets.Where(x => x is MahjongSetBody body && body.isDup && x.isColorNumCard)) mask[set.minNum - 1] |= 1 << (set.color - CardColor.Crak);
        return mask.Count(x => x == 0b111) > 0 ? (2, "Three Color Triplets") : 0;
    }
    public static Score SStraight(MahjongPattern pt, MahjongPlayer pl)
    {
        int mask = 0;
        foreach (int m in pt.sets
            .Where(x => x is MahjongSetBody body && body.bodyType == BodyType.Straight)
            .Select(x => (x.color - CardColor.Crak) * 7 + x.minNum - 1))
        {
            mask |= 1 << m;
        }
        if (new int[] { 0x49 << 14, 0x49 << 7, 0x49 }.Count(x => (mask & x) == x) > 0) return pt.isMenzen ? (2, "Straight") : (1, "Straight");
        if (((mask >> 14) & (mask >> 7) & mask) > 0) return pt.isMenzen ? (2, "Three Color Straight") : (1, "Three Color Straight");
        return 0;
    }
    public static Score SSevenHead(MahjongPattern pt, MahjongPlayer pl) => pt is MahjongPatternSevenPairs ? (2, 25, "Seven Pairs") : 0;
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
        if (mask is 0b0010 or 0b0100 or 0b1000) return pt.isMenzen ? (6, "Flush") : (5, "Flush");
        if (mask is 0b0011 or 0b0101 or 0b1001) return pt.isMenzen ? (3, "Half-Flush") : (2, "Half-Flush");
        return 0;
    }
    public static Score SGreenOnly(MahjongPattern pt, MahjongPlayer pl)
    {
        if (pt.sets
            .Select(x => x.cardSet.Count(y => (y.color, y.num) is not (CardColor.Bam, 2) or (CardColor.Bam, 3) or (CardColor.Bam, 4) or (CardColor.Bam, 6) or (CardColor.Bam, 8) or (CardColor.Dragon, 1)))
            .Aggregate((a, b) => a + b) == 0) return Score.Yakuman(1, "All Green");
        return 0;
    }
    public static Score SNine(MahjongPattern pt, MahjongPlayer pl)
    {
        if (SSameNumCard(pt, pl).fan != 6) return 0;
        int[] counter = new int[] { 3, 1, 1, 1, 1, 1, 1, 1, 3 };
        foreach (var set in pt.sets) foreach (var card in set.cardSet) counter[card.num - 1]--;
        if (counter.Count(x => x == -1) == 1) return counter[pt.lastCard.num - 1] == -1 ? Score.Yakuman(2, "Pure Nine Gates") : Score.Yakuman(1, "Nine Gates");
        return 0;
    }
    #region Bu
    public static Score SBaseBu(MahjongPattern pt, MahjongPlayer pl) => pt is MahjongPatternSevenPairs ? 0 : (0, pt.isRon && pt.isMenzen ? 30 : 20);
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

    public static Score DoraScore(MahjongPattern pt, List<MahjongCard> doraIndicators)
    {
        int count = 0;
        var doraList = doraIndicators.Select(x => x.IndicatingCard).ToList();
        foreach (var set in pt.sets) foreach (var card in set.cardSet)
            {
                if (card.isRed) count++;
                count += doraList.Count(x => x * card == 0);
            }
        return (count, $"Dora {count}");
    }
}

public struct Score
{
    public int fan;
    public int bu;
    public int yakuman;
    public bool isSevenHead;
    public string fanString;
    public string yakumanString;

    public static implicit operator Score((int fan, int bu)tuple) => new Score { fan = tuple.fan, bu = tuple.bu};
    public static implicit operator Score((int fan, int bu, string name)tuple) => new Score { fan = tuple.fan, bu = tuple.bu, fanString = tuple.name };
    public static implicit operator Score((int fan, string name) tuple) => new Score { fan = tuple.fan, fanString = tuple.name };
    public static implicit operator Score(int fan) => new Score { fan = fan };
    public static Score operator +(Score a, Score b)
    {
        return new Score 
        { 
            fan = a.fan + b.fan, 
            bu = a.bu + b.bu, 
            yakuman = a.yakuman + b.yakuman, 
            isSevenHead = a.isSevenHead || b.isSevenHead, 
            fanString = a.fanString is "" or null || b.fanString is "" or null ? a.fanString + b.fanString : $"{a.fanString},{b.fanString}", 
            yakumanString = a.yakumanString is "" or null || b.yakumanString is "" or null ? a.yakumanString + b.yakumanString : $"{a.yakumanString},{b.yakumanString}" 
        };
    }
    public static Score Yakuman(int num, string name) => new Score() { yakuman = num, yakumanString = name};
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
    public override string ToString() => yakuman > 0 ? yakumanString : fanString;
}