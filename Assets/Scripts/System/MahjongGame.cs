using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[SelectionBase]
public class MahjongGame : MonoBehaviour
{
    public List<MahjongCard> nextCards;
    public MahjongPlayer[] lanes;
    public List<MahjongCard> deck;
    public List<MahjongCard> doraIndicators;

    private float tsumoProb;
    public float tsumoMaxProb, tsumoMinProb, tsumoDescent;

    private int _totalScore;
    private int totalScore
    {
        get => _totalScore;
        set
        {
            _totalScore = value;
            scoreText.text = value.ToString();
        }
    }

    private int _resetterCoolDown;
    private int resetterCoolDown
    {
        get => _resetterCoolDown;
        set
        {
            _resetterCoolDown = value > 0 ? value : 0;
            resetterCoolDownText.text = _resetterCoolDown.ToString();
            resetterCoolDownText.gameObject.SetActive(_resetterCoolDown != 0);
        }
    }
    public int initResetterCoolDown;

    private int makeDoraCoolDown
    {
        get => _makeDoraCoolDown;
        set
        {
            _makeDoraCoolDown = value > 0 ? value : 0;
            if (_makeDoraCoolDown == 0 && makeDoraNum < maxMakeDoraNum)
            {
                makeDoraNum++;
                if (makeDoraNum < maxMakeDoraNum) _makeDoraCoolDown = initMakeDoraCoolDown;
            }
            makeDoraCoolDownText.text = _makeDoraCoolDown.ToString();
            makeDoraCoolDownText.gameObject.SetActive(_makeDoraCoolDown != 0);
        }
    }

    private int _makeDoraCoolDown, makeDoraNum;

    public int initMakeDoraCoolDown, maxDoraNum, maxMakeDoraNum;

    public CardSpriteSetting spriteSetting;

    public UIMouseInteraction startButton, resetterButton;
    public UIImage skipButton;
    public Text scoreText, resetterCoolDownText;
    private int skipNum;

    public RectTransform doraIndicatorsParent;
    private UIImage[] doraIndicatorRenderer;
    public UIMouseInteraction makeDoraButton;
    public Text makeDoraCoolDownText;

    public UILaneController[] laneUI;
    public RectTransform nextCardTransform;
    private UIImage[] nextCardRenderer;
    public GameObject cardPrefab;
    public GameObject quadButtonPrefab;

    public GameObject gameParent;

    private enum State { Insert, Resetter, Dora}
    private State currentState;

    private void Start()
    {
        startButton.AddAction(MouseAction.LeftClick, () =>
        {
            ResetGame();
            startButton.gameObject.SetActive(false);
            gameParent.SetActive(true);
        });

        float gap = cardPrefab.GetComponent<RectTransform>().sizeDelta.x * 1.1f;
        
        Loop.N(4, i =>
        {
            laneUI[i].cardImage = new UIImage[18];
            Loop.N(18, j =>
            {
                var obj = Instantiate(cardPrefab, laneUI[i].cardParent).GetComponent<RectTransform>();
                obj.anchoredPosition = new Vector2(gap * j, 0);
                laneUI[i].cardImage[j] = obj.GetComponent<UIImage>();
            });
            laneUI[i].quadButton = new UIMouseInteraction[3];
            Loop.N(3, j =>
            {
                var obj = Instantiate(quadButtonPrefab, laneUI[i].cardParent).GetComponent<RectTransform>();
                obj.anchoredPosition = new Vector2(0, 0);
                obj.gameObject.SetActive(false);
                laneUI[i].quadButton[j] = obj.GetComponent<UIMouseInteraction>();
            });
        });

        nextCardRenderer = new UIImage[3];
        Loop.N(3, i =>
        {
            var obj = Instantiate(cardPrefab, nextCardTransform).GetComponent<RectTransform>();
            obj.anchoredPosition = new Vector2(gap * i, 0);
            nextCardRenderer[i] = obj.GetComponent<UIImage>();
        });

        doraIndicatorRenderer = new UIImage[maxDoraNum];
        Loop.N(maxDoraNum, i =>
        {
            var obj = Instantiate(cardPrefab, doraIndicatorsParent).GetComponent<RectTransform>();
            obj.anchoredPosition = new Vector2(gap * i, 0);
            doraIndicatorRenderer[i] = obj.GetComponent<UIImage>();
        });

        skipButton.GetComponent<UIMouseInteraction>().AddAction(MouseAction.LeftClick, () => SkipNextCards());

        gameParent.SetActive(false);
    }

