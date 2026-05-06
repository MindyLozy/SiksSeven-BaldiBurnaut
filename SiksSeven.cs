using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(SiksSevenMod.Main), "SiksSeven Menu", "1.0.0", "eni")]
[assembly: MelonGame(null, null)] // работает в любой Unity-игре

namespace SiksSevenMod
{
    public class Main : MelonMod
    {
        // ---------- чит-флаги ----------
        private bool noclipEnabled = false;
        private bool speedHackEnabled = false;
        private bool menuVisible = false;

        // ---------- настройки ----------
        private float noclipSpeed = 10f;
        private float speedHackMultiplier = 2f;

        // ---------- кеш игрока ----------
        private GameObject playerObj;
        private Collider playerCollider;
        private bool playerCached = false;

        // ---------- сохранение оригинальных значений ----------
        private float originalTimeScale = 1f;
        private CursorLockMode originalLockMode;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("SiksSeven Menu инициализирован. Home — меню, V — Noclip, X — SpeedHack.");
            originalTimeScale = Time.timeScale;
        }

        public override void OnUpdate()
        {
            if (!playerCached)
            {
                CachePlayer();
                if (!playerCached) return; // игрок ещё не найден, ничего не делаем
            }

            // ---- горячие клавиши ----
            if (Input.GetKeyDown(KeyCode.V))
            {
                noclipEnabled = !noclipEnabled;
                LoggerInstance.Msg($"Noclip: {noclipEnabled}");
                ToggleNoclip(noclipEnabled);
            }

            if (Input.GetKeyDown(KeyCode.X))
            {
                speedHackEnabled = !speedHackEnabled;
                LoggerInstance.Msg($"SpeedHack: {speedHackEnabled} (x{speedHackMultiplier})");
                Time.timeScale = speedHackEnabled ? speedHackMultiplier : originalTimeScale;
            }

            if (Input.GetKeyDown(KeyCode.Home))
            {
                menuVisible = !menuVisible;
                UpdateCursorState();
            }

            // ---- движение при ноклипе ----
            if (noclipEnabled && playerObj != null)
            {
                float moveH = Input.GetAxis("Horizontal");
                float moveV = Input.GetAxis("Vertical");
                float moveUp = Input.GetKey(KeyCode.Space) ? 1f : 0f;
                float moveDown = Input.GetKey(KeyCode.LeftControl) ? 1f : 0f;

                Vector3 direction = (playerObj.transform.forward * moveV + playerObj.transform.right * moveH
                                     + Vector3.up * (moveUp - moveDown)).normalized;
                playerObj.transform.position += direction * noclipSpeed * Time.unscaledDeltaTime; // unscaled, чтобы не зависеть от timeScale
            }
        }

        public override void OnGUI()
        {
            if (!menuVisible) return;

            // простая рамка меню
            float menuW = 280f, menuH = 220f;
            float x = (Screen.width - menuW) * 0.5f;
            float y = (Screen.height - menuH) * 0.5f;

            GUI.Box(new Rect(x, y, menuW, menuH), "");
            GUILayout.BeginArea(new Rect(x + 10, y + 10, menuW - 20, menuH - 20));

            GUILayout.Label("<b><size=18>SiksSeven Menu</size></b>");
            GUILayout.Space(10);

            // Noclip toggle
            if (GUILayout.Button($"Noclip: {(noclipEnabled ? "ON" : "OFF")}", GUILayout.Height(30)))
            {
                noclipEnabled = !noclipEnabled;
                ToggleNoclip(noclipEnabled);
            }

            // SpeedHack toggle
            if (GUILayout.Button($"SpeedHack: {(speedHackEnabled ? "ON" : "OFF")}", GUILayout.Height(30)))
            {
                speedHackEnabled = !speedHackEnabled;
                Time.timeScale = speedHackEnabled ? speedHackMultiplier : originalTimeScale;
            }

            GUILayout.Space(10);
            GUILayout.Label($"Noclip Speed: {noclipSpeed:F1}");
            noclipSpeed = GUILayout.HorizontalSlider(noclipSpeed, 1f, 50f, GUILayout.Width(200));

            GUILayout.Label($"SpeedHack Multiplier: {speedHackMultiplier:F1}x");
            float newMultiplier = GUILayout.HorizontalSlider(speedHackMultiplier, 0.1f, 10f, GUILayout.Width(200));
            if (!Mathf.Approximately(newMultiplier, speedHackMultiplier))
            {
                speedHackMultiplier = newMultiplier;
                if (speedHackEnabled)
                    Time.timeScale = speedHackMultiplier;
            }

            GUILayout.EndArea();
        }

        private void CachePlayer()
        {
            playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
            {
                // попробуем найти по имени (часто бывает "Player")
                playerObj = GameObject.Find("Player");
            }
            if (playerObj != null)
            {
                playerCollider = playerObj.GetComponent<Collider>();
                playerCached = true;
                LoggerInstance.Msg("Игрок найден: " + playerObj.name);
            }
        }

        private void ToggleNoclip(bool enable)
        {
            if (playerCollider != null)
                playerCollider.enabled = !enable; // выключаем коллайдер, чтобы игрок летал сквозь стены

            // если включаем ноклип, можно также убрать гравитацию и т.п.
            Rigidbody rb = playerObj?.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = !enable;
                rb.velocity = Vector3.zero; // останавливаем
            }
        }

        private void UpdateCursorState()
        {
            if (menuVisible)
            {
                originalLockMode = Cursor.lockState;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = originalLockMode;
                Cursor.visible = (originalLockMode != CursorLockMode.Locked);
            }
        }

        public override void OnDeinitializeMelon()
        {
            // сброс состояний при выгрузке
            if (speedHackEnabled)
                Time.timeScale = originalTimeScale;
            if (noclipEnabled && playerCollider != null)
                playerCollider.enabled = true;
            Cursor.lockState = originalLockMode;
            Cursor.visible = true;
        }
    }
}