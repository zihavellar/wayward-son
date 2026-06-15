using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WaywardSon
{
    public class Inventory : MonoBehaviour
    {
        [System.Serializable]
        public class InventoryItem
        {
            public string itemName;
            public Vector2Int size;
            public Vector2Int position;
            public Color color;
            public string texturePath; // Path relative to Resources/ folder (without extension)

            [System.NonSerialized]
            public Texture2D icon;

            public InventoryItem(string name, int w, int h, int x, int y, Color col, string texPath = "")
            {
                itemName = name;
                size = new Vector2Int(w, h);
                position = new Vector2Int(x, y);
                color = col;
                texturePath = texPath;
            }

            public void LoadIcon()
            {
                if (!string.IsNullOrEmpty(texturePath))
                {
                    icon = Resources.Load<Texture2D>(texturePath);
                    if (icon == null)
                        Debug.LogWarning($"[Inventory] Texture not found at Resources/{texturePath}");
                }
            }
        }

        [Header("Grid Size")]
        public int gridWidth = 6;
        public int gridHeight = 5;

        [Header("Inventory State")]
        public bool showInventory = false;
        public List<InventoryItem> items = new List<InventoryItem>();

        private bool[,] grid;

        // GUI styles cached
        private GUIStyle panelStyle;
        private GUIStyle labelStyle;
        private GUIStyle cellStyle;
        private GUIStyle itemNameStyle;
        private bool stylesInitialized = false;

        private void Start()
        {
            grid = new bool[gridWidth, gridHeight];

            // Add starting items with their textures
            AddItem("9mm Glock",     2, 1, new Color(0.15f, 0.15f, 0.2f),  "Images/glock19");
            AddItem("Handgun Ammo",  1, 1, new Color(0.2f, 0.15f, 0.1f),   "Images/handgunammo");
            AddItem("Med Kit",       2, 2, new Color(0.1f, 0.2f, 0.1f),    "Images/Medkit");
            AddItem("Shotgun",       3, 1, new Color(0.15f, 0.1f, 0.05f),  "Images/shotgun");
            AddItem("Shotgun Shell", 1, 1, new Color(0.2f, 0.1f, 0.05f),   "Images/Shotgunshell");
        }

        private void Update()
        {
            // Toggle inventory view with 'I' or Gamepad Triangle/Y
            bool toggleInput = false;
            if (Keyboard.current != null)
                toggleInput = Keyboard.current.iKey.wasPressedThisFrame;
            if (!toggleInput && Gamepad.current != null)
                toggleInput = Gamepad.current.buttonNorth.wasPressedThisFrame;

            if (toggleInput)
            {
                showInventory = !showInventory;
                Debug.Log($"Inventory Toggled: {showInventory}");
            }
        }

        public bool AddItem(string name, int w, int h, Color color, string texturePath = "")
        {
            Vector2Int placementPos;
            if (FindFreeSpace(w, h, out placementPos))
            {
                var newItem = new InventoryItem(name, w, h, placementPos.x, placementPos.y, color, texturePath);
                newItem.LoadIcon();
                items.Add(newItem);
                MarkGrid(placementPos.x, placementPos.y, w, h, true);
                Debug.Log($"Added {name} ({w}x{h}) to inventory at {placementPos}");
                return true;
            }
            Debug.LogWarning($"No space in inventory for {name} ({w}x{h})!");
            return false;
        }

        private bool FindFreeSpace(int w, int h, out Vector2Int position)
        {
            UpdateGridState();
            for (int y = 0; y <= gridHeight - h; y++)
            {
                for (int x = 0; x <= gridWidth - w; x++)
                {
                    if (IsSpaceFree(x, y, w, h))
                    {
                        position = new Vector2Int(x, y);
                        return true;
                    }
                }
            }
            position = Vector2Int.zero;
            return false;
        }

        private bool IsSpaceFree(int startX, int startY, int w, int h)
        {
            for (int y = startY; y < startY + h; y++)
                for (int x = startX; x < startX + w; x++)
                    if (grid[x, y]) return false;
            return true;
        }

        private void MarkGrid(int startX, int startY, int w, int h, bool state)
        {
            for (int y = startY; y < startY + h; y++)
                for (int x = startX; x < startX + w; x++)
                    grid[x, y] = state;
        }

        private void UpdateGridState()
        {
            for (int x = 0; x < gridWidth; x++)
                for (int y = 0; y < gridHeight; y++)
                    grid[x, y] = false;
            foreach (var item in items)
                MarkGrid(item.position.x, item.position.y, item.size.x, item.size.y, true);
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = MakeTex(1, 1, new Color(0.08f, 0.08f, 0.1f, 0.95f));
            panelStyle.border = new RectOffset(6, 6, 6, 6);

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.UpperCenter;
            labelStyle.fontSize = 14;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

            cellStyle = new GUIStyle(GUI.skin.box);
            cellStyle.normal.background = MakeTex(1, 1, new Color(0.18f, 0.18f, 0.22f, 1f));
            cellStyle.border = new RectOffset(2, 2, 2, 2);

            itemNameStyle = new GUIStyle(GUI.skin.label);
            itemNameStyle.alignment = TextAnchor.LowerCenter;
            itemNameStyle.fontSize = 9;
            itemNameStyle.fontStyle = FontStyle.Bold;
            itemNameStyle.normal.textColor = Color.white;
            itemNameStyle.wordWrap = true;

            stylesInitialized = true;
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            var tex = new Texture2D(width, height);
            tex.SetPixel(0, 0, col);
            tex.Apply();
            return tex;
        }

        private void OnGUI()
        {
            if (!showInventory) return;

            InitStyles();
            UpdateGridState();

            int cellSize   = 56;
            int padding    = 16;
            int headerH    = 40;
            int footerH    = 30;
            int panelW     = gridWidth  * cellSize + padding * 2;
            int panelH     = gridHeight * cellSize + headerH + footerH + padding;
            int startX     = Screen.width  - panelW - 20;
            int startY     = (Screen.height - panelH) / 2;

            // === Panel Background ===
            GUI.Box(new Rect(startX, startY, panelW, panelH), "", panelStyle);

            // === Title ===
            GUI.Label(new Rect(startX, startY + 8, panelW, 26), "I N V E N T Á R I O", labelStyle);

            int gridStartX = startX + padding;
            int gridStartY = startY + headerH;

            // === Draw Empty Cells ===
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Rect cellRect = new Rect(
                        gridStartX + x * cellSize + 1,
                        gridStartY + y * cellSize + 1,
                        cellSize - 2,
                        cellSize - 2
                    );
                    GUI.Box(cellRect, "", cellStyle);
                }
            }

            // === Draw Items ===
            foreach (var item in items)
            {
                int ix = gridStartX + item.position.x * cellSize + 2;
                int iy = gridStartY + item.position.y * cellSize + 2;
                int iw = item.size.x * cellSize - 4;
                int ih = item.size.y * cellSize - 4;

                Rect itemRect = new Rect(ix, iy, iw, ih);

                // Colored background
                var prevColor = GUI.color;
                GUI.color = item.color;
                GUI.Box(itemRect, "", cellStyle);
                GUI.color = prevColor;

                // Icon/texture
                if (item.icon != null)
                {
                    int iconPad = 4;
                    int nameH = 14;
                    Rect iconRect = new Rect(
                        ix + iconPad,
                        iy + iconPad,
                        iw - iconPad * 2,
                        ih - iconPad * 2 - nameH
                    );
                    GUI.DrawTexture(iconRect, item.icon, ScaleMode.ScaleToFit, true);
                }

                // Item name below icon
                Rect nameRect = new Rect(ix, iy + ih - 16, iw, 16);
                GUI.Label(nameRect, item.itemName, itemNameStyle);
            }

            // === Footer hint ===
            GUIStyle hintStyle = new GUIStyle(GUI.skin.label);
            hintStyle.alignment  = TextAnchor.MiddleCenter;
            hintStyle.fontSize   = 10;
            hintStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
            GUI.Label(
                new Rect(startX, startY + panelH - footerH, panelW, footerH),
                "[ I ] / [ Y ] — fechar inventário",
                hintStyle
            );
        }
    }
}
