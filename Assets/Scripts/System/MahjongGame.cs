using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MahjongGame : MonoBehaviour
{
    public List<MahjongCard> nextCards;
    public MahjongPlayer[] lanes;
    public List<MahjongCard> deck;

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

    private int _resetterCooldown;
    private int resetterCooldown
    {
        get => _resetterCooldown;
        set
        {
            _resetterCooldown = value > 0 ? value : 0;
            resetterCooldownText.text = _resetterCooldown.ToString();
            resetterCooldownText.gameObject.SetActive(_resetterCooldown != 0);
            resetterButton.gameObject.SetActive(_resetterCooldown == 0);
        }
    }
    public int initResetterCooldown;

    public CardSpriteSetting spriteSetting;

    public UIMouseInteraction startButton, resetterButton;
    public Text scoreText, resetterCooldownText;

    public UILaneController[] laneUI;
    public RectTransform nextCardTransform;
    private UIImage[] nextCardRenderer;
    public GameObject cardPrefab;
    public GameObject quadButtonPrefab;

    public GameObject gameParent;

    private void Start()
    {
        startButton.AddAction(MouseAction.LeftClick, () =>
        {
            ResetGame();
            startButton.gameObject.SetActive(false);
            gameParent.SetActive(true);
        });
        resetterButton.AddAction(MouseAction.LeftClick, () => SetLaneButton(true));

        float gap = cardPrefab.GetComponent<RectTransform>().sizeDelta.x * 1.1f;

        SetLaneButton(false);
        
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

        gameParent.SetActive(false);
    }

    private void SetLaneButton(bool isResetter)
    {
        
        if (!isResetter)
        {
            Loop.N(4, i => 
            {
                if (!laneUI[i].isReady)
                {
                    laneUI[i].button.RemoveAction(MouseAction.LeftClick);
                    laneUI[i].button.AddAction(MouseAction.LeftClick, () => AddToLane(i));
                    laneUI[i].button.GetComponent<UIImage>().Set(0);
                }
            });
            resetterButton.GetComponent<UIImage>().Set(0);
            resetterButton.RemoveAction(MouseAction.LeftClick);
            resetterButton.AddAction(MouseAction.LeftClick, () => SetLaneButton(true));
        }
        else
        {
            Loop.N(4, i =>
            {
                if (!laneUI[i].isReady)
                {
                    laneUI[i].button.RemoveAction(MouseAction.LeftClick);
                    laneUI[i].button.AddAction(MouseAction.LeftClick, () => UseResetter(i));
                    laneUI[i].button.GetComponent<UIImage>().Set(1);
                }
            });
            resetterButton.GetComponent<UIImage>().Set(1);
            resetterButton.RemoveAction(MouseAction.LeftClick);
            resetterButton.AddAction(MouseAction.LeftClick, () => SetLaneButton(false));
        }
    }

    public void ResetGame()
    {
        deck = new List<MahjongCard>();
        Loop.N(4, i =>
        {
            foreach (var card in MahjongInfo.cardList) deck.Add(new MahjongCard(card.color, card.num, card.num == 5 && i == 0));
        });
        UnityEngine.Random.InitState((int)(Time.time * 100f));
        deck = deck.OrderBy(x => UnityEngine.Random.Range(0f, 1f)).ToList();

        nextCards = new List<MahjongCard>();

        lanes = new MahjongPlayer[4];
        Loop.N(4, i =>
        {
            lanes[i] = new MahjongPlayer();
            lanes[i].myWind = i;
            lanes[i].ResetPlayer();
        });

        totalScore = 0;
        resetterCooldown = 0;
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
        if (resetterCooldown == 0)
        {
            ResetLane(laneNum);
            resetterCooldown = initResetterCooldown + totalScore / 5000;
            SetLaneButton(false);
            SyncUI();
        }
    }
    public void ResetLane(int laneNum)
    {
        foreach (var card in lanes[laneNum].hand) deck.Add(card);
        lanes[laneNum].ResetPlayer();
        deck = deck.OrderBy(x => UnityEngine.Random.Range(0f, 1f)).ToList();
    }

    public void AddToLane(int laneNum)
    {
        MahjongCard lastCard = null;

        if (lanes[laneNum].leftCard == 0) return;
        lanes[laneNum].AddCard(nextCards[0]);
        lastCard = nextCards[0];
        nextCards.RemoveAt(0);
        resetterCooldown--;

        if (lanes[laneNum].leftCard == 0)
        {
            var score = lanes[laneNum].CalculateBestScore(lastCard);
            Debug.Log($"{score.score} / {score.score.fan} / {score.score.bu}");
            if (score.point > 0)
            {
                laneUI[laneNum].isReady = true;
                laneUI[laneNum].readyScoreText.text = $"{score.score} / {score.point}";
                laneUI[laneNum].button.RemoveAction(MouseAction.LeftClick);
                laneUI[laneNum].button.GetComponent<UIImage>().Set(2);
                laneUI[laneNum].button.AddAction(MouseAction.LeftClick, () => 
                {
                    laneUI[laneNum].isReady = false;
                    totalScore += score.point;
                    laneUI[laneNum].readyScoreText.text = "";
                    ResetLane(laneNum);
                    lanes[laneNum].myWind++;
                    SetLaneButton(false);
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
        int iter;
        for (iter = 0; iter < nextCards.Count; iter++) nextCardRenderer[iter].Set(spriteSetting[nextCards[iter]]);
        for (; iter < 3; iter++) nextCardRenderer[iter].Set(spriteSetting[2]);
        nextCardTransform.anchoredPosition = -nextCardRenderer[nextCards.Count - 1].GetComponent<RectTransform>().anchoredPosition * .5f;

        Loop.N(4, i =>
        {
            int idx = 0;
            int quadIdx = 0;
            int streak = 0;
            MahjongCard prevCard = null;
            foreach (var card in lanes[i].closedHand)
            {
                laneUI[i].cardImage[idx++].Set(spriteSetting[card]);
                if (prevCard != null && prevCard * card == 0) streak++;
                else streak = 0;
                if (streak == 3)
                {
                    laneUI[i].quadButton[quadIdx].gameObject.SetActive(true);
                    laneUI[i].quadButton[quadIdx].GetComponent<RectTransform>().anchoredPosition = (laneUI[i].cardImage[idx - 2].GetComponent<RectTransform>().anchoredPosition + laneUI[i].cardImage[idx - 3].GetComponent<RectTransform>().anchoredPosition) * .5f;
                    laneUI[i].quadButton[quadIdx].RemoveAction(MouseAction.LeftClick);
                    laneUI[i].quadButton[quadIdx].AddAction(MouseAction.LeftClick, () => 
                    {
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
                Loop.N(4, j => laneUI[i].cardImage[idx++].Set(j is 0 or 3 ? spriteSetting[0] : spriteSetting[set.cardSet[0]]));
            }
            Loop.N(lanes[i].leftCard, j => laneUI[i].cardImage[idx++].Set(spriteSetting[1]));
            while (idx < 18) laneUI[i].cardImage[idx++].Set(spriteSetting[2]);
            while (quadIdx < 3) laneUI[i].quadButton[quadIdx++].gameObject.SetActive(false);

            laneUI[i].propertyText.text = lanes[i].myWind + (lanes[i].isRon ? " Ron" : " Tsumo");
        });
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