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
            _resetterCooldown = value;
            resetterCooldownText.text = value.ToString();
            resetterCooldownText.gameObject.SetActive(value != 0);
            resetterButton.gameObject.SetActive(value == 0);
        }
    }

    public CardSpriteSetting spriteSetting;

    public UIMouseInteraction startButton, resetterButton;
    public UIMouseInteraction[] laneButton;
    public Text scoreText, resetterCooldownText;

    public RectTransform[] laneTransform;
    private SpriteRenderer[,] laneCardRenderer;
    public RectTransform nextCardTransform;
    public SpriteRenderer[,] nextCardRenderer;
    public GameObject cardPrefab;

    private void Start()
    {
        startButton.AddAction(MouseAction.LeftClick, () =>
        {
            ResetGame();
            startButton.gameObject.SetActive(false);
        });
        resetterButton.AddAction(MouseAction.LeftClick, () => SetLaneButton(true));

        float gap = cardPrefab.GetComponent<RectTransform>().sizeDelta.x * 1.1f;
        laneCardRenderer = new SpriteRenderer[4, 18];
        Loop.N(4, 18, (i, j) =>
        {
            var obj = Instantiate(cardPrefab, laneTransform[i]).GetComponent<RectTransform>();
            obj.anchoredPosition = new Vector2(gap * j, 0);
            laneCardRenderer[i, j] = obj.GetComponent<SpriteRenderer>();
        });
    }

    private void SetLaneButton(bool isResetter)
    {
        Loop.N(4, i => laneButton[i].RemoveAction(MouseAction.LeftClick));
        if (!isResetter) Loop.N(4, i => laneButton[i].AddAction(MouseAction.LeftClick, () => AddToLane(i)));
        else Loop.N(4, i => laneButton[i].AddAction(MouseAction.LeftClick, () => UseResetter(i)));
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
        lanes = new MahjongPlayer[4];
        Loop.N(4, i =>
        {
            lanes[i] = new MahjongPlayer();
            lanes[i].myWind = i;
            lanes[i].ResetPlayer();
        });
        totalScore = 0;
        resetterCooldown = 0;
        DrawNextCards(1);
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
            resetterCooldown = 20 + totalScore / 5000;
            SetLaneButton(false);
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
        while (nextCards.Count > 0)
        {
            if (lanes[laneNum].leftCard > 0) break;
            lanes[laneNum].AddCard(nextCards[0]);
            lastCard = nextCards[0];
            nextCards.RemoveAt(0);
        }
        if (lanes[laneNum].leftCard == 0)
        {
            int score = lanes[laneNum].CalculateBestScore(lastCard);
            if (score > 0)
            {
                totalScore += score;
                ResetLane(laneNum);
                lanes[laneNum].myWind++;
            }
        }
        lanes[laneNum].closedHand = lanes[laneNum].closedHand.OrderBy(x => UnityEngine.Random.Range(0f, 1f)).ToList();
        if (nextCards.Count == 0) DrawNextCards(1); 
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
}