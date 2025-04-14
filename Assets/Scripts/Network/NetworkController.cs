using Deforestation.Machine;
using Deforestation.UI;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Deforestation.Network
{

	public class NetworkController : MonoBehaviourPunCallbacks
	{
		[SerializeField] private GameObject _explosionPrefab;
		[SerializeField] private UINetwork _ui;
		[SerializeField] private UIGameController _uIGameController;

		//Master
		[SerializeField] private List <Transform> _spawnPoints;
		private int _indexSpawns = 0;

		//Client
		private bool _waitingForSpawn = false;

		private GameObject _machine;
		private GameObject _player;

		void Start()
		{
			PhotonNetwork.ConnectUsingSettings();
		}

		public override void OnConnectedToMaster()
		{
			PhotonNetwork.JoinOrCreateRoom("DeforestationRoom", new RoomOptions { MaxPlayers = 10 }, null);
		}

		public override void OnJoinedRoom()
		{
			if (PhotonNetwork.IsMasterClient)
			{				

				SpawnMe(_spawnPoints[0].position);
				_indexSpawns = 1;				
			}
			else
			{
				_waitingForSpawn = true;
				photonView.RPC("RPC_SpawnPoint", RpcTarget.MasterClient);

			}
			
			_ui.LoadingComplete();
		}

		private void SpawnMe(Vector3 spawnPoint)
		{
			_player = PhotonNetwork.Instantiate("PlayerMultiplayer", spawnPoint, Quaternion.identity);
			_machine = PhotonNetwork.Instantiate("TheMachine", spawnPoint + Vector3.back * 7, Quaternion.identity);
			
			//dead control
			_player.GetComponent<HealthSystem>().OnDeath += PlayerDie;
			_machine.GetComponent<HealthSystem>().OnDeath += MachineDie;

			_uIGameController.enabled = true;
		}

		
		[PunRPC]
		void RPC_SpawnPoint()
		{			
			_indexSpawns++;
			if (_indexSpawns >= _spawnPoints.Count)
				_indexSpawns = 0;
			photonView.RPC("RPC_RecivePont", RpcTarget.Others, _spawnPoints[_indexSpawns].position);
			
		}

		[PunRPC]
		void RPC_RecivePont(Vector3 spawnPos)
		{
			if (_waitingForSpawn)
			{
				_waitingForSpawn = false;
				SpawnMe(spawnPos);
			}
		}

		private void MachineDie()
		{
			if (GameController.Instance.MachineModeOn)
			{
				GameController.Instance.MachineMode(false);
				_player.GetComponent<HealthSystem>().TakeDamage(1000);
			}

			DestroyImmediate(_machine);
			SpawnExplosions(_machine.transform.position + Vector3.up * 4, 5, 5);
		}

		public void SpawnExplosions(Vector3 centerPoint, int numberOfExplosions = 4, float maxDistance = 5f)
		{
			for (int i = 0; i < numberOfExplosions; i++)
			{
				Vector3 randomDirection = Random.insideUnitSphere;				
				Vector3 spawnPosition = centerPoint + randomDirection.normalized * Random.Range(0f, maxDistance);
				Instantiate(_explosionPrefab, spawnPosition, Quaternion.identity);
			}
		}
		private void PlayerDie()
		{
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
			_ui.EndGamePanel.SetActive(true);
		}

	}
}