using MelonLoader;
using UnityEngine;
using Il2Cpp;

[assembly: MelonInfo(typeof(SiksSevenMenu.Main), "SiksSeven Menu", "1.3.0", "LOLWorking")]
[assembly: MelonGame(null, null)]

namespace SiksSevenMenu
{
    public class Main : MelonMod
    {
        private bool noclipEnabled = false;
        private bool speedHackEnabled = false;
        private bool menuVisible = false;
        private bool showItemGiver = false;
        private bool flyEnabled = false;

        private float noclipSpeed = 10f;
        private float speedHackMultiplier = 2f;
        private float flySpeed = 10f;

        private string noclipInput = "10";
        private string speedInput = "2";
        private string gravityInput = "9.81";
        private string jumpInput = "2";
        private string flySpeedInput = "10";

        private bool infiniteStamina = false;
        private bool infiniteItems = false;

        private GameObject playerObj;
        private bool playerCached = false;
        private MonoBehaviour fpsController;
        private Collider playerCollider;
        private CharacterController playerCC;
        private Rigidbody playerRB;
        private PlayerPrototypeScript playerPrototype;

        // back gravity after fly
        private float savedGravity = 9.81f;

        private CursorLockMode originalLockMode;
        private ItemGiverWindow itemGiverWindow;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("SiksSeven v1.1.0: True");
            noclipInput = noclipSpeed.ToString("F1");
            speedInput = speedHackMultiplier.ToString("F1");
            gravityInput = "9.81";
            jumpInput = "2";
            flySpeedInput = flySpeed.ToString("F1");

            itemGiverWindow = new ItemGiverWindow();
        }

