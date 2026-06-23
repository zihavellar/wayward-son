using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using WaywardSon.SaveSystem;

namespace WaywardSon
{
    /// <summary>
    /// Inventário em grade 6x5.
    /// Mouse  : arrastar para mover | duplo-clique para usar | hover para tooltip.
    /// Controle: D-pad/analógico navega | A/Cruz confirma/move | X/Quadrado usa |
    ///           B/Círculo cancela | Y/Triângulo abre-fecha.
    /// </summary>
    public class Inventory : MonoBehaviour, ISaveable
    {
        // ─── Runtime item slot ────────────────────────────────────────────────
        public class InventoryItem
        {
            public ItemDefinition definition;
            public Vector2Int     position;
            public Texture2D      icon;

            public string    itemName  => definition != null ? definition.itemName  : "???";
            public Vector2Int size     => definition != null ? definition.gridSize  : Vector2Int.one;
            public Color      tintColor => definition != null ? definition.tintColor : Color.gray;

            public InventoryItem(ItemDefinition def, Vector2Int pos)
            {
                definition = def;
                position   = pos;
                if (def != null && !string.IsNullOrEmpty(def.texturePath))
                    icon = Resources.Load<Texture2D>(def.texturePath);
            }
        }

        // ─── Settings ─────────────────────────────────────────────────────────
        [Header("Grade")]
        public int gridWidth  = 6;
        public int gridHeight = 5;

        [Header("Itens Iniciais (apenas para teste)")]
        public List<ItemDefinition> startingItems = new List<ItemDefinition>();

        [Header("Controle — repetição do cursor")]
        [Tooltip("Segundos antes de começar a repetir ao segurar o direcional")]
        public float padRepeatDelay    = 0.4f;
        [Tooltip("Intervalo entre repetições enquanto segura")]
        public float padRepeatInterval = 0.12f;

        // ─── State ────────────────────────────────────────────────────────────
        public bool showInventory = false;
        public List<InventoryItem> items = new List<InventoryItem>();

        private bool[,] grid;

        // ── Mouse drag state ──
        private InventoryItem _dragging    = null;
        private Vector2Int    _dragOrigPos;
        private Vector2       _dragOffset;

        // ── Mouse double-click ──
        private float         _lastClickTime = -1f;
        private InventoryItem _lastClickItem = null;
        private const float   DoubleClickThreshold = 0.35f;

        // ── Gamepad cursor ──
        private Vector2Int    _cursor        = Vector2Int.zero;   // cell the gamepad is on
        private InventoryItem _padSelected   = null;              // item "grabbed" by gamepad
        private Vector2Int    _padGrabOrigPos;                    // original pos before gamepad move

        // D-pad repeat state
        private Vector2       _padMoveDir    = Vector2.zero;
        private float         _padNextMove   = 0f;
        private bool          _padHeld       = false;

        // ── Tooltip ──
        private InventoryItem _hoveredItem   = null;

        // ── Equipped item marker ──
        private InventoryItem _equippedItem  = null;

        // ── References ──
        private WeaponHandler _weaponHandler;
        private PlayerHealth  _playerHealth;
        private FlashlightController _flashlight;
        private bool          _usingGamepad  = false;

        // ─── GUI layout ───────────────────────────────────────────────────────
        private int _cellSize   = 56;
        private int _padding    = 16;
        private int _headerH    = 44;
        private int _footerH    = 36;   // taller to fit two hint lines
        private int _panelW, _panelH, _startX, _startY;
        private int _gridStartX, _gridStartY;

        // ─── Cached styles ────────────────────────────────────────────────────
        private GUIStyle _panelStyle, _titleStyle, _cellStyle, _itemStyle,
                         _labelStyle, _hintStyle, _tooltipStyle, _tooltipTitleStyle,
                         _equippedBadgeStyle;
        private bool _stylesReady = false;

        // ─── ISaveable ────────────────────────────────────────────────────────
        public string SaveID => "Inventory";