    private void SetLaneButton()
    {
        if (currentState != State.Resetter)
        {
            Loop.N(4, i => 
            {
                if (!laneUI[i].isReady)
                {
                    laneUI[i].button.ReplaceAction(MouseAction.LeftClick, () => AddToLane(i));
                    laneUI[i].button.GetComponent<UIImage>().SetImage(0);
                }
            });
            resetterButton.GetComponent<UIImage>().SetImage(resetterCoolDown > 0 ? 0 : 1);
            resetterButton.ReplaceAction(MouseAction.LeftClick, () => { if (resetterCoolDown == 0) { currentState = State.Resetter; SyncUI(); } });
        }
        else
        {
            Loop.N(4, i =>
            {
                if (!laneUI[i].isReady)
                {
                    laneUI[i].button.ReplaceAction(MouseAction.LeftClick, () => UseResetter(i));
                    laneUI[i].button.GetComponent<UIImage>().SetImage(1);
                }
            });
            resetterButton.GetComponent<UIImage>().SetImage(2);
            resetterButton.ReplaceAction(MouseAction.LeftClick, () => { currentState = State.Insert; SyncUI(); });
        }
    }

    private void SetCardMakeDoraButton()
    {
        if (currentState != State.Dora)
        {
            makeDoraButton.GetComponent<UIImage>().SetImage(makeDoraNum);
            makeDoraButton.ReplaceAction(MouseAction.LeftClick, () => { if (makeDoraNum > 0) { currentState = State.Dora; SyncUI(); } });
        }
        else
        {
            makeDoraButton.GetComponent<UIImage>().SetImage(maxMakeDoraNum + 1);
            makeDoraButton.ReplaceAction(MouseAction.LeftClick, () => { currentState = State.Insert; SyncUI(); });
        }
    }
    public void SkipNextCards()
    {
        if (skipNum > 0)
        {
            deck.AddRange(nextCards);
            nextCards.Clear();
            ShuffleDeck();
            DrawNextCards(3);
            skipNum--;
            SyncUI();
        }
    }
    public void ShuffleDeck() => deck = deck.OrderBy(x => UnityEngine.Random.Range(0f, 1f)).ToList();
    public void ResetGame()
    {
        deck = new List<MahjongCard>();
        Loop.N(4, i =>
        {
            foreach (var card in MahjongInfo.cardList) deck.Add(new MahjongCard(card.color, card.num, card.num == 5 && i == 0));
        });
        UnityEngine.Random.InitState((int)(Time.time * 100f));
        ShuffleDeck();

        nextCards = new List<MahjongCard>();
        doraIndicators = new List<MahjongCard>();

        lanes = new MahjongPlayer[4];
        Loop.N(4, i =>
        {
            lanes[i] = new MahjongPlayer();
            lanes[i].myWind = i;
            lanes[i].ResetPlayer();
        });

        tsumoProb = tsumoMaxProb;
        skipNum = 3;
        totalScore = 0;
        resetterCoolDown = 0;
        makeDoraCoolDown = initMakeDoraCoolDown;
        makeDoraNum = 0;
        DrawNextCards(3);

        SyncUI();
    }
    public void DrawNextCards(int num)
    {
        Loop.N(num, i =>
        {
            nextCards.Add(deck[0]);
            deck.RemoveAt(0);
        });
    }
    public void UseResetter(int laneNum)
    {
        if (resetterCoolDown == 0)
        {
            ResetLane(laneNum);
            resetterCoolDown = initResetterCoolDown;
            currentState = State.Insert;
            SyncUI();
        }
    }
    public void ResetLane(int laneNum)
    {
        foreach (var card in lanes[laneNum].hand) deck.Add(card);
        lanes[laneNum].ResetPlayer();
        lanes[laneNum].isRon = UnityEngine.Random.Range(0f, 1f) > tsumoProb;
        ShuffleDeck();
    }

