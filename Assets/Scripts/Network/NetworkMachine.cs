
using Deforestation.Machine;
using Photon.Pun;
using System;
using UnityEngine;

namespace Deforestation.Network
{
	public class NetworkMachine : MonoBehaviourPun
	{
		#region Fields

		[SerializeField] private MachineController _machine;
		public Transform _machineFollow;
		private NetworkGameController _gameController;
		#endregion

		#region Properties
		#endregion

		#region Unity Callbacks	
		private void Start()
		{
			_gameController = FindObjectOfType<NetworkGameController>(true);
			if (photonView.IsMine)
			{
				_gameController.InitializeMachine(_machineFollow, _machine);
				_gameController.gameObject.SetActive(true);
				_machine.enabled = true;
				_machine.WeaponController.enabled = true;
				_machine.GetComponent<MachineMovement>().enabled = true;
				//Autoridad de la vida en local
				_machine.HealthSystem.OnHealthChanged += SyncHealth;
				//Autoridad de disparos en local
				_machine.WeaponController.OnMachineShoot += SyncShoot;
			}
			else
			{
				//---
			}
		}


		#endregion

		#region Private Methods
		private void SyncShoot()
		{
			//Capturar la direccion del cañon
			//TODO: refactorizar!
			RaycastHit hit;
			Ray ray = GameController.Instance.MainCamera.ScreenPointToRay(Input.mousePosition);
			Physics.Raycast(ray, out hit);

			//Mandar RPC
			photonView.RPC("OthersShoot", RpcTarget.Others, hit.point);
		}

		[PunRPC]
		private void OthersShoot(Vector3 shootDirection)
		{
			_machine.WeaponController.Shoot(shootDirection);
		}

		private void SyncHealth(float value)
		{
			photonView.RPC("RefreshHealth", RpcTarget.Others, value);
		}

		[PunRPC]
		private void RefreshHealth(float value)
		{
			_machine.HealthSystem.SetHealth(value);
		}

        [PunRPC]
        private void SyncIsMoving(bool isMoving)
        {
            _machine.SetIsMoving(isMoving);
        }

        #endregion

        #region Public Methods
        #endregion

    }

}