        public void CollectData(Dictionary<string, object> data)
        {
            data["showInventory"] = showInventory;
            data["gridWidth"] = gridWidth;
            data["gridHeight"] = gridHeight;

            var itemList = new List<Dictionary<string, object>>();
            foreach (var item in items)
            {
                var itemData = new Dictionary<string, object>
                {
                    ["itemName"] = item.itemName,
                    ["posX"] = item.position.x,
                    ["posY"] = item.position.y
                };
                itemList.Add(itemData);
            }
            data["items"] = itemList;

            data["equippedItemName"] = _equippedItem?.itemName ?? "";
        }

        public void ApplyData(Dictionary<string, object> data)
        {
            if (data.TryGetValue("showInventory", out var si))
                showInventory = (bool)si;

            if (data.TryGetValue("items", out var rawItems) && rawItems is List<object> itemList)
            {
                items.Clear();
                RebuildGrid();

                foreach (var raw in itemList)
                {
                    if (raw is Dictionary<string, object> itemData)
                    {
                        string itemName = itemData.TryGetValue("itemName", out var n) ? n.ToString() : "";
                        int posX = itemData.TryGetValue("posX", out var px) ? System.Convert.ToInt32(px) : 0;
                        int posY = itemData.TryGetValue("posY", out var py) ? System.Convert.ToInt32(py) : 0;

                        var definitions = Resources.FindObjectsOfTypeAll<ItemDefinition>();
                        ItemDefinition def = null;
                        foreach (var d in definitions)
                        {
                            if (d.itemName == itemName)
                            {
                                def = d;
                                break;
                            }
                        }

                        if (def != null)
                        {
                            var invItem = new InventoryItem(def, new Vector2Int(posX, posY));
                            items.Add(invItem);
                        }
                    }
                }
                RebuildGrid();
            }
        }

        // ─── Lifecycle ────────────────────────────────────────────────────────
        private void Awake()
        {
            grid = new bool[gridWidth, gridHeight];
        }

        private void Start()
        {
            _weaponHandler = GetComponent<WeaponHandler>();
            _playerHealth  = GetComponent<PlayerHealth>();
            _flashlight    = GetComponent<FlashlightController>();

            foreach (var def in startingItems)
                if (def != null) AddItem(def);

            if (items.Count == 0)
                SpawnTestItems();
        }

        private void Update()
        {
            // Detect input source
            if (Mouse.current != null &&
                (Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f ||
                 Mouse.current.leftButton.wasPressedThisFrame))
                _usingGamepad = false;

            if (Gamepad.current != null &&
                (Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.1f ||
                 Gamepad.current.dpad.ReadValue().sqrMagnitude > 0.1f ||
                 Gamepad.current.buttonSouth.wasPressedThisFrame))
                _usingGamepad = true;

            // Toggle inventory  Y / Triangle  or  I
            bool toggle = false;
            if (Keyboard.current != null) toggle = Keyboard.current.iKey.wasPressedThisFrame;
            if (!toggle && Gamepad.current != null) toggle = Gamepad.current.buttonNorth.wasPressedThisFrame;

            if (toggle)
            {
                showInventory = !showInventory;
                if (!showInventory) ResetGamepadState();
                if (showInventory) _cursor = Vector2Int.zero;
            }

            if (!showInventory) return;

            // Only process gamepad navigation in Update (not in OnGUI)
            if (Gamepad.current != null)
                HandleGamepadInput();
        }

        // ─── Gamepad input ────────────────────────────────────────────────────

