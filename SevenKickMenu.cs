using MelonLoader;
using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace SiksSevenMenu
{
    public class SevenKickMenuWindow
    {
        public bool Visible = false;

        private Rect windowRect = new Rect(600f, 20f, 320f, 450f);
        private bool isDragging = false;
        private Vector2 dragOffset;
        private Vector2 scrollPosition = Vector2.zero;
        private float rowHeight = 30f;

        private Dictionary<int, bool> freezeStates = new Dictionary<int, bool>();
        private string newNickname = "";

        // Ссылки на сетевые методы игры (из Assembly-CSharp)
        private static Type networkManagerType;
        private static object networkManagerInstance;

        private void InitNetworkManager()
        {
            if (networkManagerType != null) return;
            // Попробуем найти типичный менеджер сети для Baldi's Basics с Photon
            networkManagerType = Type.GetType("BaldiNetworkManager, Assembly-CSharp") ??
                                 Type.GetType("NetworkManager, Assembly-CSharp") ??
                                 Type.GetType("GameManager, Assembly-CSharp");
            if (networkManagerType != null)
            {
                // Ищем статический экземпляр или создаём
                PropertyInfo instanceProp = networkManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceProp != null)
                    networkManagerInstance = instanceProp.GetValue(null);
                else
                    networkManagerInstance = null;
            }
        }

        private object[] GetPlayerList()
        {
            InitNetworkManager();
            if (networkManagerType == null) return new object[0];

            // Ожидаем, что в менеджере есть поле/свойство Players (список PhotonPlayer или actorID)
            PropertyInfo playersProp = networkManagerType.GetProperty("Players", BindingFlags.Public | BindingFlags.Instance);
            if (playersProp != null && networkManagerInstance != null)
            {
                object playersObj = playersProp.GetValue(networkManagerInstance);
                if (playersObj is System.Collections.IEnumerable enumerable)
                {
                    var list = new List<object>();
                    foreach (var p in enumerable)
                        list.Add(p);
                    return list.ToArray();
                }
            }
            // Если нет, попробуем PhotonNetwork напрямую через рефлексию (как раньше)
            Type photonType = Type.GetType("PhotonNetwork, Assembly-CSharp");
            if (photonType != null)
            {
                object localPlayer = photonType.GetProperty("player")?.GetValue(null);
                object othersObj = photonType.GetProperty("otherPlayers")?.GetValue(null);
                object[] others = othersObj as object[];
                if (others == null) others = new object[0];
                object[] full = new object[others.Length + 1];
                full[0] = localPlayer;
                Array.Copy(others, 0, full, 1, others.Length);
                return full;
            }
            return new object[0];
        }

        private (string nick, int id, bool isLocal) GetPlayerInfo(object player)
        {
            if (player == null) return ("", -1, false);
            Type t = player.GetType();
            string nick = (string)t.GetProperty("NickName")?.GetValue(player) ?? "???";
            int id = (int)(t.GetProperty("ID")?.GetValue(player) ?? -1);
            bool isLocal = (bool)(t.GetProperty("isLocal")?.GetValue(player) ?? false);
            return (nick, id, isLocal);
        }

        private void CallNetworkMethod(string methodName, int actorID)
        {
            InitNetworkManager();
            if (networkManagerType != null && networkManagerInstance != null)
            {
                MethodInfo method = networkManagerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(networkManagerInstance, new object[] { actorID });
                    MelonLogger.Msg($"Вызван {methodName} для actor {actorID}");
                    return;
                }
            }
            // Fallback: PhotonNetwork.CloseConnection через рефлексию
            Type photonType = Type.GetType("PhotonNetwork, Assembly-CSharp");
            if (photonType != null)
            {
                MethodInfo closeMethod = photonType.GetMethod("CloseConnection", BindingFlags.Public | BindingFlags.Static);
                if (closeMethod != null)
                {
                    // нужно передать объект PhotonPlayer, найдём его по actorID? Это сложно.
                    MelonLogger.Warning("Не удалось вызвать Kick: используйте прямые методы BaldiNetworkManager");
                }
            }
            MelonLogger.Error($"Метод {methodName} не найден в сетевом менеджере.");
        }

        public void OnGUI()
        {
            if (!Visible) return;

            object[] playerList = GetPlayerList();

            Rect headerRect = new Rect(windowRect.x, windowRect.y, windowRect.width, 25);
            Event e = Event.current;
            if (e.type == EventType.MouseDown && headerRect.Contains(e.mousePosition))
            {
                isDragging = true;
                dragOffset = e.mousePosition - new Vector2(windowRect.x, windowRect.y);
                e.Use();
            }
            if (e.type == EventType.MouseDrag && isDragging)
            {
                windowRect.x = e.mousePosition.x - dragOffset.x;
                windowRect.y = e.mousePosition.y - dragOffset.y;
                e.Use();
            }
            if (e.type == EventType.MouseUp) isDragging = false;

            GUI.Box(windowRect, "Seven Kick Menu");

            Rect listRect = new Rect(windowRect.x + 10, windowRect.y + 35, windowRect.width - 20, 200);
            float contentHeight = playerList.Length * rowHeight;

            if (listRect.Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
            {
                scrollPosition.y += e.delta.y * 20f;
                e.Use();
            }
            scrollPosition.y = Mathf.Clamp(scrollPosition.y, 0, Mathf.Max(0, contentHeight - listRect.height));

            float startY = listRect.y - scrollPosition.y;
            for (int i = 0; i < playerList.Length; i++)
            {
                float yPos = startY + i * rowHeight;
                if (yPos + rowHeight < listRect.y || yPos > listRect.yMax) continue;

                object player = playerList[i];
                var (nick, id, isLocal) = GetPlayerInfo(player);
                Rect rowRect = new Rect(listRect.x, yPos, listRect.width, rowHeight - 1);

                if (isLocal) GUI.Box(rowRect, "");

                GUI.Label(new Rect(rowRect.x + 5, rowRect.y, 120, 20), $"{nick} (ID:{id})");

                // Kick
                if (GUI.Button(new Rect(rowRect.x + 130, rowRect.y, 40, 20), "Kick"))
                {
                    CallNetworkMethod("KickPlayer", id);
                }
                // Crash
                if (GUI.Button(new Rect(rowRect.x + 175, rowRect.y, 45, 20), "Crash"))
                {
                    // Предположим, что CrashPlayer вызывает загрузку сцены Lose
                    CallNetworkMethod("CrashPlayer", id);
                }
                // Freeze (чекбокс) — используем RPC или метод
                if (!freezeStates.ContainsKey(id))
                    freezeStates[id] = false;
                bool frozen = GUI.Toggle(new Rect(rowRect.x + 225, rowRect.y, 20, 20), freezeStates[id], "");
                if (frozen != freezeStates[id])
                {
                    freezeStates[id] = frozen;
                    // Вызываем FreezePlayer(id, frozen) или отправляем RPC
                    InitNetworkManager();
                    if (networkManagerType != null && networkManagerInstance != null)
                    {
                        MethodInfo freezeMethod = networkManagerType.GetMethod("FreezePlayer", BindingFlags.Public | BindingFlags.Instance);
                        if (freezeMethod != null)
                            freezeMethod.Invoke(networkManagerInstance, new object[] { id, frozen });
                        else
                            MelonLogger.Warning("FreezePlayer не найден. Используйте другие методы.");
                    }
                }
                // Take Username
                if (GUI.Button(new Rect(rowRect.x + 250, rowRect.y, 60, 20), "Take Name"))
                {
                    // Меняет локальный ник на ник выбранного игрока
                    Type photonType = Type.GetType("PhotonNetwork, Assembly-CSharp");
                    if (photonType != null)
                    {
                        photonType.GetProperty("playerName")?.SetValue(null, nick);
                    }
                }
            }

            float nickY = listRect.yMax + 10;
            GUI.Label(new Rect(windowRect.x + 10, nickY, 100, 20), "Change Nick:");
            newNickname = GUI.TextField(new Rect(windowRect.x + 110, nickY, 100, 20), newNickname);
            if (GUI.Button(new Rect(windowRect.x + 215, nickY, 80, 20), "Apply"))
            {
                Type photonType = Type.GetType("PhotonNetwork, Assembly-CSharp");
                photonType?.GetProperty("playerName")?.SetValue(null, newNickname);
            }

            float btnY = nickY + 30;
            if (GUI.Button(new Rect(windowRect.x + 10, btnY, 120, 25), "Crash All"))
            {
                InitNetworkManager();
                if (networkManagerType != null && networkManagerInstance != null)
                {
                    MethodInfo crashAll = networkManagerType.GetMethod("CrashAllPlayers", BindingFlags.Public | BindingFlags.Instance);
                    crashAll?.Invoke(networkManagerInstance, null);
                }
            }
            if (GUI.Button(new Rect(windowRect.x + 140, btnY, 120, 25), "Destroy All"))
            {
                InitNetworkManager();
                if (networkManagerType != null && networkManagerInstance != null)
                {
                    MethodInfo destroyAll = networkManagerType.GetMethod("DestroyAllPlayers", BindingFlags.Public | BindingFlags.Instance);
                    destroyAll?.Invoke(networkManagerInstance, null);
                }
            }
        }
    }
}
