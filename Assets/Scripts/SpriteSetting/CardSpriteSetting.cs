using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "CardSpriteSetting", menuName = "ScriptableObjects/CardSpriteSetting", order = 1)]
public class CardSpriteSetting : ScriptableObject
{
    public Sprite[] sprites;
    private Dictionary<(CardColor color, int num, bool isRed), Sprite> _sprtDict;
    public Dictionary<(CardColor color, int num, bool isRed), Sprite> sprtDict
    {
        get
        {
            if (_sprtDict == null)
            {
                _sprtDict = new Dictionary<(CardColor color, int num, bool isRed), Sprite>();
                foreach (var sprt in sprites)
                {
                    int.TryParse(sprt.name, out int id);
                    _sprtDict.Add(((CardColor)(id / 100), (id / 10) % 10, id % 10 == 1), sprt);
                }
            }
            return _sprtDict;
        }
    }
    public Sprite this[MahjongCard card] => sprtDict[(card.color, card.num, card.isRed)];
    public Sprite this[int num] => sprtDict[(0, num, false)];
}