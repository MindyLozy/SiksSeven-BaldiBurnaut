using MelonLoader;
using UnityEngine;
using Il2Cpp;

namespace SiksSevenMenu
{
    public class ItemGiverWindow
    {
        public bool Visible = false;

        private Rect windowRect = new Rect(360f, 20f, 220f, 350f);
        private bool isDragging = false;
        private Vector2 dragOffset;
        private float scrollOffset = 0f;
        private float itemHeight = 28f;

        private readonly string[] itemNames = new string[]
        {
            "Bsoda", "Apple", "Banana", "Quarter",
            "GrapplingHook", "Dirty Chalk", "Present",
            "SafetyScissors", "SnowBall", "SwingingDoorLock",
            "Tape", "ZestyBar"
        };

        public void OnGUI()
        {
            if (!Visible) return;


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
            if (e.type == EventType.MouseUp)
                isDragging = false;

            
            GUI.Box(windowRect, "Item Giver");

           
            Rect listRect = new Rect(windowRect.x + 10, windowRect.y + 35, windowRect.width - 20, windowRect.height - 50);
            float contentHeight = itemNames.Length * itemHeight;
            float viewHeight = listRect.height;

            if (listRect.Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
            {
                scrollOffset += e.delta.y * 20f;
                e.Use();
            }
            scrollOffset = Mathf.Clamp(scrollOffset, 0f, Mathf.Max(0, contentHeight - viewHeight));

            int firstVisible = Mathf.FloorToInt(scrollOffset / itemHeight);
            int lastVisible = Mathf.CeilToInt((scrollOffset + viewHeight) / itemHeight);
            lastVisible = Mathf.Min(lastVisible, itemNames.Length - 1);

            for (int i = firstVisible; i <= lastVisible; i++)
            {
                float yPos = listRect.y + i * itemHeight - scrollOffset;
                Rect btnRect = new Rect(listRect.x, yPos, listRect.width - 12, itemHeight - 2);
                if (btnRect.yMax > windowRect.y + 35 && btnRect.y < windowRect.yMax - 15)
                {
                    if (GUI.Button(btnRect, itemNames[i]))
                    {
                        SpawnItem(itemNames[i]);
                    }
                }
            }
        }

        private void SpawnItem(string itemName)
        {
            // ищейка предметов
            GameObject prefab = null;
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.name.Equals(itemName, System.StringComparison.OrdinalIgnoreCase))
                {
                    prefab = go;
                    break;
                }
            }

            // 2
            if (prefab == null)
            {
                MelonLogger.Error($"prefab '{itemName}' not found in scene, not in resourses");
                return;
            }

            // 3
            GameObject newItem = Object.Instantiate(prefab);
            if (newItem == null)
            {
                MelonLogger.Error($"failed to create '{itemName}'.");
                return;
            }

            // создание
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) player = GameObject.Find("Player");

            if (player != null && Camera.main != null)
            {
                Vector3 spawnPos = player.transform.position
                                   + Camera.main.transform.forward * 2.5f
                                   + Vector3.up * 1.2f;
                newItem.transform.position = spawnPos;
                MelonLogger.Msg($"item '{itemName}' spawned");
            }
            else
            {
                // создание в центре карты
                newItem.transform.position = new Vector3(0f, 1f, 0f);
                MelonLogger.Warning($"player not found '{itemName}' spawned in center of map.");
            }
        }
    }
}
