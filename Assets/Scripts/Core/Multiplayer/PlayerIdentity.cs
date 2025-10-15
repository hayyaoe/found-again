using Unity.Collections;
using Unity.Netcode;

public class PlayerIdentity : NetworkBehaviour
{
    public readonly NetworkVariable<FixedString64Bytes> PlayerName =
        new(writePerm: NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            var name = GameNetwork.LobbyRoster.GetName(OwnerClientId);
            if (string.IsNullOrWhiteSpace(name)) name = $"Player_{OwnerClientId}";
            PlayerName.Value = name;
        }
    }
}
