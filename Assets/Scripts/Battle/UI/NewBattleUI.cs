using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
public class NewBattleUI : MonoBehaviour
{
    public Image HpBarFull;
    public Image HpBarWhite;
    public Image MpBarFull;
    public Image MpBarWhite;
    public Image EnemyHpBarFull;
    public Button Attack;
    public Button Defend;
    public Button Skill;
    public Button Skill2;
    public Button Maski;
    public float maxHealth = 100f;
    public float smoothspeed = 3f;
    public float delay = 0.5f;
    private float delayTimer;
    private float Health;
    private float whiteHealth;
    private float MPwhitehealth;
    private float whiteMP;
    
    public void updateHP(float damage)
    {
        Health -= 30f;
        HpBarFull.fillAmount = Health / maxHealth;
        whiteHealth = Health / maxHealth;
        delayTimer = delay;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Health = maxHealth;
        whiteHealth = Health;
        delayTimer = delay;
    }

    // Update is called once per frame
    void Update()
    {
       if (delayTimer > 0)
        {
            delayTimer -= Time.deltaTime;
            return;
        }

        HpBarWhite.fillAmount = Mathf.Lerp(HpBarWhite.fillAmount, whiteHealth, Time.deltaTime * smoothspeed);
    }
}
