
using Deforestation.Interaction;
using Deforestation.Machine;
using UnityEngine;

namespace Deforestation.Network
{

	public class NetworInteractions : MachineInteraction
	{
		#region Fields
		#endregion

		#region Properties
		#endregion

		#region Unity Callbacks	
		#endregion

		#region Private Methods
		#endregion

		#region Public Methods
		public override void Interact()
		{
			if (_type == MachineInteractionType.Door)
			{
				//Move Door
				transform.position = _target.position;
			}
			if (_type == MachineInteractionType.Stairs)
			{
				//Teleport Player
				GameController.Instance.TeleportPlayer(_target.position);
			}
			if (_type == MachineInteractionType.Machine)
			{
				MachineController machine = _target.GetComponent<MachineController>();
				Transform follow = _target.GetComponent<NetworkMachine>()._machineFollow;
				(NetworkGameController.Instance as NetworkGameController).InitializeMachine(follow, machine);
				GameController.Instance.MachineMode(true);
			}
		}

		#endregion

	}

}