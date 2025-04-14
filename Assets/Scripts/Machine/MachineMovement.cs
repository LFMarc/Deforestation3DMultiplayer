using Deforestation.Dinosaurus;
using Deforestation.Recolectables;
using Deforestation.Network; // Necesario para acceder a NetworkMachine
using UnityEngine;
using Photon.Pun;
using System;

namespace Deforestation.Machine
{
    public class MachineMovement : MonoBehaviour
    {
        #region Fields
        [SerializeField] private float _speedForce = 50;
        [SerializeField] private float _speedRotation = 15;
        private Rigidbody _rb;
        private Vector3 _movementDirection;
        private Inventory _inventory => GameController.Instance.Inventory;

        [Header("Energy")]
        [SerializeField] private float energyDecayRate = 20f;
        private float energyTimer = 0f;

        private bool _wasMoving = false;
        public Action<bool> OnMovementChanged;

        private NetworkMachine _networkMachine;
        #endregion

        #region Unity Callbacks	
        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            _networkMachine = GetComponent<NetworkMachine>();
        }

        private void Update()
        {
            if (_inventory.HasResource(RecolectableType.HyperCrystal))
            {
                _movementDirection = new Vector3(Input.GetAxis("Vertical"), 0, 0);
                transform.Rotate(Vector3.up * _speedRotation * Time.deltaTime * Input.GetAxis("Horizontal"));

                bool isMovingNow = _movementDirection.magnitude > 0.01f || Mathf.Abs(Input.GetAxis("Horizontal")) > 0.01f;

                if (isMovingNow != _wasMoving)
                {
                    _wasMoving = isMovingNow;

                    // Actualiza localmente
                    GameController.Instance.MachineController.SetIsMoving(isMovingNow);

                    // Sincroniza con los demás
                    if (_networkMachine.photonView.IsMine)
                    {
                        _networkMachine.photonView.RPC("SyncIsMoving", RpcTarget.Others, isMovingNow);
                    }

                    OnMovementChanged?.Invoke(isMovingNow);
                }

                if (isMovingNow)
                {
                    energyTimer += Time.deltaTime;
                    if (energyTimer >= energyDecayRate)
                        _inventory.UseResource(RecolectableType.HyperCrystal);
                }
            }
            else
            {
                GameController.Instance.MachineController.StopMoving();
            }

            CheckGround();
        }

        private void FixedUpdate()
        {
            _rb.AddRelativeForce(_movementDirection.normalized * _speedForce, ForceMode.Impulse);
        }

        void CheckGround()
        {
            RaycastHit hit;
            float maxDistance = 6f;
            float force = 100000;
            Vector3 direction = -transform.up;

            Debug.DrawRay(transform.position, direction * maxDistance, Color.red);

            int layerMask = 1 << LayerMask.NameToLayer("Terrain");

            if (!Physics.Raycast(transform.position, direction, out hit, maxDistance, layerMask))
                _rb.AddRelativeForce(direction * force);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.tag == "Tree")
            {
                int index = other.GetComponent<Tree>().Index;
                GameController.Instance.TerrainController.DestroyTree(index, other.transform.position);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            HealthSystem target = collision.gameObject.GetComponent<HealthSystem>();
            if (target != null)
            {
                target.TakeDamage(10);
            }
        }

        private void OnDrawGizmos() { }
        #endregion
    }
}
