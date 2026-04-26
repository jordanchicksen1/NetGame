using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SpellUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] TextMeshProUGUI spellNameText;
    [SerializeField] Image spellIcon;

    [Header("Sprites")]
    [SerializeField] Sprite fireIcon;
    [SerializeField] Sprite iceIcon;
    [SerializeField] Sprite poisonIcon;
    [SerializeField] Sprite noneIcon;

    PlayerController2D player;

    void Start()
    {
        StartCoroutine(FindPlayer());
    }

    IEnumerator FindPlayer()
    {
        while (player == null)
        {
            var players = FindObjectsByType<PlayerController2D>(FindObjectsSortMode.None);

            foreach (var p in players)
            {
                if (p.IsOwner)
                {
                    player = p;
                    break;
                }
            }

            yield return null;
        }
    }

    void Update()
    {
        if (player == null) return;

        UpdateSpellUI(player.GetCurrentSpell());
    }

    void UpdateSpellUI(SpellType spell)
    {
        switch (spell)
        {
            case SpellType.Fire:
                spellNameText.text = "Fire";
                spellIcon.sprite = fireIcon;
                break;

            case SpellType.Ice:
                spellNameText.text = "Ice";
                spellIcon.sprite = iceIcon;
                break;

            case SpellType.Poison:
                spellNameText.text = "Poison";
                spellIcon.sprite = poisonIcon;
                break;

            default:
                spellNameText.text = "None";
                spellIcon.sprite = noneIcon;
                break;
        }
    }
}