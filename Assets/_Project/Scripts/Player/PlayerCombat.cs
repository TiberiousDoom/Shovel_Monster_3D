using UnityEngine;
using VoxelRPG.Combat;
using VoxelRPG.Core;
using VoxelRPG.Core.Items;

namespace VoxelRPG.Player
{
    /// <summary>
    /// Handles player combat input and weapon management.
    /// Integrates with PlayerInventory to use equipped weapons.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInventory _inventory;
        [SerializeField] private Transform _weaponHoldPoint;
        [SerializeField] private Camera _playerCamera;

        [Header("Unarmed Combat")]
        [SerializeField] private float _unarmedDamage = 5f;
        [SerializeField] private float _unarmedCooldown = 0.5f;
        [SerializeField] private float _unarmedRange = 1.5f;
        [SerializeField] private LayerMask _targetLayers = -1;

        [Header("Input")]
        [SerializeField] private KeyCode _attackKey = KeyCode.Mouse0;

        // Current weapon
        private MeleeWeapon _equippedWeapon;
        private GameObject _weaponInstance;

        // Unarmed state
        private float _unarmedCooldownTimer;

        private void Awake()
        {
            if (_inventory == null)
            {
                _inventory = GetComponent<PlayerInventory>();
            }

            if (_playerCamera == null)
            {
                _playerCamera = Camera.main;
            }
        }

        private void Start()
        {
            // Subscribe to hotbar changes
            if (_inventory != null)
            {
                _inventory.OnHotbarSelectionChanged += OnHotbarSelectionChanged;
                // Check initial selection
                UpdateEquippedWeapon();
            }
        }

        private void OnDestroy()
        {
            if (_inventory != null)
            {
                _inventory.OnHotbarSelectionChanged -= OnHotbarSelectionChanged;
            }
        }

        private void Update()
        {
            // Update unarmed cooldown
            if (_unarmedCooldownTimer > 0)
            {
                _unarmedCooldownTimer -= Time.deltaTime;
            }

            // Check for attack input
            if (Input.GetKeyDown(_attackKey))
            {
                TryAttack();
            }
        }

        private void OnHotbarSelectionChanged(int slotIndex)
        {
            UpdateEquippedWeapon();
        }

        private void UpdateEquippedWeapon()
        {
            // Clear existing weapon
            if (_weaponInstance != null)
            {
                Destroy(_weaponInstance);
                _weaponInstance = null;
                _equippedWeapon = null;
            }

            if (_inventory == null) return;

            // Get selected item
            var selectedItem = _inventory.SelectedItem;
            if (selectedItem.IsEmpty) return;

            var itemDef = selectedItem.Item;
            if (itemDef == null || !itemDef.IsEquippable) return;
            if (itemDef.Category != ItemCategory.Weapon && itemDef.Category != ItemCategory.Tool) return;

            // Spawn weapon prefab
            // Note: For now we check for a "EquippedPrefab" field or use DropPrefab as fallback
            // In a full implementation, ItemDefinition would have an equipped prefab field
            GameObject prefab = itemDef.DropPrefab;
            if (prefab == null) return;

            // Check if prefab has MeleeWeapon component
            if (prefab.GetComponent<MeleeWeapon>() == null)
            {
                // This item doesn't have weapon behavior, skip
                return;
            }

            // Spawn and parent to hold point
            _weaponInstance = Instantiate(prefab, _weaponHoldPoint);
            _weaponInstance.transform.localPosition = Vector3.zero;
            _weaponInstance.transform.localRotation = Quaternion.identity;

            // Get weapon component
            _equippedWeapon = _weaponInstance.GetComponent<MeleeWeapon>();
            if (_equippedWeapon != null)
            {
                _equippedWeapon.SetOwner(gameObject);
                _equippedWeapon.ConfigureFromItem(itemDef);
            }

            // Disable physics on held weapon
            var rb = _weaponInstance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            var col = _weaponInstance.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }

            Debug.Log($"[PlayerCombat] Equipped weapon: {itemDef.DisplayName}");
        }

        private void TryAttack()
        {
            if (_equippedWeapon != null)
            {
                // Use weapon attack
                _equippedWeapon.TryAttack();
            }
            else
            {
                // Unarmed attack
                TryUnarmedAttack();
            }
        }

        private void TryUnarmedAttack()
        {
            if (_unarmedCooldownTimer > 0) return;

            _unarmedCooldownTimer = _unarmedCooldown;

            // Raycast from camera
            Ray ray = _playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));

            if (Physics.Raycast(ray, out RaycastHit hit, _unarmedRange, _targetLayers))
            {
                // Check for damageable
                var damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable == null)
                {
                    damageable = hit.collider.GetComponentInParent<IDamageable>();
                }

                if (damageable != null && damageable.IsAlive)
                {
                    damageable.TakeDamage(_unarmedDamage, gameObject);
                    Debug.Log($"[PlayerCombat] Unarmed hit for {_unarmedDamage} damage!");
                }
            }
        }

        /// <summary>
        /// Gets the currently equipped weapon, or null if unarmed.
        /// </summary>
        public MeleeWeapon EquippedWeapon => _equippedWeapon;

        /// <summary>
        /// Whether player has a weapon equipped.
        /// </summary>
        public bool HasWeaponEquipped => _equippedWeapon != null;
    }
}