        private void HandleGamepadInput()
        {
            var gp = Gamepad.current;

            // ── Cursor navigation (D-pad + left stick) ──
            Vector2 rawDir = gp.dpad.ReadValue();
            if (rawDir.sqrMagnitude < 0.1f)
            {
                rawDir = gp.leftStick.ReadValue();
                // Apply deadzone
                if (rawDir.sqrMagnitude < 0.4f) rawDir = Vector2.zero;
            }

            // Convert to cardinal direction
            Vector2Int dir = Vector2Int.zero;
            if (Mathf.Abs(rawDir.x) > Mathf.Abs(rawDir.y))
                dir = new Vector2Int(rawDir.x > 0 ? 1 : -1, 0);
            else if (Mathf.Abs(rawDir.y) > 0.1f)
                dir = new Vector2Int(0, rawDir.y > 0 ? -1 : 1); // Y inverted (up = row-1)

            if (dir != Vector2Int.zero)
            {
                bool newPress = (Vector2)dir != _padMoveDir;
                _padMoveDir = dir;

                if (!_padHeld || newPress)
                {
                    // First press → move immediately, then wait for delay
                    if (!_padHeld)
                    {
                        MoveCursor(dir);
                        _padNextMove = Time.unscaledTime + padRepeatDelay;
                        _padHeld = true;
                    }
                }
                else if (Time.unscaledTime >= _padNextMove)
                {
                    MoveCursor(dir);
                    _padNextMove = Time.unscaledTime + padRepeatInterval;
                }
            }
            else
            {
                _padHeld    = false;
                _padMoveDir = Vector2.zero;
            }

            // ── A / Cross  →  Select / Confirm drop ──
            if (gp.buttonSouth.wasPressedThisFrame)
                GamepadConfirm();

            // ── X / Square  →  Use item ──
            if (gp.buttonWest.wasPressedThisFrame)
                GamepadUse();

            // ── B / Circle  →  Cancel grab or close ──
            if (gp.buttonEast.wasPressedThisFrame)
            {
                if (_padSelected != null)
                    GamepadCancelGrab();
                else
                {
                    showInventory = false;
                    ResetGamepadState();
                }
            }
        }

        private void MoveCursor(Vector2Int dir)
        {
            _cursor.x = Mathf.Clamp(_cursor.x + dir.x, 0, gridWidth  - 1);
            _cursor.y = Mathf.Clamp(_cursor.y + dir.y, 0, gridHeight - 1);
        }

        /// <summary>A/Cross: grab item at cursor, or drop grabbed item at cursor.</summary>
        private void GamepadConfirm()
        {
            if (_padSelected == null)
            {
                // Try to grab item at cursor
                InventoryItem hit = ItemAt(_cursor.x, _cursor.y);
                if (hit != null)
                {
                    _padGrabOrigPos = hit.position;
                    _padSelected    = hit;
                    items.Remove(hit);
                    RebuildGrid();
                }
            }
            else
            {
                // Try to drop at cursor (top-left of item at cursor)
                if (CanFit(_cursor.x, _cursor.y, _padSelected.size.x, _padSelected.size.y, _padSelected))
                {
                    _padSelected.position = _cursor;
                    items.Add(_padSelected);
                    RebuildGrid();
                    _padSelected = null;
                }
                // else: invalid position, stay grabbed (visual feedback via red highlight)
            }
        }

        /// <summary>X/Square: use item at cursor.</summary>
        private void GamepadUse()
        {
            if (_padSelected != null)
            {
                // Use the grabbed item and return it to original position if consumable removes it
                UseItem(_padSelected);
                // If item was removed by UseItem (consumable), _padSelected is now orphaned
                if (!items.Contains(_padSelected))
                    _padSelected = null;
                else
                {
                    // Weapon stays; put it back
                    _padSelected.position = _padGrabOrigPos;
                    if (CanFit(_padGrabOrigPos.x, _padGrabOrigPos.y, _padSelected.size.x, _padSelected.size.y, _padSelected))
                        items.Add(_padSelected);
                    _padSelected = null;
                    RebuildGrid();
                }
                return;
            }

            InventoryItem target = ItemAt(_cursor.x, _cursor.y);
            if (target != null)
                UseItem(target);
        }

        private void GamepadCancelGrab()
        {
            if (_padSelected == null) return;
            _padSelected.position = _padGrabOrigPos;
            items.Add(_padSelected);
            _padSelected = null;
            RebuildGrid();
        }

