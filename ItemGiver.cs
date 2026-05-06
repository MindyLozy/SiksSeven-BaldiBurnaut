using MelonLoader;
using UnityEngine;
using Il2Cpp;

namespace SiksSevenMenu
{
    public class ItemGiverWindow
    {
        public bool Visible = false;

        // перетаскиваемое окно
        private Rect windowRect = new Rect(360f, 20f, 220f, 350f);
        private bool isDragging = false;
        private Vector2 dragOffset;

        private readonly string[] itemNames = new string[]
        {
            "Bsoda", "Apple", "Banana", "Quarter",
            "GrapplingHook", "Dirty Chalk Eraser", "Present",
            "SafetyScissors", "SnowBall", "Swinging Door Lock",
            "Tape", "ZestyBar"
        };

        private Vector2 scrollPos;

        public void OnGUI()
        {
            if (!Visible) return;

            // обрабатываем перетаскивание за заголовок
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
            {
                isDragging = false;
            }

            // фон окна
            GUI.Box(windowRect, "Item Giver");

            // область с прокруткой
            GUILayout.BeginArea(new Rect(windowRect.x + 10, windowRect.y + 25, windowRect.width - 20, windowRect.height - 35));
            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            foreach (string name in itemNames)
            {
                if (GUILayout.Button(name, GUILayout.Height(25)))
                {
                    GiveItem(name);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void GiveItem(string itemName)
        {
            // ищем префаб предмета по имени
            Item[] allItems = Resources.FindObjectsOfTypeAll<Item>();
            Item targetPrefab = null;
            foreach (var it in allItems)
            {
                if (it.name.Equals(itemName, System.StringComparison.OrdinalIgnoreCase))
                {
                    targetPrefab = it;
                    break;
                }
            }

            if (targetPrefab == null)
            {
                MelonLogger.Error("not found item: " + itemName);
                return;
            }

            Item newItem = GameObject.Instantiate(targetPrefab);

            // пытаемся положить в инвентарь игрока
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) player = GameObject.Find("Player");
            if (player == null)
            {
                MelonLogger.Error("Player not found");
                return;
            }

            InventorySlotScript invSlot = player.GetComponent<InventorySlotScript>();
            if (invSlot != null)
            {
                // вызываем любой существующий метод добавления
                invSlot.AddItem(newItem);
                MelonLogger.Msg($"added item: {itemName}");
            }
            else
            {
                // если InventorySlotScript не найден, предмет остаётся в мире
                MelonLogger.Warning("Inventory not found");
            }
        }
    }
}
