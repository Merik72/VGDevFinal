using UnityEngine;
using UnityEngine.UI;
using Gamekit3D; // Reference the namespace where PlayerControl is located

public class CooldownUIManager : MonoBehaviour
{
    public Image aoeCooldownImage; // Reference for the AoE cooldown icon
    public Image ultimateCooldownImage; // Reference for the Ultimate cooldown icon

    public PlayerControl playerControl; // Reference to the player control script

    void Update()
    {
        UpdateCooldownUI();
    }

    void UpdateCooldownUI()
    {
        if (playerControl != null)
        {
            // Update AoE cooldown image
            float aoeCooldownRemaining = playerControl.GetRemainingAOECooldown();
            float aoeMaxCooldown = playerControl.GetAOECooldown();

            // Avoid setting the Fill Amount to zero on game start
            float aoeFillAmount = Mathf.Clamp01(aoeCooldownRemaining / aoeMaxCooldown);
            aoeCooldownImage.fillAmount = aoeFillAmount;

            // Update Ultimate cooldown image
            float ultimateCooldownRemaining = playerControl.GetRemainingUltimateCooldown();
            float ultimateMaxCooldown = playerControl.GetUltimateCooldown();

            float ultimateFillAmount = Mathf.Clamp01(ultimateCooldownRemaining / ultimateMaxCooldown);
            ultimateCooldownImage.fillAmount = ultimateFillAmount;
        }
    }
}