        public override void OnUpdate()
        {
            if (!playerCached || playerObj == null)
            {
                CachePlayer();
                if (playerCached)
                {
                    if (noclipEnabled) ToggleNoclip(true);
                    if (flyEnabled) ToggleFly(true);
                }
            }
            if (!playerCached) return;

            if (Input.GetKeyDown(KeyCode.V))
            {
                noclipEnabled = !noclipEnabled;
                if (noclipEnabled) { flyEnabled = false; ToggleFly(false); }
                LoggerInstance.Msg($"Noclip: {noclipEnabled}");
                ToggleNoclip(noclipEnabled);
            }
            if (Input.GetKeyDown(KeyCode.X))
            {
                speedHackEnabled = !speedHackEnabled;
                LoggerInstance.Msg($"SpeedHack: {speedHackEnabled} (x{speedHackMultiplier})");
            }
            if (Input.GetKeyDown(KeyCode.Z))
            {
                flyEnabled = !flyEnabled;
                if (flyEnabled) { noclipEnabled = false; ToggleNoclip(false); }
                LoggerInstance.Msg($"Fly: {flyEnabled}");
                ToggleFly(flyEnabled);
            }
            if (Input.GetKeyDown(KeyCode.F1))
            {
                menuVisible = !menuVisible;
                UpdateCursorState();
            }
            if (Input.GetKeyDown(KeyCode.Insert))
            {
                showItemGiver = !showItemGiver;
                itemGiverWindow.Visible = showItemGiver;
                UpdateCursorState();
            }

            if (infiniteStamina && playerPrototype != null)
            {
                playerPrototype.MaxStamina = 999999999f;
                playerPrototype.stamina = 999999999f;
            }

            if (infiniteItems)
            {
                MonoBehaviour[] allMono = Object.FindObjectsOfType<MonoBehaviour>();
                foreach (var m in allMono)
                    if (m is ItemScript item)
                        item._uses = 9999;
            }

            // Noclip 
            if (noclipEnabled && playerObj != null)
            {
                if (fpsController != null && fpsController.enabled) fpsController.enabled = false;
                if (playerCC != null) playerCC.enabled = false;
                if (playerRB != null) playerRB.useGravity = false;
                if (playerCollider != null) playerCollider.enabled = false;

                if (Camera.main == null) return;

                float moveF = Input.GetKey(KeyCode.W) ? 1f : 0f;
                float moveB = Input.GetKey(KeyCode.S) ? 1f : 0f;
                float moveL = Input.GetKey(KeyCode.A) ? 1f : 0f;
                float moveR = Input.GetKey(KeyCode.D) ? 1f : 0f;
                float moveUp = Input.GetKey(KeyCode.Space) ? 1f : 0f;
                float moveDown = Input.GetKey(KeyCode.LeftControl) ? 1f : 0f;

                Vector3 dir = Camera.main.transform.forward * (moveF - moveB)
                            + Camera.main.transform.right * (moveR - moveL)
                            + Camera.main.transform.up * (moveUp - moveDown);
                if (dir.magnitude > 0.01f)
                {
                    dir.Normalize();
                    playerObj.transform.position += dir * noclipSpeed * Time.unscaledDeltaTime;
                }
            }
            // Fly (fix)
            else if (flyEnabled && playerObj != null)
            {
                if (fpsController != null && fpsController.enabled) fpsController.enabled = false;
                if (playerCC != null) playerCC.enabled = true;    // коллизии
                if (playerCollider != null) playerCollider.enabled = true;
                if (playerRB != null) playerRB.useGravity = false;

                // null
                if (playerPrototype != null)
                    playerPrototype.MainGravity = 0f;

                if (Camera.main == null) return;

                float moveF = Input.GetKey(KeyCode.W) ? 1f : 0f;
                float moveB = Input.GetKey(KeyCode.S) ? 1f : 0f;
                float moveL = Input.GetKey(KeyCode.A) ? 1f : 0f;
                float moveR = Input.GetKey(KeyCode.D) ? 1f : 0f;
                float moveUp = Input.GetKey(KeyCode.Space) ? 1f : 0f;
                float moveDown = Input.GetKey(KeyCode.LeftControl) ? 1f : 0f;

                Vector3 dir = Camera.main.transform.forward * (moveF - moveB)
                            + Camera.main.transform.right * (moveR - moveL)
                            + Camera.main.transform.up * (moveUp - moveDown);
                if (dir.magnitude > 0.01f)
                {
                    dir.Normalize();
                    Vector3 motion = dir * flySpeed * Time.unscaledDeltaTime;
                    if (playerCC != null)
                        playerCC.Move(motion);
                    else
                        playerObj.transform.position += motion; // fallback
                }
            }
            // SpeedHack
            else if (speedHackEnabled && playerObj != null)
            {
                float moveH = Input.GetAxis("Horizontal");
                float moveV = Input.GetAxis("Vertical");
                if (Mathf.Abs(moveH) > 0.01f || Mathf.Abs(moveV) > 0.01f)
                {
                    Vector3 extra = (playerObj.transform.forward * moveV + playerObj.transform.right * moveH).normalized
                                    * (speedHackMultiplier - 1f) * Time.deltaTime * 5f;
                    if (playerCC != null && playerCC.enabled)
                        playerCC.Move(extra);
                    else if (playerRB != null)
                        playerRB.MovePosition(playerObj.transform.position + extra);
                    else
                        playerObj.transform.position += extra;
                }
            }
        }

