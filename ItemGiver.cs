using MelonLoader;
using UnityEngine;
using Il2Cpp;
using System.Reflection;

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
            "GrapplingHook", "Dirty Chalk Eraser", "Present",
            "SafetyScissors", "SnowBall", "Swinging Door Lock",
            "Tape", "ZestyBar"
        };

        public void OnGUI()
        {
            if (!Visible) return;

            // --- Перетаскивание ---
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

            // --- Окно ---
            GUI.Box(windowRect, "Item Giver");

            // --- Прямоугольник списка (клиентская область) ---
            Rect listRect = new Rect(windowRect.x + 10, windowRect.y + 35, windowRect.width - 20, windowRect.height - 50);
            float contentHeight = itemNames.Length * itemHeight;
            float viewHeight = listRect.height;

            // Прокрутка колёсиком, только если мышь над окном
            if (listRect.Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
            {
                scrollOffset += e.delta.y * 20f;
                e.Use();
            }
            scrollOffset = Mathf.Clamp(scrollOffset, 0f, Mathf.Max(0, contentHeight - viewHeight));

            // Отрисовка видимых кнопок в абсолютных координатах
            int firstVisible = Mathf.FloorToInt(scrollOffset / itemHeight);
            int lastVisible = Mathf.CeilToInt((scrollOffset + viewHeight) / itemHeight);
            lastVisible = Mathf.Min(lastVisible, itemNames.Length - 1);

            for (int i = firstVisible; i <= lastVisible; i++)
            {
                float yPos = listRect.y + i * itemHeight - scrollOffset;
                Rect btnRect = new Rect(listRect.x, yPos, listRect.width - 12, itemHeight - 2);
                if (btnRect.yMax > windowRect.y + 35 && btnRect.y < windowRect.yMax - 15) // дополнительная проверка видимости
                {
                    if (GUI.Button(btnRect, itemNames[i]))
                    {
                        GiveItem(itemNames[i]);
                    }
                }
            }
        }

        private void GiveItem(string itemName)
        {
            // Найти префаб как GameObject
            GameObject prefab = null;
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.name.Equals(itemName, System.StringComparison.OrdinalIgnoreCase))
                {
                    prefab = go;
                    break;
                }
            }
            if (prefab == null)
            {
                MelonLogger.Error("Предмет не найден: " + itemName);
                return;
            }

            GameObject newObj = Object.Instantiate(prefab);
            ItemScript itemScr = newObj.GetComponent<ItemScript>();
            if (itemScr == null)
            {
                MelonLogger.Error("ItemScript не найден на: " + itemName);
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) player = GameObject.Find("Player");
            if (player == null)
            {
                MelonLogger.Error("Игрок не найден, предмет создан в мире.");
                return;
            }

            InventorySlotScript invSlot = player.GetComponent<InventorySlotScript>();
            if (invSlot != null)
            {
                itemScr._inventoryScript = invSlot;

                var addMethod = invSlot.GetType().GetMethod("AddItem", new System.Type[] { typeof(ItemScript) });
                if (addMethod != null)
                    addMethod.Invoke(invSlot, new object[] { itemScr });
                else
                    MelonLogger.Warning("AddItem не найден, но предмет привязан к инвентарю.");

                MelonLogger.Msg($"Предмет {itemName} выдан.");
            }
            else
            {
                MelonLogger.Warning("InventorySlotScript не найден — предмет лежит в мире.");
            }
        }
    }
}
