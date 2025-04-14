using Deforestation.Interaction;
using Deforestation.Recolectables;
using Photon.Pun;
using StarterAssets;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deforestation.Network
{
    public class NetworkPlayer : MonoBehaviourPun
    {
        #region Fields

        [SerializeField] private HealthSystem _health;
        [SerializeField] private Inventory _inventory;
        [SerializeField] private InteractionSystem _interactions;
        [SerializeField] private CharacterController _controller;
        [SerializeField] private FirstPersonController _fps;
        [SerializeField] private StarterAssetsInputs _inputs;
        [SerializeField] private PlayerInput _inputsPlayer;
        [SerializeField] private GameObject _3dAvatar;
        [SerializeField] private Transform _playerFollow;

        private NetworkGameController _gameController;
        private Animator _anim;

        #endregion

        #region Unity Callbacks	
        private void Awake()
        {
            _anim = _3dAvatar.GetComponent<Animator>();
        }

        private void Start()
        {
            _gameController = FindObjectOfType<NetworkGameController>(true);

            if (photonView.IsMine)
            {
                _gameController.InitializePlayer(_health, _controller, _inventory, _interactions, _playerFollow);
                _gameController.NetworkPlayer = this;

                _health.OnHealthChanged += Hit;
                _health.OnDeath += Die;

                _health.enabled = true;
                _inventory.enabled = true;
                _interactions.enabled = true;
                _fps.enabled = true;
                _controller.enabled = true;

                Invoke(nameof(AddInitialCrystals), 1);
            }
            else
            {
                DisconectPlayer();
            }
        }

        private void Update()
        {
            if (!photonView.IsMine) return;

            float vertical = Input.GetAxis("Vertical");
            float horizontal = Input.GetAxis("Horizontal");

            bool isMoving = horizontal != 0 || vertical != 0;

            _anim.SetBool("Run", isMoving);

            if (isMoving)
            {
                // Esto hará que la animación de correr se reproduzca en reversa al ir hacia atrás
                _anim.SetFloat("RunSpeedMultiplier", vertical < 0 ? -1f : 1f);
            }
            else
            {
                _anim.SetFloat("RunSpeedMultiplier", 1f); // Por defecto
            }

            if (Input.GetKeyUp(KeyCode.Space))
                _anim.SetTrigger("Jump");
        }

        #endregion

        #region Public Methods

        public void SetAvatarVisible(bool isVisible)
        {
            _3dAvatar.SetActive(isVisible);

            if (photonView.IsMine)
            {
                photonView.RPC("RPC_SetAvatarVisible", RpcTarget.Others, isVisible);
            }
        }

        #endregion

        #region Private Methods

        private void AddInitialCrystals()
        {
            _inventory.AddRecolectable(RecolectableType.SuperCrystal, 7);
            _inventory.AddRecolectable(RecolectableType.HyperCrystal, 3);
        }

        private void DisconectPlayer()
        {
            Destroy(_health);
            Destroy(_inventory);
            Destroy(_interactions);
            Destroy(_fps);
            Destroy(_controller);
            Destroy(_inputs);
            Destroy(_inputsPlayer);
        }

        private void Die()
        {
            _anim.SetTrigger("Die");
            DisconectPlayer();
            enabled = false;
        }

        private void Hit(float obj)
        {
            _anim.SetTrigger("Hit");
        }

        [PunRPC]
        private void RPC_SetAvatarVisible(bool isVisible)
        {
            _3dAvatar.SetActive(isVisible);
        }

        #endregion
    }
}
