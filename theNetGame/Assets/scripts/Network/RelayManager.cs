using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Authentication;
using Unity.Services.Core;
using System.Threading.Tasks;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ================= HOST =================
    public async Task<string> StartHostWithRelay()
    {
        Debug.Log("Initializing Relay...");

        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("Signing in anonymously...");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        Debug.Log("Creating allocation...");
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);

        Debug.Log("Getting join code...");
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        Debug.Log("JOIN CODE GENERATED: " + joinCode);

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        transport.SetRelayServerData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData
        );

        NetworkManager.Singleton.StartHost();

        return joinCode;
    }

    // ================= CLIENT =================
    public async Task JoinRelay(string joinCode)
    {
        Debug.Log("Joining relay with code: " + joinCode);

        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("Signing in anonymously...");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        Debug.Log("Joining allocation...");
        JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        transport.SetRelayServerData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData,
            allocation.HostConnectionData
        );

        NetworkManager.Singleton.StartClient();

        Debug.Log("Client started!");
    }
}