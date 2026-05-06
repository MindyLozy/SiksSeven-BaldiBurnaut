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
        private Vector2 scrollPos;

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

            // Перетаскивание за заголовок
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

            GUILayout.BeginArea(new Rect(windowRect.x + 10, windowRect.y + 25, windowRect.width - 20, windowRect.height - 35));
            scrollPos = GUILayout.BeginScrollView(scrollPos);

            foreach (string name in itemNames)
            {
                if (GUILayout.Button(name))
                {
                    GiveItem(name);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void GiveItem(string itemName)
        {
            // Ищем префаб как GameObject (в Балди предметы — префабы с компонентом Item)
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

            // Создаём экземпляр
            GameObject newObj = Object.Instantiate(prefab);
            ItemScript itemScr = newObj.GetComponent<ItemScript>();
            if (itemScr == null)
            {
                MelonLogger.Error("ItemScript не найден на предмете: " + itemName);
                return;
            }

            // Ищем инвентарь игрока
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) player = GameObject.Find("Player");
            if (player == null)
            {
                MelonLogger.Error("Игрок не найден — предмет создан в мире.");
                return;
            }

            InventorySlotScript invSlot = player.GetComponent<InventorySlotScript>();
            if (invSlot != null)
            {
                // Присваиваем инвентарный скрипт предмету
                itemScr._inventoryScript = invSlot;

                // Пытаемся добавить через метод AddItem (рефлексия)
                var addMethod = invSlot.GetType().GetMethod("AddItem", new System.Type[] { typeof(ItemScript) });
                if (addMethod != null)
                    addMethod.Invoke(invSlot, new object[] { itemScr });
                else
                    MelonLogger.Warning("AddItem не найден, предмет привязан к инвентарю, но не добавлен. Возможно, нужен ручной вызов.");
                
                MelonLogger.Msg($"Предмет {itemName} выдан.");
            }
            else
            {
                MelonLogger.Warning("InventorySlotScript не найден — предмет лежит в мире.");
            }
        }
    }
}
