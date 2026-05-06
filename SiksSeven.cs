using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using Il2Cpp; // пространство имён игры (содержит Item, PlayerPrototypeScript и т.д.)

[assembly: MelonInfo(typeof(SiksSevenMenu.Main), "SiksSeven Menu", "1.1.0", "LOLWorking")]
[assembly: MelonGame(null, null)]

namespace SiksSevenMenu
{
    public class Main : MelonMod
    {
        // основные читы
        private bool noclipEnabled = false;
        private bool speedHackEnabled = false;
        private bool menuVisible = false;
        private bool showItemGiver = false;    // окно Item Giver

        private float noclipSpeed = 10f;
        private float speedHackMultiplier = 2f;

        // строки ввода
        private string noclipInput = "10";
        private string speedInput = "2";
        private string gravityInput = "9.81";
        private string jumpInput = "2";

        // дополнительные читы
        private bool infiniteStamina = false;
        private bool infiniteItems = false;

        // игрок
        private GameObject playerObj;
        private bool playerCached = false;
        private MonoBehaviour fpsController;
        private Collider playerCollider;
        private CharacterController playerCC;
        private Rigidbody playerRB;
        private PlayerPrototypeScript playerPrototype; // для доступа к полям

        private CursorLockMode originalLockMode;

        // Item Giver (окно)
        private ItemGiverWindow itemGiverWindow;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("true : 67");
            noclipInput = noclipSpeed.ToString("F1");
            speedInput = speedHackMultiplier.ToString("F1");
            gravityInput = "9.81";
            jumpInput = "2";

            // подписка на смену сцены
            SceneManager.sceneLoaded += OnSceneLoaded;

            itemGiverWindow = new ItemGiverWindow();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            playerCached = false; // сбросим кэш, чтобы найти нового игрока
            playerObj = null;
            fpsController = null;
            playerCollider = null;
            playerCC = null;
            playerRB = null;
            playerPrototype = null;

            // повторно применим активные читы, как только игрок найдётся
            // они применятся в следующем OnUpdate при CachePlayer()
        }

