using MelonLoader;
using UnityEngine;
using Photon;
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
        private float listWidth = 300f;

        // freeze player logic
        private Dictionary<int, bool> freezeStates = new Dictionary<int, bool>();

        // changer nick
        private string newNickname = "";

        public void OnGUI()
        {
            if (!Visible) return;

            // drag
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

            // main menu
            GUI.Box(windowRect, "Seven Kick Menu");

            // player list
            Rect listRect = new Rect(windowRect.x + 10, windowRect.y + 35, windowRect.width - 20, 200);
            float contentHeight = PhotonNetwork.playerList.Length * rowHeight;

            // scroller
            if (listRect.Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
            {
                scrollPosition.y += e.delta.y * 20f;
                e.Use();
            }
            scrollPosition.y = Mathf.Clamp(scrollPosition.y, 0, Mathf.Max(0, contentHeight - listRect.height));

            // Рисуем видимые строки игроков
            float startY = listRect.y - scrollPosition.y;
            for (int i = 0; i < PhotonNetwork.playerList.Length; i++)
            {
                float yPos = startY + i * rowHeight;
                if (yPos + rowHeight < listRect.y || yPos > listRect.yMax)
                    continue; // не видно

                PhotonPlayer player = PhotonNetwork.playerList[i];
                Rect rowRect = new Rect(listRect.x, yPos, listRect.width, rowHeight - 1);

                // Фон строки
                if (player.IsLocal) GUI.Box(rowRect, ""); // подсветка себя

                // Ник и Actor
                string label = $"{player.NickName} (ID:{player.ActorNr})";
                GUI.Label(new Rect(rowRect.x + 5, rowRect.y, 120, 20), label);

                // Кнопка Kick
                if (GUI.Button(new Rect(rowRect.x + 130, rowRect.y, 40, 20), "Kick"))
                {
                    PhotonNetwork.CloseConnection(player);
                }
                // Кнопка Crash
                if (GUI.Button(new Rect(rowRect.x + 175, rowRect.y, 45, 20), "Crash"))
                {
                    byte evCode = 1; // код события Crash
                    RaiseEventOptions options = new RaiseEventOptions { TargetActors = new int[] { player.ActorNr } };
                    PhotonNetwork.RaiseEvent(evCode, null, options, SendOptions.SendReliable);
                }
                // Чекбокс Freeze
                if (!freezeStates.ContainsKey(player.ActorNr))
                    freezeStates[player.ActorNr] = false;
                bool frozen = GUI.Toggle(new Rect(rowRect.x + 225, rowRect.y, 20, 20), freezeStates[player.ActorNr], "");
                if (frozen != freezeStates[player.ActorNr])
                {
                    freezeStates[player.ActorNr] = frozen;
                    byte evCode = 2; // код события Freeze
                    RaiseEventOptions options = new RaiseEventOptions { TargetActors = new int[] { player.ActorNr } };
                    PhotonNetwork.RaiseEvent(evCode, new object[] { frozen }, options, SendOptions.SendReliable);
                }
                // take user
                if (GUI.Button(new Rect(rowRect.x + 250, rowRect.y, 60, 20), "Take Name"))
                {
                    PhotonNetwork.networkingPeer.PlayerName = player.NickName;
                }
            }

            // -changer
            float nickY = listRect.yMax + 10;
            GUI.Label(new Rect(windowRect.x + 10, nickY, 100, 20), "Change Nick:");
            newNickname = GUI.TextField(new Rect(windowRect.x + 110, nickY, 100, 20), newNickname);
            if (GUI.Button(new Rect(windowRect.x + 215, nickY, 80, 20), "Apply"))
            {
                PhotonNetwork.playerName = newNickname;
            }

            // power buttons
            float btnY = nickY + 30;
            if (GUI.Button(new Rect(windowRect.x + 10, btnY, 120, 25), "Crash All"))
            {
                byte evCode = 1;
                RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                PhotonNetwork.RaiseEvent(evCode, null, options, SendOptions.SendReliable);
            }
            if (GUI.Button(new Rect(windowRect.x + 140, btnY, 120, 25), "Destroy All"))
            {
                PhotonNetwork.DestroyAll();
            }
        }
    }
}