    public void AddToLane(int laneNum)
    {
        MahjongCard lastCard = null;

        if (lanes[laneNum].leftCard == 0) return;
        lanes[laneNum].AddCard(nextCards[0]);
        lastCard = nextCards[0];
        nextCards.RemoveAt(0);
        resetterCoolDown--;
        makeDoraCoolDown--;

        if (lanes[laneNum].leftCard == 0)
        {
            var score = lanes[laneNum].CalculateBestScore(lastCard, doraIndicators);
            Debug.Log($"{score.score} / {score.score.fan} / {score.score.bu}");
            if (score.point > 0)
            {
                laneUI[laneNum].isReady = true;
                laneUI[laneNum].readyScoreText.text = $"{score.score} / {score.point}";
                laneUI[laneNum].button.RemoveAction(MouseAction.LeftClick);
                laneUI[laneNum].button.GetComponent<UIImage>().SetImage(2);
                laneUI[laneNum].button.AddAction(MouseAction.LeftClick, () => 
                {
                    laneUI[laneNum].isReady = false;
                    totalScore += score.point;
                    laneUI[laneNum].readyScoreText.text = "";
                    ResetLane(laneNum);
                    lanes[laneNum].myWind++;
                    initResetterCoolDown++;
                    skipNum = 3;
                    tsumoProb = Mathf.Max(tsumoProb - tsumoDescent, tsumoMinProb);
                    SyncUI();
                });
            }
        }
        lanes[laneNum].closedHand = lanes[laneNum].closedHand.OrderBy(x => x.GetHashCode()).ToList();
        if (nextCards.Count == 0) DrawNextCards(3);
        SyncUI();
    }
    public void SyncUI()
    {
        var doraTuples = doraIndicators.Select(x => x.IndicatingCard);
        Func<MahjongCard, int> doraCount = x => doraTuples.Count(y => x * y == 0);

        int iter;
        for (iter = 0; iter < nextCards.Count; iter++)nextCardRenderer[iter].Set(spriteSetting[nextCards[iter]], doraCount(nextCards[iter]));
        for (; iter < 3; iter++) nextCardRenderer[iter].Set(spriteSetting[2], 0);
        nextCardTransform.anchoredPosition = -nextCardRenderer[nextCards.Count - 1].GetComponent<RectTransform>().anchoredPosition * .5f;

        for (iter = 0; iter < doraIndicators.Count; iter++) doraIndicatorRenderer[iter].Set(spriteSetting[doraIndicators[iter]], doraCount(doraIndicators[iter]));
        for (; iter < maxDoraNum; iter++) doraIndicatorRenderer[iter].Set(spriteSetting[1], 0);

        Loop.N(4, i =>
        {
            int idx = 0;
            int quadIdx = 0;
            int streak = 0;
            MahjongCard prevCard = null;
            Loop.N(18, j => laneUI[i].cardImage[j].GetComponent<UIMouseInteraction>().RemoveAction(MouseAction.LeftClick));
            foreach (var card in lanes[i].closedHand)
            {
                laneUI[i].cardImage[idx].Set(spriteSetting[card], doraCount(card));
                if (currentState == State.Dora && !laneUI[i].isReady)
                {
                    laneUI[i].cardImage[idx].SetColor(maxDoraNum + 1);
                    laneUI[i].cardImage[idx].GetComponent<UIMouseInteraction>().AddAction(MouseAction.LeftClick, () =>
                    {
                        lanes[i].RemoveCard(card);
                        doraIndicators.Add(card);
                        while (doraIndicators.Count > maxDoraNum)
                        {
                            deck.Add(doraIndicators[0]);
                            doraIndicators.RemoveAt(0);
                            ShuffleDeck();
                        }
                        makeDoraNum--;
                        currentState = State.Insert;
                        SyncUI();
                    });
                }
                idx++;
                if (prevCard != null && prevCard * card == 0) streak++;
                else streak = 0;
                if (streak == 3)
                {
                    laneUI[i].quadButton[quadIdx].gameObject.SetActive(true);
                    laneUI[i].quadButton[quadIdx].GetComponent<RectTransform>().anchoredPosition = (laneUI[i].cardImage[idx - 2].GetComponent<RectTransform>().anchoredPosition + laneUI[i].cardImage[idx - 3].GetComponent<RectTransform>().anchoredPosition) * .5f;
                    laneUI[i].quadButton[quadIdx].ReplaceAction(MouseAction.LeftClick, () => 
                    {
                        if (currentState != State.Insert) return;
                        lanes[i].MakeQuad(card);
                        laneUI[i].isReady = false;
                        SyncUI();
                    });
                    quadIdx++;
                }
                prevCard = card;
            }
            foreach (var set in lanes[i].openHand)
            {
                Loop.N(4, j => laneUI[i].cardImage[idx++].Set(j is 0 or 3 ? spriteSetting[0] : spriteSetting[set.cardSet[j == 2 ? 3 : j]], doraCount(set.cardSet[j])));
            }
            Loop.N(lanes[i].leftCard, j => laneUI[i].cardImage[idx++].Set(spriteSetting[1], 0));
            while (idx < 18) laneUI[i].cardImage[idx++].Set(spriteSetting[2], 0);
            while (quadIdx < 3) laneUI[i].quadButton[quadIdx++].gameObject.SetActive(false);

            laneUI[i].propertyText.text = lanes[i].myWind + (lanes[i].isRon ? " Ron" : " Tsumo");
        });
        SetLaneButton();
        SetCardMakeDoraButton();
        skipButton.SetImage(skipNum);
    }
}

public struct WindCounter
{
    private int _wind;
    private int wind
    {
        get => _wind;
        set
        {
            _wind = value % 4;
            if (_wind < 0) _wind += 4;
        }
    }
    public WindCounter(int w)
    {
        _wind = 0;
        wind = w;
    }

    public static implicit operator WindCounter(int x) => new WindCounter(x);
    public static implicit operator int(WindCounter x) => x.wind;

    public override string ToString() => wind switch
    {
        0 => "East",
        1 => "South",
        2 => "West",
        3 => "North",
        _ => ""
    };
}