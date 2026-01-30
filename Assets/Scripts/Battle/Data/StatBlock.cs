using System;
using UnityEngine;

[Serializable]
public struct StatBlock
{
    public int HP;
    public int MP;
    public int ATK;
    public int DEF;
    public int MAG;
    public int RES;
    public int SPD;

    public StatBlock(int hp, int mp, int atk, int def, int mag, int res, int spd)
    {
        HP = hp;
        MP = mp;
        ATK = atk;
        DEF = def;
        MAG = mag;
        RES = res;
        SPD = spd;
    }
}

[Serializable]
public struct StatMultiplier
{
    public float HP;
    public float MP;
    public float ATK;
    public float DEF;
    public float MAG;
    public float RES;
    public float SPD;

    public static StatMultiplier One => new StatMultiplier
    {
        HP = 1f,
        MP = 1f,
        ATK = 1f,
        DEF = 1f,
        MAG = 1f,
        RES = 1f,
        SPD = 1f
    };

    public void Multiply(StatMultiplier other)
    {
        HP *= other.HP;
        MP *= other.MP;
        ATK *= other.ATK;
        DEF *= other.DEF;
        MAG *= other.MAG;
        RES *= other.RES;
        SPD *= other.SPD;
    }
}
