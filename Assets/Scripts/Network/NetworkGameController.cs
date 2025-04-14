using Deforestation.Interaction;
using Deforestation.Machine;
using Deforestation.Recolectables;
using Photon.Pun;
using Photon.Realtime;
using StarterAssets;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deforestation.Network
{
    public class NetworkGameController : GameController
    {
        #region Fields
        private List<MachineController> _allMachines = new();
        private PhotonView _photonView;
        #endregion

        #region Properties
        public NetworkPlayer NetworkPlayer { get; set; }
        #endregion

        #region Unity Callbacks
        private void Start()
        {
            _photonView = GetComponent<PhotonView>();
        }
        #endregion

        #region Public Methods
        public void InitializePlayer(HealthSystem health, CharacterController player, Inventory inventory, InteractionSystem interaction, Transform playerFollow)
        {
            _playerHealth = health;
            _player = player;
            _inventory = inventory;
            _interactionSystem = interaction;
            _playerFollow = playerFollow;
            NetworkPlayer = GameObject.FindObjectOfType<NetworkPlayer>();
        }

        public void InitializeMachine(Transform follow, MachineController machine)
        {
            if (_machine != null)
            {
                _machine.HealthSystem.OnHealthChanged -= _uiController.UpdateMachineHealth;
            }

            _machineFollow = follow;
            _machine = machine;

            _machine.HealthSystem.OnHealthChanged += _uiController.UpdateMachineHealth;
            _machine.HealthSystem.TakeDamage(0); // Refrescar UI

            if (!_allMachines.Contains(machine))
                _allMachines.Add(machine);
        }

        public override void MachineMode(bool machineMode)
        {
            MachineModeOn = machineMode;

            // Ocultar avatar para todos
            if (NetworkPlayer != null)
            {
                NetworkPlayer.SetAvatarVisible(!machineMode);
            }

            //Player
            _player.gameObject.SetActive(!machineMode);
            _player.enabled = !machineMode;

            //Cursor + UI
            if (machineMode)
            {
                if (Inventory.HasResource(RecolectableType.HyperCrystal))
                    _machine.StartDriving(machineMode);

                _player.transform.parent = _machineFollow;
                _uiController.HideInteraction();
                Cursor.lockState = CursorLockMode.None;
                _virtualCamera.Follow = _machineFollow;

                _machine.enabled = true;
                _machine.WeaponController.enabled = true;
                _machine.GetComponent<MachineMovement>().enabled = true;

                // Marcar máquina como usada y verificar victoria
                if (PhotonNetwork.IsConnected && _machine != null)
                {
                    _photonView.RPC(nameof(MarkMachineAsUsed), RpcTarget.AllBuffered, _machine.GetComponent<PhotonView>().ViewID);
                }
            }
            else
            {
                _machine.enabled = false;
                _machine.WeaponController.enabled = false;
                _machine.GetComponent<MachineMovement>().enabled = false;

                _player.transform.parent = null;
                _virtualCamera.Follow = _playerFollow;
                Cursor.lockState = CursorLockMode.Locked;
            }

            Cursor.visible = machineMode;
        }

        [PunRPC]
        private void MarkMachineAsUsed(int viewID)
        {
            var pv = PhotonView.Find(viewID);
            if (pv != null)
            {
                var machine = pv.GetComponent<MachineController>();
                if (machine != null)
                {
                    machine.SetInUse(true);
                    Debug.Log($"Machine {viewID} marked as in use.");
                }
                CheckVictoryCondition();
            }
        }

        private void CheckVictoryCondition()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            bool allInUse = _allMachines.All(m => m != null && m.IsInUse);
            if (allInUse)
            {
                int winnerID = PhotonNetwork.LocalPlayer.ActorNumber;
                _photonView.RPC(nameof(AnnounceWinner), RpcTarget.All, winnerID);
            }
        }

        [PunRPC]
        private void AnnounceWinner(int actorID)
        {
            if (PhotonNetwork.CurrentRoom.Players.TryGetValue(actorID, out Player winner))
            {
                Debug.Log($"?? ¡El jugador {winner.NickName} ha ganado!");
            }
        }
        #endregion
    }
}