        private void ResetGamepadState()
        {
            if (_padSelected != null) GamepadCancelGrab();
            if (_dragging    != null) CancelDrag();
        }

        // ─── Public API ───────────────────────────────────────────────────────

        public bool AddItem(ItemDefinition def)
        {
            if (def == null) return false;
            Vector2Int pos;
            if (!FindFreeSpace(def.gridSize.x, def.gridSize.y, out pos))
            {
                Debug.LogWarning($"[Inventory] Sem espaço para {def.itemName}!");
                return false;
            }
            var item = new InventoryItem(def, pos);
            items.Add(item);
            MarkGrid(pos.x, pos.y, def.gridSize.x, def.gridSize.y, true);
            Debug.Log($"[Inventory] +{def.itemName} na posição {pos}");
            return true;
        }

        // ─── Grid helpers ─────────────────────────────────────────────────────

        private void RebuildGrid()
        {
            for (int x = 0; x < gridWidth; x++)
                for (int y = 0; y < gridHeight; y++)
                    grid[x, y] = false;
            foreach (var it in items)
                MarkGrid(it.position.x, it.position.y, it.size.x, it.size.y, true);
        }

        private void MarkGrid(int sx, int sy, int w, int h, bool state)
        {
            for (int y = sy; y < sy + h; y++)
                for (int x = sx; x < sx + w; x++)
                    if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
                        grid[x, y] = state;
        }

        private bool FindFreeSpace(int w, int h, out Vector2Int position)
        {
            RebuildGrid();
            for (int y = 0; y <= gridHeight - h; y++)
                for (int x = 0; x <= gridWidth - w; x++)
                    if (CanFit(x, y, w, h, null))
                    { position = new Vector2Int(x, y); return true; }
            position = Vector2Int.zero;
            return false;
        }

        private bool CanFit(int sx, int sy, int w, int h, InventoryItem ignore)
        {
            for (int y = sy; y < sy + h; y++)
                for (int x = sx; x < sx + w; x++)
                {
                    if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return false;
                    if (!grid[x, y]) continue;
                    if (ItemAt(x, y) != ignore) return false;
                }
            return true;
        }

        private InventoryItem ItemAt(int gx, int gy)
        {
            foreach (var it in items)
                if (gx >= it.position.x && gx < it.position.x + it.size.x &&
                    gy >= it.position.y && gy < it.position.y + it.size.y)
                    return it;
            return null;
        }

        // ─── Item use ─────────────────────────────────────────────────────────

        private void UseItem(InventoryItem item)
        {
            if (item == null || item.definition == null) return;
            var def = item.definition;

            switch (def.itemType)
            {
                case ItemDefinition.ItemType.Weapon:
                    if (_weaponHandler != null)
                    {
                        _weaponHandler.EquipWeapon(def.weaponData);
                        _equippedItem = item;   // mark as equipped in inventory
                    }
                    else
                        Debug.LogWarning("[Inventory] WeaponHandler não encontrado!");
                    break;

                case ItemDefinition.ItemType.Ammo:
                    if (_weaponHandler != null)
                    {
                        _weaponHandler.AddAmmo(def.ammoAmount);
                        RemoveItem(item);
                    }
                    break;

                case ItemDefinition.ItemType.Consumable:
                    if (_playerHealth != null)
                    {
                        _playerHealth.Heal(def.healAmount);
                        RemoveItem(item);
                    }
                    else
                        Debug.LogWarning("[Inventory] PlayerHealth não encontrado!");
                    break;

                case ItemDefinition.ItemType.Battery:
                    if (_flashlight != null)
                    {
                        bool used = _flashlight.RechargeBattery(def.batteryAmount);
                        if (used) RemoveItem(item);
                    }
                    else
                        Debug.LogWarning("[Inventory] FlashlightController não encontrado!");
                    break;
            }
        }

        private void RemoveItem(InventoryItem item)
        {
            items.Remove(item);
            RebuildGrid();
        }

        // ─── Mouse drag helpers ───────────────────────────────────────────────