        public override void OnUpdate()
        {
            if (!playerCached)
            {
                CachePlayer();
                if (playerCached)
                {
                    // когда игрок найден, применяем все активные читы
                    if (noclipEnabled) ToggleNoclip(true);
                    // остальное применяется через флаги
                }
                if (!playerCached) return;
            }

            // клавиши активации
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

            if (Input.GetKeyDown(KeyCode.Insert))
            {
                showItemGiver = !showItemGiver;
            }

            // применение бесконечной стамины каждый кадр (на случай обновления игры)
            if (infiniteStamina && playerPrototype != null)
            {
                playerPrototype.MaxStamina = 999999999f;
                playerPrototype.stamina = 999999999f;
            }

            // применение бесконечных предметов (ко всем ItemScript на сцене)
            if (infiniteItems)
            {
                ItemScript[] items = GameObject.FindObjectsOfType<ItemScript>();
                foreach (var item in items)
                {
                    item._uses = 9999;
                }
            }

            // ноклип
            if (noclipEnabled && playerObj != null)
            {
                if (fpsController != null && fpsController.enabled)
                    fpsController.enabled = false;
                if (playerCC != null && playerCC.enabled)
                    playerCC.enabled = false;
                if (playerRB != null && playerRB.useGravity)
                    playerRB.useGravity = false;

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
            // спидхак (только когда ноклип выключен)
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

            float menuW = 320f, menuH = 480f;
            float x = 20f;
            float y = 20f;

            GUI.Box(new Rect(x, y, menuW, menuH), "");
            GUI.Label(new Rect(x + 10, y + 10, menuW - 20, 30), "SiksSeven Menu");

            int lineY = 50;

            // Noclip
            if (GUI.Button(new Rect(x + 10, y + lineY, menuW - 20, 30), $"Noclip: {(noclipEnabled ? "ON" : "OFF")}"))
            {
                noclipEnabled = !noclipEnabled;
                ToggleNoclip(noclipEnabled);
            }
            lineY += 40;

            // SpeedHack
            if (GUI.Button(new Rect(x + 10, y + lineY, menuW - 20, 30), $"SpeedHack: {(speedHackEnabled ? "ON" : "OFF")}"))
            {
                speedHackEnabled = !speedHackEnabled;
            }
            lineY += 40;

            // Noclip Speed
            GUI.Label(new Rect(x + 10, y + lineY, 100, 20), "Noclip Speed:");
            noclipInput = GUI.TextField(new Rect(x + 120, y + lineY, 70, 20), noclipInput);
            if (GUI.Button(new Rect(x + 200, y + lineY, 80, 20), "Apply"))
            {
                if (float.TryParse(noclipInput, out float val))
                {
                    noclipSpeed = Mathf.Clamp(val, 0.1f, 100f);
                    noclipInput = noclipSpeed.ToString("F1");
                }
                else noclipInput = noclipSpeed.ToString("F1");
            }
            lineY += 30;

            // Speed Multiplier
            GUI.Label(new Rect(x + 10, y + lineY, 130, 20), "Speed Multiplier:");
            speedInput = GUI.TextField(new Rect(x + 150, y + lineY, 70, 20), speedInput);
            if (GUI.Button(new Rect(x + 230, y + lineY, 50, 20), "Apply"))
            {
                if (float.TryParse(speedInput, out float val))
                {
                    speedHackMultiplier = Mathf.Clamp(val, 0.1f, 20f);
                    speedInput = speedHackMultiplier.ToString("F1");
                }
                else speedInput = speedHackMultiplier.ToString("F1");
            }
            lineY += 40;

            // Player Gravity
            GUI.Label(new Rect(x + 10, y + lineY, 100, 20), "Player Gravity:");
            gravityInput = GUI.TextField(new Rect(x + 120, y + lineY, 70, 20), gravityInput);
            if (GUI.Button(new Rect(x + 200, y + lineY, 80, 20), "Apply"))
            {
                if (float.TryParse(gravityInput, out float val))
                {
                    if (playerPrototype != null)
                    {
                        playerPrototype.MainGravity = val;
                        gravityInput = val.ToString("F2");
                    }
                }
                else gravityInput = (playerPrototype != null ? playerPrototype.MainGravity : 9.81f).ToString("F2");
            }
            lineY += 40;

            // Jump Height
            GUI.Label(new Rect(x + 10, y + lineY, 100, 20), "Jump Height:");
            jumpInput = GUI.TextField(new Rect(x + 120, y + lineY, 70, 20), jumpInput);
            if (GUI.Button(new Rect(x + 200, y + lineY, 80, 20), "Apply"))
            {
                if (float.TryParse(jumpInput, out float val))
                {
                    if (playerPrototype != null)
                    {
                        playerPrototype._jumpHeight = val;
                        jumpInput = val.ToString("F1");
                    }
                }
                else jumpInput = (playerPrototype != null ? playerPrototype._jumpHeight : 2f).ToString("F1");
            }
            lineY += 40;

            // Infinite Stamina (чекбокс)
            bool newStamina = GUI.Toggle(new Rect(x + 10, y + lineY, 200, 20), infiniteStamina, "Infinite Stamina");
            if (newStamina != infiniteStamina)
            {
                infiniteStamina = newStamina;
                if (playerPrototype != null)
                {
                    if (infiniteStamina)
                    {
                        playerPrototype.MaxStamina = 999999999f;
                        playerPrototype.stamina = 999999999f;
                    }
                    else
                    {
                        playerPrototype.MaxStamina = 100f;
                        playerPrototype.stamina = 100f;
                    }
                }
            }
            lineY += 30;

            // Infinite Items (чекбокс)
            bool newItems = GUI.Toggle(new Rect(x + 10, y + lineY, 200, 20), infiniteItems, "Infinite Items");
            if (newItems != infiniteItems)
            {
                infiniteItems = newItems;
                ItemScript[] items = GameObject.FindObjectsOfType<ItemScript>();
                foreach (var item in items)
                {
                    item._uses = infiniteItems ? 9999 : 0;
                }
            }
            lineY += 30;
        }

        private void CachePlayer()
        {
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
                // поиск FPS-контроллера
                MonoBehaviour[] monos = playerObj.GetComponents<MonoBehaviour>();
                foreach (var m in monos)
                {
                    string t = m.GetType().Name;
                    if (t.Contains("FPScontroller") || t.Contains("FirstPersonController") || t.Contains("PlayerController"))
                    {
                        fpsController = m;
                        break;
                    }
                }
                playerCollider = playerObj.GetComponent<Collider>();
                playerCC = playerObj.GetComponent<CharacterController>();
                playerRB = playerObj.GetComponent<Rigidbody>();
                playerPrototype = playerObj.GetComponent<PlayerPrototypeScript>();

                playerCached = true;
                LoggerInstance.Msg("Игрок найден: " + playerObj.name +
                    (playerPrototype != null ? " + PlayerPrototypeScript" : ""));
            }
        }

        private void ToggleNoclip(bool enable)
        {
            if (enable)
            {
                if (fpsController != null) fpsController.enabled = false;
                if (playerCC != null) playerCC.enabled = false;
                if (playerCollider != null) playerCollider.enabled = false;
                if (playerRB != null)
                {
                    playerRB.useGravity = false;
                    playerRB.velocity = Vector3.zero;
                }
            }
            else
            {
                if (fpsController != null) fpsController.enabled = true;
                if (playerCC != null) playerCC.enabled = true;
                if (playerCollider != null) playerCollider.enabled = true;
                if (playerRB != null) playerRB.useGravity = true;
            }
        }

        private void UpdateCursorState()
        {
            if (menuVisible || showItemGiver)
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
            if (noclipEnabled)
            {
                if (fpsController != null) fpsController.enabled = true;
                if (playerCC != null) playerCC.enabled = true;
                if (playerCollider != null) playerCollider.enabled = true;
                if (playerRB != null) playerRB.useGravity = true;
            }
            Cursor.lockState = originalLockMode;
            Cursor.visible = true;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}
