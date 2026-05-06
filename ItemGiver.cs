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
            // Найти префаб предмета через Resources
            Item targetPrefab = null;
            foreach (var it in Resources.FindObjectsOfTypeAll<Item>())
            {
                if (it.name.Equals(itemName, System.StringComparison.OrdinalIgnoreCase))
                {
                    targetPrefab = it;
                    break;
                }
            }
            if (targetPrefab == null)
            {
                MelonLogger.Error("Предмет не найден: " + itemName);
                return;
            }

            Item newItem = Object.Instantiate(targetPrefab);

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) player = GameObject.Find("Player");
            if (player == null)
            {
                MelonLogger.Error("Игрок не найден — предмет создан на сцене.");
                return;
            }

            // Попытаемся добавить в InventorySlotScript через рефлексию
            InventorySlotScript invSlot = player.GetComponent<InventorySlotScript>();
            if (invSlot != null)
            {
                System.Type t = invSlot.GetType();
                MethodInfo addMethod = t.GetMethod("AddItem", new System.Type[] { typeof(Item) });
                if (addMethod != null)
                {
                    addMethod.Invoke(invSlot, new object[] { newItem });
                    MelonLogger.Msg($"Предмет {itemName} добавлен в инвентарь.");
                }
                else
                {
                    MelonLogger.Warning("Метод AddItem не найден. Пробуем альтернативу.");
                    // Альтернатива: напрямую задаём _inventoryScript и кладём в слот
                    ItemScript its = newItem.GetComponent<ItemScript>();
                    if (its != null)
                    {
                        its._inventoryScript = player.GetComponent<MonoBehaviour>(); // Cast if needed
                        MelonLogger.Msg($"Предмет {itemName} помещён в инвентарь через _inventoryScript.");
                    }
                }
            }
            else
            {
                MelonLogger.Warning("InventorySlotScript не обнаружен. Предмет остался в мире.");
            }
        }
    }
}