        private void CancelDrag()
        {
            if (_dragging == null) return;
            _dragging.position = _dragOrigPos;
            items.Add(_dragging);
            _dragging = null;
            RebuildGrid();
        }

        private bool ScreenToGrid(Vector2 pos, out Vector2Int cell)
        {
            int gx = Mathf.FloorToInt((pos.x - _gridStartX) / _cellSize);
            int gy = Mathf.FloorToInt((pos.y - _gridStartY) / _cellSize);
            cell = new Vector2Int(gx, gy);
            return gx >= 0 && gx < gridWidth && gy >= 0 && gy < gridHeight;
        }

        private Rect ItemRect(InventoryItem it)
        {
            return new Rect(
                _gridStartX + it.position.x * _cellSize + 2,
                _gridStartY + it.position.y * _cellSize + 2,
                it.size.x * _cellSize - 4,
                it.size.y * _cellSize - 4);
        }

        private Rect CellRect(int x, int y)
        {
            return new Rect(
                _gridStartX + x * _cellSize + 1,
                _gridStartY + y * _cellSize + 1,
                _cellSize - 2, _cellSize - 2);
        }

        // ─── GUI ──────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (!showInventory) return;

            InitStyles();
            ComputeLayout();
            RebuildGrid();

            Event  e     = Event.current;
            Vector2 mouse = e.mousePosition;

            // ── Panel ──
            GUI.Box(new Rect(_startX, _startY, _panelW, _panelH), GUIContent.none, _panelStyle);

            // ── Title ──
            GUI.Label(new Rect(_startX, _startY + 8, _panelW, 28), "I N V E N T Á R I O", _titleStyle);

            // ── Empty cells ──
            for (int x = 0; x < gridWidth; x++)
                for (int y = 0; y < gridHeight; y++)
                    GUI.Box(CellRect(x, y), GUIContent.none, _cellStyle);

            // ── Gamepad cursor highlight (background, before items) ──
            if (_usingGamepad)
                DrawGamepadCursorBackground();

            // ── Items ──
            _hoveredItem = null;
            foreach (var it in items)
            {
                Rect r       = ItemRect(it);
                bool hovered = !_usingGamepad && r.Contains(mouse) && _dragging == null;
                bool padSel  = _usingGamepad && _padSelected == null &&
                               _cursor.x >= it.position.x && _cursor.x < it.position.x + it.size.x &&
                               _cursor.y >= it.position.y && _cursor.y < it.position.y + it.size.y;

                if (hovered || padSel) _hoveredItem = it;

                DrawItem(it, r, hovered || padSel, false);

                // ─ Mouse events ─
                if (!_usingGamepad)
                {
                    if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(mouse))
                    {
                        if (_lastClickItem == it && (Time.realtimeSinceStartup - _lastClickTime) < DoubleClickThreshold)
                        {
                            UseItem(it);
                            _lastClickItem = null;
                            e.Use();
                            return;
                        }
                        else
                        {
                            _lastClickTime  = Time.realtimeSinceStartup;
                            _lastClickItem  = it;

                            _dragOrigPos = it.position;
                            _dragOffset  = new Vector2(mouse.x - r.x, mouse.y - r.y);
                            items.Remove(it);
                            RebuildGrid();
                            _dragging = it;
                            e.Use();
                            return;
                        }
                    }
                }
            }

            // ── Gamepad: grabbed item ghost following cursor ──
            if (_usingGamepad && _padSelected != null)
                DrawGamepadGhostAndHighlight();

            // ── Mouse drag ghost ──
            if (!_usingGamepad && _dragging != null)
                DrawMouseDragGhost(e, mouse);

            // ── Tooltip ──
            if (_hoveredItem != null)
            {
                Vector2 tipAnchor = _usingGamepad
                    ? new Vector2(
                        _gridStartX + (_cursor.x + 1) * _cellSize + 8,
                        _gridStartY + _cursor.y * _cellSize)
                    : mouse;
                DrawTooltip(_hoveredItem, tipAnchor);
            }