        public override void OnGUI()
        {
            if (menuVisible)
            {
                float mw = 320f, mh = 560f;   // уменьшили высоту без Spray
                float mx = 20f, my = 20f;

                GUI.Box(new Rect(mx, my, mw, mh), "");
                GUI.Label(new Rect(mx + 10, my + 10, mw - 20, 30), "SiksSeven Menu");

                int yOff = 50;

                if (GUI.Button(new Rect(mx + 10, my + yOff, mw - 20, 30), $"Noclip: {(noclipEnabled ? "ON" : "OFF")}"))
                {
                    noclipEnabled = !noclipEnabled;
                    if (noclipEnabled) { flyEnabled = false; ToggleFly(false); }
                    ToggleNoclip(noclipEnabled);
                }
                yOff += 40;

                if (GUI.Button(new Rect(mx + 10, my + yOff, mw - 20, 30), $"SpeedHack: {(speedHackEnabled ? "ON" : "OFF")}"))
                    speedHackEnabled = !speedHackEnabled;
                yOff += 40;

                GUI.Label(new Rect(mx + 10, my + yOff, 100, 20), "Noclip Speed:");
                noclipInput = GUI.TextField(new Rect(mx + 120, my + yOff, 70, 20), noclipInput);
                if (GUI.Button(new Rect(mx + 200, my + yOff, 80, 20), "Apply"))
                {
                    if (float.TryParse(noclipInput, out float v))
                    {
                        noclipSpeed = Mathf.Clamp(v, 0.1f, 100f);
                        noclipInput = noclipSpeed.ToString("F1");
                    }
                    else noclipInput = noclipSpeed.ToString("F1");
                }
                yOff += 30;

                GUI.Label(new Rect(mx + 10, my + yOff, 130, 20), "Speed Multiplier:");
                speedInput = GUI.TextField(new Rect(mx + 150, my + yOff, 70, 20), speedInput);
                if (GUI.Button(new Rect(mx + 230, my + yOff, 50, 20), "Apply"))
                {
                    if (float.TryParse(speedInput, out float v))
                    {
                        speedHackMultiplier = Mathf.Clamp(v, 0.1f, 20f);
                        speedInput = speedHackMultiplier.ToString("F1");
                    }
                    else speedInput = speedHackMultiplier.ToString("F1");
                }
                yOff += 40;

                GUI.Label(new Rect(mx + 10, my + yOff, 100, 20), "Player Gravity:");
                gravityInput = GUI.TextField(new Rect(mx + 120, my + yOff, 70, 20), gravityInput);
                if (GUI.Button(new Rect(mx + 200, my + yOff, 80, 20), "Apply"))
                {
                    if (float.TryParse(gravityInput, out float v) && playerPrototype != null)
                    {
                        playerPrototype.MainGravity = v;
                        gravityInput = v.ToString("F2");
                        savedGravity = v;   // помним, что установил игрок
                    }
                    else gravityInput = (playerPrototype != null ? playerPrototype.MainGravity : 9.81f).ToString("F2");
                }
                yOff += 40;

                GUI.Label(new Rect(mx + 10, my + yOff, 100, 20), "Jump Height:");
                jumpInput = GUI.TextField(new Rect(mx + 120, my + yOff, 70, 20), jumpInput);
                if (GUI.Button(new Rect(mx + 200, my + yOff, 80, 20), "Apply"))
                {
                    if (float.TryParse(jumpInput, out float v) && playerPrototype != null)
                    {
                        playerPrototype._jumpHeight = v;
                        jumpInput = v.ToString("F1");
                    }
                    else jumpInput = (playerPrototype != null ? playerPrototype._jumpHeight : 2f).ToString("F1");
                }
                yOff += 40;

                bool newStamina = GUI.Toggle(new Rect(mx + 10, my + yOff, 200, 20), infiniteStamina, "Infinite Stamina");
                if (newStamina != infiniteStamina)
                {
                    infiniteStamina = newStamina;
                    if (playerPrototype != null)
                    {
                        playerPrototype.MaxStamina = infiniteStamina ? 999999999f : 100f;
                        playerPrototype.stamina = infiniteStamina ? 999999999f : 100f;
                    }
                }
                yOff += 30;

                bool newItems = GUI.Toggle(new Rect(mx + 10, my + yOff, 200, 20), infiniteItems, "Infinite Items");
                if (newItems != infiniteItems)
                {
                    infiniteItems = newItems;
                    MonoBehaviour[] allMono = Object.FindObjectsOfType<MonoBehaviour>();
                    foreach (var m in allMono)
                        if (m is ItemScript item)
                            item._uses = infiniteItems ? 9999 : 0;
                }
                yOff += 40;

                if (GUI.Button(new Rect(mx + 10, my + yOff, mw - 20, 30), $"Fly: {(flyEnabled ? "ON" : "OFF")}"))
                {
                    flyEnabled = !flyEnabled;
                    if (flyEnabled) { noclipEnabled = false; ToggleNoclip(false); }
                    ToggleFly(flyEnabled);
                }
                yOff += 40;

                GUI.Label(new Rect(mx + 10, my + yOff, 100, 20), "Fly Speed:");
                flySpeedInput = GUI.TextField(new Rect(mx + 120, my + yOff, 70, 20), flySpeedInput);
                if (GUI.Button(new Rect(mx + 200, my + yOff, 80, 20), "Apply"))
                {
                    if (float.TryParse(flySpeedInput, out float v))
                    {
                        flySpeed = Mathf.Clamp(v, 0.1f, 100f);
                        flySpeedInput = flySpeed.ToString("F1");
                    }
                    else flySpeedInput = flySpeed.ToString("F1");
                }
            }

            if (showItemGiver)
                itemGiverWindow.OnGUI();
        }

