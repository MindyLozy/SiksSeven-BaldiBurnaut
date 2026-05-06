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

        private static Type photonNetworkType;
        private static Type raiseEventOptionsType;
        private static Type receiverGroupType;

        private void InitPhotonTypes()
        {
            if (photonNetworkType != null) return;
            photonNetworkType = Type.GetType("PhotonNetwork, Assembly-CSharp") ??
                                Type.GetType("PhotonNetwork, Photon3Unity3D") ??
                                Type.GetType("PhotonNetwork, Assembly-CSharp-firstpass");
            if (photonNetworkType == null) return;
            raiseEventOptionsType = Type.GetType("RaiseEventOptions, Assembly-CSharp") ??
                                   Type.GetType("RaiseEventOptions, Photon3Unity3D") ??
                                   Type.GetType("RaiseEventOptions, Assembly-CSharp-firstpass");
            receiverGroupType = Type.GetType("ReceiverGroup, Assembly-CSharp") ??
                                Type.GetType("ReceiverGroup, Photon3Unity3D") ??
                                Type.GetType("ReceiverGroup, Assembly-CSharp-firstpass");
        }

        private object GetPhotonField(string fieldName)
        {
            InitPhotonTypes();
            if (photonNetworkType == null) return null;
            FieldInfo field = photonNetworkType.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
            return field?.GetValue(null);
        }

        private void SetPhotonField(string fieldName, object value)
        {
            InitPhotonTypes();
            FieldInfo field = photonNetworkType.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
            field?.SetValue(null, value);
        }

        private object CallPhotonStatic(string methodName, params object[] args)
        {
            InitPhotonTypes();
            if (photonNetworkType == null) return null;
            MethodInfo method = photonNetworkType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            return method?.Invoke(null, args);
        }

        private object[] GetPlayerList()
        {
            InitPhotonTypes();
            if (photonNetworkType == null) return new object[0];

            object localPlayer = GetPhotonField("player");
            object othersObj = GetPhotonField("otherPlayers");
            object[] others = othersObj as object[];
            if (others == null) others = new object[0];

            object[] fullList = new object[others.Length + 1];
            fullList[0] = localPlayer;
            Array.Copy(others, 0, fullList, 1, others.Length);
            return fullList;
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
                Rect rowRect = new Rect(listRect.x, yPos, listRect.width, rowHeight - 1);

                // Используем рефлексию для получения свойств
                Type playerType = player.GetType();
                bool isLocal = (bool)playerType.GetProperty("isLocal").GetValue(player);
                string nick = (string)playerType.GetProperty("NickName").GetValue(player);
                int id = (int)playerType.GetProperty("ID").GetValue(player);

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
                    PropertyInfo targetActorsProp = raiseEventOptionsType.GetProperty("TargetActors");
                    targetActorsProp.SetValue(options, new int[] { id }, null);
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
                    PropertyInfo targetActorsProp = raiseEventOptionsType.GetProperty("TargetActors");
                    targetActorsProp.SetValue(options, new int[] { id }, null);
                    object[] content = new object[] { frozen };
                    CallPhotonStatic("RaiseEvent", evCode, content, false, options);
                }
                // Take Username
                if (GUI.Button(new Rect(rowRect.x + 250, rowRect.y, 60, 20), "Take Name"))
                {
                    SetPhotonField("playerName", nick);
                }
            }

            float nickY = listRect.yMax + 10;
            GUI.Label(new Rect(windowRect.x + 10, nickY, 100, 20), "Change Nick:");
            newNickname = GUI.TextField(new Rect(windowRect.x + 110, nickY, 100, 20), newNickname);
            if (GUI.Button(new Rect(windowRect.x + 215, nickY, 80, 20), "Apply"))
            {
                SetPhotonField("playerName", newNickname);
            }

            float btnY = nickY + 30;
            if (GUI.Button(new Rect(windowRect.x + 10, btnY, 120, 25), "Crash All"))
            {
                byte evCode = 1;
                object options = Activator.CreateInstance(raiseEventOptionsType);
                // ReceiverGroup.Others = 1
                object othersEnum = Enum.ToObject(receiverGroupType, 1);
                PropertyInfo receiversProp = raiseEventOptionsType.GetProperty("Receivers");
                receiversProp.SetValue(options, othersEnum, null);
                CallPhotonStatic("RaiseEvent", evCode, null, false, options);
            }
            if (GUI.Button(new Rect(windowRect.x + 140, btnY, 120, 25), "Destroy All"))
            {
                CallPhotonStatic("DestroyAll");
            }
        }
    }
}
