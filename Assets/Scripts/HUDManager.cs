using UnityEngine;

namespace WaywardSon
{
    /// <summary>
    /// HUD unificado que centraliza toda a informação visual do jogo.
    /// Substitui os OnGUI individuais de PlayerController, WeaponHandler e FlashlightController.
    /// 
    /// Layout:
    /// - Canto superior esquerdo: Arma + Munição
    /// - Canto inferior esquerdo: HP (ECG) + Bateria da Lanterna
    /// - Centro inferior: Dicas de controles
    /// - Indicador de visão passiva: Próximo à barra de bateria
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("References")]
        public PlayerController playerController;
        public PlayerHealth playerHealth;
        public WeaponHandler weaponHandler;
        public FlashlightController flashlight;
        public CharacterStamina characterStamina;

        [Header("Layout Settings")]
        public float topMargin = 20f;
        public float bottomMargin = 20f;
        public float sideMargin = 20f;
        public float lineHeight = 28f;
        public float barHeight = 16f;
        public float barWidth = 180f;

        // Cached styles
        private GUIStyle _weaponStyle;
        private GUIStyle _ammoStyle;
        private GUIStyle _hpStyle;
        private GUIStyle _batteryLabelStyle;
        private GUIStyle _passiveStyle;
        private GUIStyle _controlsStyle;
        private GUIStyle _statusStyle;
        private bool _stylesReady = false;

        // Texture cache
        private Texture2D _whiteTexture;
        private Texture2D _darkTexture;

        private void Start()
        {
            // Auto-find references if not set
            if (playerController == null)
                playerController = FindAnyObjectByType<PlayerController>();
            if (playerHealth == null)
                playerHealth = FindAnyObjectByType<PlayerHealth>();
            if (weaponHandler == null)
                weaponHandler = FindAnyObjectByType<WeaponHandler>();
            if (flashlight == null)
                flashlight = FindAnyObjectByType<FlashlightController>();
            if (characterStamina == null)
                characterStamina = FindAnyObjectByType<CharacterStamina>();

            // Create textures
            _whiteTexture = MakeTex(Color.white);
            _darkTexture = MakeTex(new Color(0.1f, 0.1f, 0.12f, 0.85f));
        }

        private void OnGUI()
        {
            if (!_stylesReady) InitStyles();

            // ── TOP-LEFT: Weapon Info ──
            DrawWeaponInfo();

            // ── BOTTOM-LEFT: HP + Battery ──
            DrawPlayerStatus();

            // ── CENTER-BOTTOM: Controls Hint ──
            DrawControlsHint();
        }

        // ═══════════════════════════════════════════════════════════════════
        // WEAPON INFO (Top-Left)
        // ═══════════════════════════════════════════════════════════════════

        private void DrawWeaponInfo()
        {
            if (weaponHandler == null || weaponHandler.activeWeapon == null) return;

            float x = sideMargin;
            float y = topMargin;

            // Weapon name + aiming state
            string aimTag = (playerController != null && playerController.isAiming) ? "  [AIM]" : "";
            _weaponStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(x, y, 400, lineHeight), $"{weaponHandler.activeWeapon.weaponName}{aimTag}", _weaponStyle);

            // Ammo
            y += lineHeight;
            _ammoStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(x, y, 300, lineHeight), $"Munição: {weaponHandler.currentAmmo} / {weaponHandler.activeWeapon.ammoCapacity}", _ammoStyle);

            // Passive vision indicator (under weapon info)
            if (flashlight != null && !flashlight.IsFlashlightOn)
            {
                y += lineHeight;
                _passiveStyle.normal.textColor = new Color(1f, 0.6f, 0.2f);
                GUI.Label(new Rect(x, y, 400, lineHeight), "⚠ VISÃO PASSIVA  [-20% vel | -30% dano]", _passiveStyle);
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // PLAYER STATUS (Bottom-Left): HP Bar + Battery Bar
        // ═══════════════════════════════════════════════════════════════════

        private void DrawPlayerStatus()
        {
            float x = sideMargin;
            float y = Screen.height - bottomMargin - barHeight;

            // ── HP Bar ──
            DrawBar(x, y, barWidth, barHeight, GetHPFill(), GetHPColor(), "HP");
            y -= barHeight + 6f;

            // HP text
            if (playerHealth != null)
            {
                string hpText = $"ECG: {playerHealth.CurrentState.ToString().ToUpper()}  ({playerHealth.currentHealth}/{playerHealth.maxHealth})";
                _hpStyle.normal.textColor = GetHPColor();
                GUI.Label(new Rect(x, y, 400, lineHeight), hpText, _hpStyle);
                y -= lineHeight + 4f;
            }

            // ── Battery Bar ──
            if (flashlight != null)
            {
                DrawBar(x, y, barWidth, barHeight, flashlight.BatteryPercentage, GetBatteryColor(), "BATERIA");
                y -= barHeight + 6f;

                // Flashlight state
                string flashState = flashlight.IsFlashlightOn ? "🔦 ON" : " flashlight OFF";
                _batteryLabelStyle.normal.textColor = flashlight.IsFlashlightOn ? Color.yellow : new Color(0.5f, 0.5f, 0.5f);
                GUI.Label(new Rect(x, y, 300, lineHeight), flashState, _batteryLabelStyle);
            }

            // ── Stamina Bar ──
            if (characterStamina != null)
            {
                y -= lineHeight + 10f;

                DrawBar(x, y, barWidth, barHeight, characterStamina.StaminaNormalized, GetStaminaColor(), "STAMINA");
                y -= barHeight + 6f;

                // Sprint state
                string sprintState = characterStamina.IsSprinting ? "▶ SPRINT" : "SHIFT Correr";
                _batteryLabelStyle.normal.textColor = characterStamina.IsSprinting
                    ? new Color(0.3f, 0.9f, 1f)
                    : new Color(0.5f, 0.5f, 0.5f);
                GUI.Label(new Rect(x, y, 300, lineHeight), sprintState, _batteryLabelStyle);
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // CONTROLS HINT (Center-Bottom)
        // ═══════════════════════════════════════════════════════════════════

        private void DrawControlsHint()
        {
            string text = "WASD Mover  |  LShift/L3 Correr  |  RMB/LT Mirar  |  LMB/RT Atirar  |  F/LB Lanterna  |  I/Y Inventário  |  E/Cruz Coletar";
            Vector2 size = _controlsStyle.CalcSize(new GUIContent(text));

            float x = (Screen.width - size.x) * 0.5f;
            float y = Screen.height - bottomMargin - 10f;

            // Background
            GUI.color = new Color(0, 0, 0, 0.6f);
            GUI.DrawTexture(new Rect(x - 8f, y - 2f, size.x + 16f, size.y + 4f), _whiteTexture);
            GUI.color = Color.white;

            _controlsStyle.normal.textColor = new Color(0.6f, 0.6f, 0.65f);
            GUI.Label(new Rect(x, y, size.x, size.y), text, _controlsStyle);
        }

        // ═══════════════════════════════════════════════════════════════════
        // BAR DRAWING UTILITY
        // ═══════════════════════════════════════════════════════════════════

        private void DrawBar(float x, float y, float width, float height, float fill, Color fillColor, string label)
        {
            // Background
            GUI.color = new Color(0.12f, 0.12f, 0.15f, 0.9f);
            GUI.DrawTexture(new Rect(x, y, width, height), _darkTexture);

            // Border
            GUI.color = new Color(0.3f, 0.3f, 0.35f, 0.8f);
            GUI.DrawTexture(new Rect(x, y, width, 1f), _whiteTexture);          // top
            GUI.DrawTexture(new Rect(x, y + height - 1, width, 1f), _whiteTexture); // bottom
            GUI.DrawTexture(new Rect(x, y, 1f, height), _whiteTexture);          // left
            GUI.DrawTexture(new Rect(x + width - 1, y, 1f, height), _whiteTexture); // right

            // Fill
            float fillWidth = Mathf.Clamp01(fill) * (width - 4f);
            GUI.color = fillColor;
            GUI.DrawTexture(new Rect(x + 2, y + 2, fillWidth, height - 4f), _whiteTexture);

            // Label
            GUI.color = Color.white;
            GUI.Label(new Rect(x + 4, y, width, height), $"{label}  {Mathf.RoundToInt(fill * 100)}%", _batteryLabelStyle);

            GUI.color = Color.white;
        }

        // ═══════════════════════════════════════════════════════════════════
        // COLOR HELPERS
        // ═══════════════════════════════════════════════════════════════════

        private float GetHPFill()
        {
            if (playerHealth == null) return 1f;
            return (float)playerHealth.currentHealth / playerHealth.maxHealth;
        }

        private Color GetHPColor()
        {
            if (playerHealth == null) return Color.green;
            switch (playerHealth.CurrentState)
            {
                case PlayerHealth.HealthState.Fine:    return new Color(0.2f, 0.85f, 0.3f);
                case PlayerHealth.HealthState.Caution: return new Color(0.9f, 0.8f, 0.15f);
                case PlayerHealth.HealthState.Danger:  return new Color(0.9f, 0.2f, 0.15f);
                default: return Color.green;
            }
        }

        private Color GetBatteryColor()
        {
            if (flashlight == null) return Color.green;
            float pct = flashlight.BatteryPercentage;
            if (pct > 0.5f) return new Color(0.2f, 0.85f, 0.3f);
            if (pct > 0.2f) return new Color(0.9f, 0.8f, 0.15f);
            return new Color(0.9f, 0.2f, 0.15f);
        }

        private Color GetStaminaColor()
        {
            if (characterStamina == null) return Color.cyan;
            float pct = characterStamina.StaminaNormalized;
            if (pct > 0.5f) return new Color(0.3f, 0.85f, 1f);
            if (pct > 0.2f) return new Color(0.9f, 0.8f, 0.15f);
            return new Color(0.9f, 0.2f, 0.15f);
        }

        // ═══════════════════════════════════════════════════════════════════
        // STYLE INIT
        // ═══════════════════════════════════════════════════════════════════

        private void InitStyles()
        {
            _weaponStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };

            _ammoStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };

            _hpStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };

            _batteryLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };

            _passiveStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };

            _controlsStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal
            };

            _statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };

            _stylesReady = true;
        }

        private Texture2D MakeTex(Color col)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, col);
            t.Apply();
            return t;
        }
    }
}