            // ── Footer ──
            string footerText = _usingGamepad
                ? "⬆⬇⬅➡ Mover  |  A Pegar/Soltar  |  X Usar  |  B Cancelar/Fechar  |  Y Fechar"
                : "[ I ] Fechar  |  Arrastar para mover  |  Duplo-clique para usar";
            GUI.Label(new Rect(_startX, _startY + _panelH - _footerH, _panelW, _footerH), footerText, _hintStyle);
        }

        // ─── Gamepad visual sub-methods ───────────────────────────────────────

        private void DrawGamepadCursorBackground()
        {
            // Determine highlight color
            Color hi;
            if (_padSelected != null)
            {
                bool fits = CanFit(_cursor.x, _cursor.y, _padSelected.size.x, _padSelected.size.y, _padSelected);
                hi = fits ? new Color(0f, 1f, 0.4f, 0.22f) : new Color(1f, 0.2f, 0.1f, 0.22f);
            }
            else
            {
                hi = new Color(1f, 1f, 1f, 0.12f);
            }

            var prev = GUI.color;
            GUI.color = hi;

            int w = _padSelected != null ? _padSelected.size.x : 1;
            int h = _padSelected != null ? _padSelected.size.y : 1;
            for (int dx = 0; dx < w; dx++)
                for (int dy = 0; dy < h; dy++)
                {
                    int cx = _cursor.x + dx, cy = _cursor.y + dy;
                    if (cx < gridWidth && cy < gridHeight)
                        GUI.Box(CellRect(cx, cy), GUIContent.none, _cellStyle);
                }

            GUI.color = prev;
        }

        private void DrawGamepadGhostAndHighlight()
        {
            // Ghost at cursor position
            Rect ghostRect = new Rect(
                _gridStartX + _cursor.x * _cellSize + 2,
                _gridStartY + _cursor.y * _cellSize + 2,
                _padSelected.size.x * _cellSize - 4,
                _padSelected.size.y * _cellSize - 4);

            DrawItem(_padSelected, ghostRect, true, true);
        }

        // ─── Mouse drag visual ────────────────────────────────────────────────

        private void DrawMouseDragGhost(Event e, Vector2 mouse)
        {
            Rect ghostRect = new Rect(
                mouse.x - _dragOffset.x,
                mouse.y - _dragOffset.y,
                _dragging.size.x * _cellSize - 4,
                _dragging.size.y * _cellSize - 4);

            // Highlight target cells
            Vector2Int targetCell;
            if (ScreenToGrid(new Vector2(ghostRect.x + 2, ghostRect.y + 2), out targetCell))
            {
                bool fits = CanFit(targetCell.x, targetCell.y, _dragging.size.x, _dragging.size.y, _dragging);
                Color hi  = fits ? new Color(0f, 1f, 0.4f, 0.25f) : new Color(1f, 0.2f, 0.2f, 0.25f);
                var prevC = GUI.color;

                for (int dx = 0; dx < _dragging.size.x; dx++)
                    for (int dy = 0; dy < _dragging.size.y; dy++)
                    {
                        int cx = targetCell.x + dx, cy = targetCell.y + dy;
                        if (cx < gridWidth && cy < gridHeight)
                        {
                            GUI.color = hi;
                            GUI.Box(CellRect(cx, cy), GUIContent.none, _cellStyle);
                        }
                    }
                GUI.color = prevC;
            }

            DrawItem(_dragging, ghostRect, false, true);

            // Drop
            if (e.type == EventType.MouseUp && e.button == 0)
            {
                Vector2Int dropCell;
                bool dropped = false;
                if (ScreenToGrid(new Vector2(ghostRect.x + 2, ghostRect.y + 2), out dropCell) &&
                    CanFit(dropCell.x, dropCell.y, _dragging.size.x, _dragging.size.y, _dragging))
                {
                    _dragging.position = dropCell;
                    items.Add(_dragging);
                    dropped = true;
                }
                if (!dropped) CancelDrag();
                else { _dragging = null; RebuildGrid(); }
                e.Use();
            }

            if (e.type == EventType.MouseDown && e.button == 1)
            {
                CancelDrag();
                e.Use();
            }
        }

        // ─── Draw helpers ─────────────────────────────────────────────────────

        private void DrawItem(InventoryItem it, Rect r, bool highlighted, bool isGhost)
        {
            var prev = GUI.color;

            GUI.color = highlighted
                ? Color.Lerp(it.tintColor, Color.white, 0.3f)
                : it.tintColor;
            GUI.Box(r, GUIContent.none, _itemStyle);

            if (isGhost)
            {
                GUI.color = new Color(1, 1, 1, 0.55f);
                GUI.Box(r, GUIContent.none, _itemStyle);
            }

            GUI.color = prev;

            if (it.icon != null)
            {
                int p = 5, nameH = 14;
                GUI.DrawTexture(
                    new Rect(r.x + p, r.y + p, r.width - p * 2, r.height - p * 2 - nameH),
                    it.icon, ScaleMode.ScaleToFit, true);
            }

            GUI.Label(new Rect(r.x, r.yMax - 16, r.width, 16), it.itemName, _labelStyle);

            // ── Equipped marker ──
            if (it == _equippedItem)
            {
                // Amber glow border (two overlapping boxes with alpha)
                GUI.color = new Color(1f, 0.78f, 0.1f, 0.85f);
                GUI.Box(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), GUIContent.none, _itemStyle);
                GUI.color = new Color(1f, 0.78f, 0.1f, 0.4f);
                GUI.Box(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), GUIContent.none, _itemStyle);
                GUI.color = prev;

                // Small "EQUIPADO" badge — top-left corner
                Rect badgeRect = new Rect(r.x + 2, r.y + 2, 52, 13);
                GUI.color = new Color(1f, 0.78f, 0.1f, 0.92f);
                GUI.Box(badgeRect, GUIContent.none, _itemStyle);
                GUI.color = prev;
                GUI.Label(badgeRect, "✔ EQUIPADO", _equippedBadgeStyle);
            }
        }

        private void DrawTooltip(InventoryItem it, Vector2 anchor)
        {
            if (it.definition == null) return;

            string typeTag = it.definition.itemType switch
            {
                ItemDefinition.ItemType.Weapon     => $"[ARMA]",
                ItemDefinition.ItemType.Ammo       => $"[MUNIÇÃO +{it.definition.ammoAmount}]",
                ItemDefinition.ItemType.Consumable => $"[CURA +{it.definition.healAmount} HP]",
                ItemDefinition.ItemType.Battery    => $"[BATERIA +{it.definition.batteryAmount}]",
                _                                  => ""
            };
            string body = $"{typeTag}\n{it.definition.description}";
            Vector2 sz  = _tooltipStyle.CalcSize(new GUIContent(body));
            sz.x = Mathf.Max(sz.x, 160f);
            sz.y += 26;

            float tx = Mathf.Clamp(anchor.x + 14, 0, Screen.width  - sz.x - 4);
            float ty = Mathf.Clamp(anchor.y - sz.y * 0.5f, 0, Screen.height - sz.y - 4);

            Rect box = new Rect(tx, ty, sz.x, sz.y);
            GUI.Box(box, GUIContent.none, _tooltipStyle);
            GUI.Label(new Rect(tx + 8, ty + 6, sz.x - 16, 20), it.itemName, _tooltipTitleStyle);
            GUI.Label(new Rect(tx + 8, ty + 26, sz.x - 16, sz.y - 30), body, _hintStyle);
        }

        // ─── Layout ───────────────────────────────────────────────────────────

        private void ComputeLayout()
        {
            _panelW     = gridWidth  * _cellSize + _padding * 2;
            _panelH     = gridHeight * _cellSize + _headerH + _footerH + _padding;
            _startX     = Screen.width  - _panelW - 24;
            _startY     = (Screen.height - _panelH) / 2;
            _gridStartX = _startX + _padding;
            _gridStartY = _startY + _headerH;
        }

        // ─── Styles ───────────────────────────────────────────────────────────

        private void InitStyles()
        {
            if (_stylesReady) return;

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTex(new Color(0.06f, 0.06f, 0.08f, 0.97f)) },
                border = new RectOffset(8, 8, 8, 8)
            };
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperCenter,
                fontSize  = 15,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(0.85f, 0.85f, 0.9f) }
            };
            _cellStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTex(new Color(0.15f, 0.15f, 0.2f, 1f)) },
                border = new RectOffset(2, 2, 2, 2)
            };
            _itemStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTex(Color.white) },
                border = new RectOffset(2, 2, 2, 2)
            };
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.LowerCenter,
                fontSize  = 9,
                fontStyle = FontStyle.Bold,
                wordWrap  = true,
                normal    = { textColor = Color.white }
            };
            _hintStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize  = 10,
                normal    = { textColor = new Color(0.5f, 0.5f, 0.55f) },
                wordWrap  = true
            };
            _tooltipStyle = new GUIStyle(GUI.skin.box)
            {
                normal  = { background = MakeTex(new Color(0.04f, 0.04f, 0.06f, 0.96f)) },
                padding = new RectOffset(8, 8, 6, 6),
                border  = new RectOffset(4, 4, 4, 4)
            };
            _tooltipTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 12,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(0.9f, 0.85f, 0.5f) }
            };
            _equippedBadgeStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize  = 7,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(0.1f, 0.07f, 0f) }
            };

            _stylesReady = true;
        }

        private Texture2D MakeTex(Color col)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, col);
            t.Apply();
            return t;
        }

        // ─── Test data fallback ───────────────────────────────────────────────

        private void SpawnTestItems()
        {
            AddRuntimeItem("9mm Glock",     "Pistola semi-automática compacta.", new Vector2Int(2, 1),
                           "Images/glock19",      new Color(0.13f, 0.13f, 0.18f), ItemDefinition.ItemType.Weapon);
            AddRuntimeItem("Handgun Ammo",  "Caixa de munição 9mm.",             new Vector2Int(1, 1),
                           "Images/handgunammo",  new Color(0.18f, 0.13f, 0.08f), ItemDefinition.ItemType.Ammo, 0, 15);
            AddRuntimeItem("Med Kit",       "Kit de primeiros socorros. Restaura HP.", new Vector2Int(2, 2),
                           "Images/Medkit",       new Color(0.08f, 0.18f, 0.08f), ItemDefinition.ItemType.Consumable, 40);
            AddRuntimeItem("Shotgun",       "Espingarda de dois canos.",         new Vector2Int(3, 1),
                           "Images/shotgun",      new Color(0.14f, 0.09f, 0.05f), ItemDefinition.ItemType.Weapon);
            AddRuntimeItem("Shotgun Shell", "Munição calibre 12.",               new Vector2Int(1, 1),
                           "Images/Shotgunshell", new Color(0.20f, 0.10f, 0.05f), ItemDefinition.ItemType.Ammo, 0, 8);
        }

        private void AddRuntimeItem(string name, string desc, Vector2Int size, string tex,
                                    Color tint, ItemDefinition.ItemType type, int heal = 0, int ammo = 0)
        {
            var def = ScriptableObject.CreateInstance<ItemDefinition>();
            def.itemName    = name;
            def.description = desc;
            def.gridSize    = size;
            def.texturePath = tex;
            def.tintColor   = tint;
            def.itemType    = type;
            def.healAmount  = heal;
            def.ammoAmount  = ammo;

            if (type == ItemDefinition.ItemType.Weapon)
            {
                if (name == "Glock 19")
                {
                    def.weaponData = Resources.Load<WeaponData>("Weapons/PistolData");
                }
                else if (name == "Shotgun")
                {
                    def.weaponData = Resources.Load<WeaponData>("Weapons/ShotgunData");
                }
            }

            AddItem(def);
        }
    }
}
