using UnityEngine;

namespace WaywardSon
{
    /// <summary>
    /// ScriptableObject que define um tipo de item no jogo.
    /// Crie via Assets > Create > Wayward Son > Item Definition.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "Wayward Son/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        public enum ItemType { Weapon, Ammo, Consumable, Battery }

        [Header("Identificação")]
        public string itemName = "Item";
        [TextArea(2, 4)]
        public string description = "Descrição do item.";

        [Header("Grade")]
        public Vector2Int gridSize = new Vector2Int(1, 1);

        [Header("Visual")]
        /// <summary>Caminho relativo à pasta Resources/ sem extensão. Ex: Images/glock19</summary>
        public string texturePath = "";
        public Color tintColor = new Color(0.15f, 0.15f, 0.2f, 1f);

        [Header("Tipo e Efeito")]
        public ItemType itemType = ItemType.Consumable;

        // Weapon
        [Tooltip("Referência ao WeaponData — preencha apenas se itemType = Weapon")]
        public WeaponData weaponData;

        // Ammo
        [Tooltip("Quantidade de munição que este item fornece")]
        public int ammoAmount = 15;

        // Consumable
        [Tooltip("Quantidade de HP restaurado — preencha apenas se itemType = Consumable")]
        public int healAmount = 40;

        // Battery
        [Tooltip("Quantidade de bateria que este item fornece — preencha apenas se itemType = Battery")]
        public float batteryAmount = 30f;
    }
}