        private void CachePlayer()
        {
            playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null) playerObj = GameObject.Find("Player");
            if (playerObj == null)
            {
                CharacterController[] ccs = Object.FindObjectsOfType<CharacterController>();
                if (ccs.Length > 0) playerObj = ccs[0].gameObject;
            }
            if (playerObj != null)
            {
                foreach (var m in playerObj.GetComponents<MonoBehaviour>())
                {
                    string n = m.GetType().Name;
                    if (n.Contains("FPScontroller") || n.Contains("FirstPersonController") || n.Contains("PlayerController"))
                    {
                        fpsController = m;
                        break;
                    }
                }
                playerCollider = playerObj.GetComponent<Collider>();
                playerCC = playerObj.GetComponent<CharacterController>();
                playerRB = playerObj.GetComponent<Rigidbody>();
                playerPrototype = playerObj.GetComponent<PlayerPrototypeScript>();
                if (playerPrototype != null)
                    savedGravity = playerPrototype.MainGravity;  // запомним гравитацию по умолчанию
                playerCached = true;
                LoggerInstance.Msg("Игрок найден: " + playerObj.name);
            }
        }

        private void ToggleNoclip(bool enable)
        {
            if (enable)
            {
                if (fpsController != null) fpsController.enabled = false;
                if (playerCC != null) playerCC.enabled = false;
                if (playerCollider != null) playerCollider.enabled = false;
                if (playerRB != null) { playerRB.useGravity = false; playerRB.velocity = Vector3.zero; }
            }
            else
            {
                if (fpsController != null) fpsController.enabled = true;
                if (playerCC != null) playerCC.enabled = true;
                if (playerCollider != null) playerCollider.enabled = true;
                if (playerRB != null) playerRB.useGravity = true;
            }
        }

        private void ToggleFly(bool enable)
        {
            if (enable)
            {
                if (playerPrototype != null)
                {
                    savedGravity = playerPrototype.MainGravity;   // на случай, если ещё не сохранили
                    playerPrototype.MainGravity = 0f;
                }
                if (fpsController != null) fpsController.enabled = false;
                if (playerCC != null) playerCC.enabled = true;
                if (playerCollider != null) playerCollider.enabled = true;
                if (playerRB != null) playerRB.useGravity = false;
            }
            else
            {
                if (playerPrototype != null)
                    playerPrototype.MainGravity = savedGravity;   // возвращаем ту гравитацию, что была
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
            if (noclipEnabled || flyEnabled)
            {
                if (fpsController != null) fpsController.enabled = true;
                if (playerCC != null) playerCC.enabled = true;
                if (playerCollider != null) playerCollider.enabled = true;
                if (playerRB != null) playerRB.useGravity = true;
                if (playerPrototype != null)
                    playerPrototype.MainGravity = savedGravity;
            }
            Cursor.lockState = originalLockMode;
            Cursor.visible = true;
        }
    }
}
