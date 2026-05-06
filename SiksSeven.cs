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

        // строки для текстовых полей
        private string noclipSpeedStr = "10";
        private string speedHackStr = "2";

        private GameObject playerObj;
        private bool playerCached = false;

        // компоненты игрока
        private MonoBehaviour fpsController;    // любой скрипт движения от первого лица
        private Collider playerCollider;
        private CharacterController playerCC;
        private Rigidbody playerRB;

        private CursorLockMode originalLockMode;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("SiksSeven Menu инициализирован. Home — меню, V — Noclip, X — SpeedHack.");
            noclipSpeedStr = noclipSpeed.ToString("F1");
            speedHackStr = speedHackMultiplier.ToString("F1");
        }

        public override void OnUpdate()
        {
            if (!playerCached)
            {
                CachePlayer();
                if (!playerCached) return;
            }

            // горячие клавиши
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

            // ---- ноклип (свободный полёт) ----
            if (noclipEnabled && playerObj != null)
            {
                // отключаем стандартный контроллер, если ещё не выключен
                if (fpsController != null && fpsController.enabled)
                    fpsController.enabled = false;
                if (playerCC != null && playerCC.enabled)
                    playerCC.enabled = false;
                if (playerRB != null && playerRB.useGravity)
                    playerRB.useGravity = false;

                // движение относительно камеры
                if (Camera.main == null) return;

                float moveF = Input.GetKey(KeyCode.W) ? 1f : 0f;
                float moveB = Input.GetKey(KeyCode.S) ? 1f : 0f;
                float moveL = Input.GetKey(KeyCode.A) ? 1f : 0f;
                float moveR = Input.GetKey(KeyCode.D) ? 1f : 0f;
                float moveUp = Input.GetKey(KeyCode.Space) ? 1f : 0f;
                float moveDown = Input.GetKey(KeyCode.LeftControl) ? 1f : 0f;

                Vector3 direction = Vector3.zero;
                direction += Camera.main.transform.forward * (moveF - moveB);
                direction += Camera.main.transform.right * (moveR - moveL);
                direction += Camera.main.transform.up * (moveUp - moveDown);

                if (direction.magnitude > 0.01f)
                {
                    direction.Normalize();
                    playerObj.transform.position += direction * noclipSpeed * Time.unscaledDeltaTime;
                }
            }
            // ---- спидхак игрока (работает только когда ноклип выключен) ----
            else if (speedHackEnabled && playerObj != null)
            {
                float moveH = Input.GetAxis("Horizontal");
                float moveV = Input.GetAxis("Vertical");
                if (Mathf.Abs(moveH) > 0.01f || Mathf.Abs(moveV) > 0.01f)
                {
                    Vector3 extraMovement = (playerObj.transform.forward * moveV + playerObj.transform.right * moveH).normalized
                                            * (speedHackMultiplier - 1f) * Time.deltaTime * 5f;
                    if (playerCC != null && playerCC.enabled)
                        playerCC.Move(extraMovement);
                    else if (playerRB != null)
                        playerRB.MovePosition(playerObj.transform.position + extraMovement);
                    else
                        playerObj.transform.position += extraMovement;
                }
            }
        }

        public override void OnGUI()
        {
            if (!menuVisible) return;

            float menuW = 300f, menuH = 260f;
            float x = 20f;                     // от левого края
            float y = 20f;                     // от верхнего края

            GUI.Box(new Rect(x, y, menuW, menuH), "");

            GUI.Label(new Rect(x + 10, y + 10, menuW - 20, 30), "SiksSeven Menu");

            // кнопка Noclip
            if (GUI.Button(new Rect(x + 10, y + 50, menuW - 20, 30), $"Noclip: {(noclipEnabled ? "ON" : "OFF")}"))
            {
                noclipEnabled = !noclipEnabled;
                ToggleNoclip(noclipEnabled);
            }

            // кнопка SpeedHack
            if (GUI.Button(new Rect(x + 10, y + 90, menuW - 20, 30), $"SpeedHack: {(speedHackEnabled ? "ON" : "OFF")}"))
            {
                speedHackEnabled = !speedHackEnabled;
            }

            // поле Noclip Speed
            GUI.Label(new Rect(x + 10, y + 135, 100, 20), "Noclip Speed:");
            string newNoclipStr = GUI.TextField(new Rect(x + 120, y + 135, 70, 20), noclipSpeedStr);
            if (newNoclipStr != noclipSpeedStr)
            {
                if (float.TryParse(newNoclipStr, out float parsed))
                {
                    noclipSpeed = Mathf.Clamp(parsed, 0.1f, 100f);
                    noclipSpeedStr = noclipSpeed.ToString("F1");
                }
                else
                {
                    noclipSpeedStr = noclipSpeed.ToString("F1");
                }
            }

            // поле Speed Multiplier
            GUI.Label(new Rect(x + 10, y + 165, 130, 20), "Speed Multiplier:");
            string newSpeedStr = GUI.TextField(new Rect(x + 150, y + 165, 70, 20), speedHackStr);
            if (newSpeedStr != speedHackStr)
            {
                if (float.TryParse(newSpeedStr, out float parsed))
                {
                    speedHackMultiplier = Mathf.Clamp(parsed, 0.1f, 20f);
                    speedHackStr = speedHackMultiplier.ToString("F1");
                }
                else
                {
                    speedHackStr = speedHackMultiplier.ToString("F1");
                }
            }
        }

        private void CachePlayer()
        {
            playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
                playerObj = GameObject.Find("Player");
            if (playerObj == null)
            {
                // ищем любой объект с CharacterController
                CharacterController[] ccs = GameObject.FindObjectsOfType<CharacterController>();
                if (ccs != null && ccs.Length > 0)
                    playerObj = ccs[0].gameObject;
            }
            if (playerObj != null)
            {
                // пробуем найти типичный контроллер от первого лица (имена могут различаться)
                MonoBehaviour[] allMono = playerObj.GetComponents<MonoBehaviour>();
                foreach (var m in allMono)
                {
                    string typeName = m.GetType().Name;
                    if (typeName.Contains("FPScontroller") || typeName.Contains("FirstPersonController") || typeName.Contains("PlayerController"))
                    {
                        fpsController = m;
                        break;
                    }
                }

                playerCollider = playerObj.GetComponent<Collider>();
                playerCC = playerObj.GetComponent<CharacterController>();
                playerRB = playerObj.GetComponent<Rigidbody>();
                playerCached = true;
                LoggerInstance.Msg("Игрок найден: " + playerObj.name);
            }
        }

        private void ToggleNoclip(bool enable)
        {
            if (enable)
            {
                // при включении ноклипа отключаем всё стандартное управление
                if (fpsController != null)
                    fpsController.enabled = false;
                if (playerCC != null)
                    playerCC.enabled = false;
                if (playerCollider != null)
                    playerCollider.enabled = false;
                if (playerRB != null)
                {
                    playerRB.useGravity = false;
                    playerRB.velocity = Vector3.zero;
                }
            }
            else
            {
                // возвращаем обратно
                if (fpsController != null)
                    fpsController.enabled = true;
                if (playerCC != null)
                    playerCC.enabled = true;
                if (playerCollider != null)
                    playerCollider.enabled = true;
                if (playerRB != null)
                    playerRB.useGravity = true;
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
            // возвращаем всё как было при выгрузке мода
            if (noclipEnabled)
            {
                if (fpsController != null) fpsController.enabled = true;
                if (playerCC != null) playerCC.enabled = true;
                if (playerCollider != null) playerCollider.enabled = true;
                if (playerRB != null) playerRB.useGravity = true;
            }
            Cursor.lockState = originalLockMode;
            Cursor.visible = true;
        }
    }
}
