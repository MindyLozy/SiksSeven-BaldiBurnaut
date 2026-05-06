using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(SiksSevenMenu.Main), "SiksSeven Menu", "1.0.0", "LolWorking")]
[assembly: MelonGame(null, null)]

namespace SiksSevenMenu
{
    public class Main : MelonMod
    {
        private bool noclipEnabled = false;
        private bool speedHackEnabled = false;
        private bool menuVisible = false;

        private float noclipSpeed = 10f;
        private float speedHackMultiplier = 2f;

        private GameObject playerObj;
        private bool playerCached = false;

        // Компоненты игрока
        private Collider playerCollider;
        private CharacterController playerCC;
        private Rigidbody playerRB;

        private CursorLockMode originalLockMode;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("SiksSeven Menu инициализирован. Home — меню, V — Noclip, X — SpeedHack.");
        }

        public override void OnUpdate()
        {
            if (!playerCached)
            {
                CachePlayer();
                if (!playerCached) return;
            }

            // Горячие клавиши
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
            }

            if (Input.GetKeyDown(KeyCode.Home))
            {
                menuVisible = !menuVisible;
                UpdateCursorState();
            }

            // ---- движение ноклипа ----
            if (noclipEnabled && playerObj != null)
            {
                float moveH = Input.GetAxis("Horizontal");
                float moveV = Input.GetAxis("Vertical");
                float moveUp = Input.GetKey(KeyCode.Space) ? 1f : 0f;
                float moveDown = Input.GetKey(KeyCode.LeftControl) ? 1f : 0f;

                Vector3 direction = (playerObj.transform.forward * moveV + playerObj.transform.right * moveH
                                     + Vector3.up * (moveUp - moveDown)).normalized;
                playerObj.transform.position += direction * noclipSpeed * Time.unscaledDeltaTime;
            }
            // ---- спидхак игрока (только когда ноклип выключен) ----
            else if (speedHackEnabled && playerObj != null)
            {
                float moveH = Input.GetAxis("Horizontal");
                float moveV = Input.GetAxis("Vertical");
                if (Mathf.Abs(moveH) > 0.01f || Mathf.Abs(moveV) > 0.01f)
                {
                    Vector3 extraMovement = (playerObj.transform.forward * moveV + playerObj.transform.right * moveH).normalized
                                            * (speedHackMultiplier - 1f) * Time.deltaTime * 5f; // 5f — примерная базовая скорость
                    if (playerCC != null)
                    {
                        playerCC.Move(extraMovement);
                    }
                    else if (playerRB != null)
                    {
                        playerRB.MovePosition(playerObj.transform.position + extraMovement);
                    }
                    else
                    {
                        playerObj.transform.position += extraMovement;
                    }
                }
            }
        }

        public override void OnGUI()
        {
            if (!menuVisible) return;

            float menuW = 280f, menuH = 220f;
            float x = (Screen.width - menuW) * 0.5f;
            float y = (Screen.height - menuH) * 0.5f;

            // Фон меню
            GUI.Box(new Rect(x, y, menuW, menuH), "");

            // Заголовок
            GUI.Label(new Rect(x + 10, y + 10, menuW - 20, 30), "SiksSeven Menu");

            // Кнопка Noclip
            if (GUI.Button(new Rect(x + 10, y + 50, menuW - 20, 30), $"Noclip: {(noclipEnabled ? "ON" : "OFF")}"))
            {
                noclipEnabled = !noclipEnabled;
                ToggleNoclip(noclipEnabled);
            }

            // Кнопка SpeedHack
            if (GUI.Button(new Rect(x + 10, y + 90, menuW - 20, 30), $"SpeedHack: {(speedHackEnabled ? "ON" : "OFF")}"))
            {
                speedHackEnabled = !speedHackEnabled;
            }

            // Слайдер Noclip Speed
            GUI.Label(new Rect(x + 10, y + 130, 100, 20), $"Noclip Speed: {noclipSpeed:F1}");
            noclipSpeed = GUI.HorizontalSlider(new Rect(x + 120, y + 135, 150, 20), noclipSpeed, 1f, 50f);

            // Слайдер SpeedHack Multiplier
            GUI.Label(new Rect(x + 10, y + 160, 120, 20), $"Speed Multiplier: {speedHackMultiplier:F1}x");
            speedHackMultiplier = GUI.HorizontalSlider(new Rect(x + 140, y + 165, 130, 20), speedHackMultiplier, 0.1f, 10f);
        }

        private void CachePlayer()
        {
            // Пробуем найти игрока по тегу "Player", потом по имени, потом находим объект с CharacterController
            playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
                playerObj = GameObject.Find("Player");
            if (playerObj == null)
            {
                CharacterController[] ccs = GameObject.FindObjectsOfType<CharacterController>();
                if (ccs != null && ccs.Length > 0)
                    playerObj = ccs[0].gameObject;
            }
            if (playerObj != null)
            {
                playerCollider = playerObj.GetComponent<Collider>();
                playerCC = playerObj.GetComponent<CharacterController>();
                playerRB = playerObj.GetComponent<Rigidbody>();
                playerCached = true;
                LoggerInstance.Msg("Игрок найден: " + playerObj.name);
            }
        }

        private void ToggleNoclip(bool enable)
        {
            // Отключаем CharacterController, если есть
            if (playerCC != null)
                playerCC.enabled = !enable;

            // Отключаем коллайдер, если есть
            if (playerCollider != null)
                playerCollider.enabled = !enable;

            // Отключаем гравитацию у Rigidbody, если есть
            if (playerRB != null)
            {
                playerRB.useGravity = !enable;
                if (enable) playerRB.velocity = Vector3.zero;
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
            if (noclipEnabled && playerCC != null)
                playerCC.enabled = true;
            if (noclipEnabled && playerCollider != null)
                playerCollider.enabled = true;
            if (playerRB != null)
                playerRB.useGravity = true;
            Cursor.lockState = originalLockMode;
            Cursor.visible = true;
        }
    }
}
