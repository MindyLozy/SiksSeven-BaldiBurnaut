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

        // Рефлексивные типы Photon
        private Type photonNetworkType;
        private Type photonPlayerType;
        private Type raiseEventOptionsType;
        private Type receiverGroupType;

        private void InitPhoton()
        {
            if (photonNetworkType != null) return;
            photonNetworkType = Type.GetType("PhotonNetwork, Assembly-CSharp");
            photonPlayerType = Type.GetType("PhotonPlayer, Assembly-CSharp");
            raiseEventOptionsType = Type.GetType("RaiseEventOptions, Assembly-CSharp");
            receiverGroupType = Type.GetType("ReceiverGroup, Assembly-CSharp");
            if (photonNetworkType == null) MelonLogger.Error("PhotonNetwork не найден");
        }

        private object[] GetPlayerList()
        {
            InitPhoton();
            if (photonNetworkType == null) return new object[0];
            object localPlayer = photonNetworkType.GetProperty("player", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            object othersObj = photonNetworkType.GetProperty("otherPlayers", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            object[] others = othersObj as object[];
            if (others == null) others = new object[0];
            object[] list = new object[others.Length + 1];
            list[0] = localPlayer;
            Array.Copy(others, 0, list, 1, others.Length);
            return list;
        }

        private (string nick, int id, bool isLocal) GetPlayerInfo(object player)
        {
            if (player == null) return ("???", -1, false);
            Type t = player.GetType();
            string nick = t.GetProperty("NickName", BindingFlags.Public | BindingFlags.Instance)?.GetValue(player) as string ?? "???";
            int id = (int)(t.GetProperty("ID", BindingFlags.Public | BindingFlags.Instance)?.GetValue(player) ?? -1);
            bool isLocal = (bool)(t.GetProperty("isLocal", BindingFlags.Public | BindingFlags.Instance)?.GetValue(player) ?? false);
            return (nick, id, isLocal);
        }

        private void CallPhotonStatic(string methodName, params object[] parameters)
        {
            InitPhoton();
            if (photonNetworkType == null) return;
            MethodInfo method = photonNetworkType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            method?.Invoke(null, parameters);
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
                    CallPhotonStatic("CloseConnection", player);
                }
                // Crash
                if (GUI.Button(new Rect(rowRect.x + 175, rowRect.y, 45, 20), "Crash"))
                {
                    byte evCode = 1;
                    object options = Activator.CreateInstance(raiseEventOptionsType);
                    raiseEventOptionsType.GetProperty("TargetActors")?.SetValue(options, new int[] { id });
                    CallPhotonStatic("RaiseEvent", evCode, null, false, options);
                }
                // Freeze
                if (!freezeStates.ContainsKey(id))
                    freezeStates[id] = false;
                bool frozen = GUI.Toggle(new Rect(rowRect.x + 225, rowRect.y, 20, 20), freezeStates[id], "");
                if (frozen != freezeStates[id])
                {
                    freezeStates[id] = frozen;
                    byte evCode = 2;
                    object options = Activator.CreateInstance(raiseEventOptionsType);
                    raiseEventOptionsType.GetProperty("TargetActors")?.SetValue(options, new int[] { id });
                    CallPhotonStatic("RaiseEvent", evCode, new object[] { frozen }, false, options);
                }
                // Take Username
                if (GUI.Button(new Rect(rowRect.x + 250, rowRect.y, 60, 20), "Take Name"))
                {
                    photonNetworkType?.GetProperty("playerName", BindingFlags.Public | BindingFlags.Static)?.SetValue(null, nick);
                }
            }

            float nickY = listRect.yMax + 10;
            GUI.Label(new Rect(windowRect.x + 10, nickY, 100, 20), "Change Nick:");
            newNickname = GUI.TextField(new Rect(windowRect.x + 110, nickY, 100, 20), newNickname);
            if (GUI.Button(new Rect(windowRect.x + 215, nickY, 80, 20), "Apply"))
            {
                photonNetworkType?.GetProperty("playerName", BindingFlags.Public | BindingFlags.Static)?.SetValue(null, newNickname);
            }

            float btnY = nickY + 30;
            if (GUI.Button(new Rect(windowRect.x + 10, btnY, 120, 25), "Crash All"))
            {
                byte evCode = 1;
                object options = Activator.CreateInstance(raiseEventOptionsType);
                object othersEnum = Enum.ToObject(receiverGroupType, 1); // ReceiverGroup.Others
                raiseEventOptionsType.GetProperty("Receivers")?.SetValue(options, othersEnum);
                CallPhotonStatic("RaiseEvent", evCode, null, false, options);
            }
            if (GUI.Button(new Rect(windowRect.x + 140, btnY, 120, 25), "Destroy All"))
            {
                CallPhotonStatic("DestroyAll");
            }
        }
    }
}
