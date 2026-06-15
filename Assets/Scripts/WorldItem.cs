using UnityEngine;
using UnityEngine.InputSystem;

namespace WaywardSon
{
    /// <summary>
    /// Coloque este componente num objeto 3D na cena para torná-lo coletável.
    /// Requer: Collider (trigger) no mesmo objeto ou filho.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WorldItem : MonoBehaviour
    {
        [Header("Definição")]
        public ItemDefinition definition;

        [Header("Visual")]
        public float hoverAmplitude = 0.12f;
        public float hoverSpeed    = 1.8f;
        public float rotateSpeed   = 60f;

        [Header("UI Prompt")]
        public float pickupRange = 2.5f;

        // Internals
        private Vector3 _startPos;
        private bool    _playerNearby = false;
        private Inventory _inventory;
        private GUIStyle  _promptStyle;
        private bool      _stylesReady = false;

        private void Start()
        {
            _startPos = transform.position;
            GetComponent<Collider>().isTrigger = true;

            // Try to find inventory on scene load
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _inventory = player.GetComponent<Inventory>();
        }

        private void Update()
        {
            // Hover animation
            float newY = _startPos.y + Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

            if (!_playerNearby) return;

            // Re-check in case inventory wasn't found yet
            if (_inventory == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) _inventory = player.GetComponent<Inventory>();
            }

            // Input: E key or Gamepad South button
            bool pickupInput = false;
            if (Keyboard.current != null) pickupInput = Keyboard.current.eKey.wasPressedThisFrame;
            if (!pickupInput && Gamepad.current != null)
                pickupInput = Gamepad.current.buttonSouth.wasPressedThisFrame;

            if (pickupInput && _inventory != null)
            {
                TryPickup();
            }
        }

        private void TryPickup()
        {
            if (definition == null)
            {
                Debug.LogWarning($"[WorldItem] {name} tem ItemDefinition nulo!");
                return;
            }

            bool added = _inventory.AddItem(definition);
            if (added)
            {
                Debug.Log($"[WorldItem] Coletado: {definition.itemName}");
                Destroy(gameObject);
            }
            else
            {
                Debug.Log($"[WorldItem] Inventário cheio! Não foi possível coletar {definition.itemName}");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _playerNearby = true;
                if (_inventory == null)
                    _inventory = other.GetComponent<Inventory>();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
                _playerNearby = false;
        }

        private void InitStyles()
        {
            if (_stylesReady) return;

            _promptStyle = new GUIStyle(GUI.skin.box);
            _promptStyle.fontSize  = 14;
            _promptStyle.fontStyle = FontStyle.Bold;
            _promptStyle.alignment = TextAnchor.MiddleCenter;
            _promptStyle.normal.textColor    = Color.white;
            _promptStyle.normal.background   = MakeTex(new Color(0f, 0f, 0f, 0.72f));
            _promptStyle.padding = new RectOffset(10, 10, 6, 6);

            _stylesReady = true;
        }

        private Texture2D MakeTex(Color col)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, col);
            t.Apply();
            return t;
        }

        private void OnGUI()
        {
            if (!_playerNearby || definition == null) return;
            InitStyles();

            string label = $"[E]  Coletar  {definition.itemName}";
            Vector2 size = _promptStyle.CalcSize(new GUIContent(label));
            size.x += 20;

            // Project world position to screen
            Vector3 screenPos = Camera.main != null
                ? Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 0.6f)
                : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 1f);

            if (screenPos.z < 0) return; // behind camera

            float guiY = Screen.height - screenPos.y; // flip Y for GUI
            Rect rect = new Rect(screenPos.x - size.x * 0.5f, guiY - size.y - 10f, size.x, size.y);

            GUI.Box(rect, label, _promptStyle);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, pickupRange);
        }
#endif
    }
}
