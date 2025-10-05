using UnityEngine;

/// <summary>
/// Пример создания сундука новичка
/// Этот файл показывает, как настроить сундук в Unity Editor
/// </summary>
public class ChestExampleSetup : MonoBehaviour
{
    [Header("Пример настройки сундука новичка")]
    [TextArea(5, 10)]
    public string setupInstructions = @"
ИНСТРУКЦИЯ ПО НАСТРОЙКЕ СУНДУКА НОВИЧКА:

1. Создайте новый Item в папке Items:
   - Right-click в Project → Create → Items → Item
   - Назовите его 'Chest_Starter'

2. Настройте Item:
   - Item Type: Chest
   - Item Name: 'Сундук новичка'
   - Can Use: ✓ true
   - Can Drop: ✓ true
   - Can Sell: ✓ true
   - Max Stack: 1

3. Создайте ChestItemData:
   - Right-click в Project → Create → Items → Chest Item
   - Назовите его 'StarterChestData'

4. Настройте ChestItemData:
   - Chest Name: 'Сундук новичка'
   - Description: 'Сундук с полезными предметами для начинающих'
   - Gold Reward: 100
   - Gold Chance: 1.0

5. Добавьте награды в ChestItemData:
   - Item ID 5 (Blade of Enigma) - Quantity: 1, Drop Chance: 1.0, Is Guaranteed: ✓
   - Item ID 6 (другой предмет) - Quantity: 1, Drop Chance: 0.8, Is Guaranteed: ✗
   - Добавьте больше предметов по желанию

6. Привяжите ChestItemData к Item:
   - В Item 'Chest_Starter' перетащите 'StarterChestData' в поле Chest Data

7. Добавьте предмет в ItemDatabase:
   - Откройте ItemDatabase
   - Добавьте новый предмет с ID (например, 100)
   - Перетащите Item 'Chest_Starter' в соответствующую ячейку

8. Тестирование:
   - Добавьте предмет в инвентарь через консоль или код
   - Двойной клик на предмет должен открыть сундук и выдать награды
   - Сундук должен исчезнуть из инвентаря

ПРИМЕР КОДА ДЛЯ ДОБАВЛЕНИЯ В ИНВЕНТАРЬ:
PlayerCore player = FindObjectOfType<PlayerCore>();
Item chestItem = ItemDatabase.Instance.GetItem(100); // ID сундука
player.Inventory.AddItem(chestItem, 1);
";
}
