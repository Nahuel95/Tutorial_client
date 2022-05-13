using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Networking;

public class Client : MonoBehaviour
{
    public static Client Instance { private set; get; }

    private const int MAX_USER = 100;
    private const int PORT = 26000;
    private const int WEB_PORT = 26001;
    private const string SERVER_IP = "127.0.0.1";
    private const int BYTE_SIZE = 1024;


    private byte reliableChannel;
    private int connectionId;
    private int hostId;
    private byte error;

    public Account self;
    private string token;
    private bool isStarted = false;
    // Use this for initialization
    private void Start()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Init();
    }

    private void Update()
    {
        UpdateMessagePump();
    }

    public void Init()
    {
        NetworkTransport.Init();

        ConnectionConfig cc = new ConnectionConfig();
        reliableChannel = cc.AddChannel(QosType.Reliable);

        HostTopology topo = new HostTopology(cc, MAX_USER);

        hostId = NetworkTransport.AddHost(topo, 0);

#if UNITY_WEBGL && !UNITY_EDITOR
        connectionId = NetworkTransport.Connect(hostId, SERVER_IP, WEB_PORT, 0, out error);
        Debug.Log("Connecting from Web");
#else
        connectionId = NetworkTransport.Connect(hostId, SERVER_IP, PORT, 0, out error);
        Debug.Log("Connecting from standalone");
#endif


        Debug.Log(string.Format("Attempting to connect on {0}", SERVER_IP));
        isStarted = true;
    }

    public void Shutdown()
    {
        isStarted = false;
        NetworkTransport.Shutdown();
    }

    public void UpdateMessagePump()
    {
        if (!isStarted)
        {
            return;
        }

        int recHostId;
        int connectionId;
        int channelId;

        byte[] recBuffer = new byte[BYTE_SIZE];
        int dataSize;

        NetworkEventType type = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, BYTE_SIZE, out dataSize, out error);
        switch (type)
        {
            case NetworkEventType.Nothing:
                break;

            case NetworkEventType.ConnectEvent:
                Debug.Log("You have connected to the server");
                break;


            case NetworkEventType.DisconnectEvent:
                Debug.Log("You have been disconnected from server");
                break;

            case NetworkEventType.DataEvent:
                BinaryFormatter formatter = new BinaryFormatter();
                MemoryStream ms = new MemoryStream(recBuffer);
                NetMsg msg = (NetMsg)formatter.Deserialize(ms);

                OnData(connectionId, channelId, recHostId, msg);
                break;

            default:
            case NetworkEventType.BroadcastEvent:
                Debug.Log("Unexpected network event type");
                break;
        }

    }

    #region Send
    public void SendServer(NetMsg msg) {
        byte[] buffer = new byte[BYTE_SIZE];

        BinaryFormatter formatter = new BinaryFormatter();
        MemoryStream ms = new MemoryStream(buffer);
        formatter.Serialize(ms, msg);

        NetworkTransport.Send(hostId, connectionId, reliableChannel, buffer, BYTE_SIZE, out error);
    }

    public void SendCreateAccount(string username, string password, string email) {

        if (!Utility.IsEmail(email))
        {
            LobbyScene.Instance.ChangeAuthenticationMessage("Email is invalid");
            LobbyScene.Instance.EnableInputs();
            return;
        }

        if (!Utility.IsUsername(username))
        {
            LobbyScene.Instance.ChangeAuthenticationMessage("Username is invalid");
            LobbyScene.Instance.EnableInputs();
            return;
        }

        if (password == null || password == "")
        {
            LobbyScene.Instance.ChangeAuthenticationMessage("Password is invalid");
            LobbyScene.Instance.EnableInputs();
            return;
        }

        Net_CreateAccount ca = new Net_CreateAccount();

        ca.Username = username;
        ca.Password = Utility.Sha256FromString(password);
        ca.Email = email;

        LobbyScene.Instance.ChangeAuthenticationMessage("Sending request...");

        SendServer(ca);
    }

    public void SendLoginRequest(string usernameOrEmail, string password) {

        if (!Utility.IsUsernameAndDiscriminator(usernameOrEmail) && !Utility.IsEmail(usernameOrEmail))
        {
            LobbyScene.Instance.ChangeAuthenticationMessage("Enter your Email or Username#Discriminator");
            LobbyScene.Instance.EnableInputs();
            return;
        }

        if (password == null || password == "")
        {
            LobbyScene.Instance.ChangeAuthenticationMessage("Password is invalid");
            LobbyScene.Instance.EnableInputs();
            return;
        }

        Net_LoginRequest lr = new Net_LoginRequest();

        lr.UsernameOrEmail = usernameOrEmail;
        lr.Password = Utility.Sha256FromString(password);

        LobbyScene.Instance.ChangeAuthenticationMessage("Sending login request...");

        SendServer(lr);
    }
    #endregion

    private void OnData(int cnnId, int channelId, int recHostId, NetMsg msg)
    {
        Debug.Log("Recieved a message of type " + msg.OP);
        switch (msg.OP)
        {
            case NetOP.None:
                Debug.Log("Unexpected NET OP");
                break;

            case NetOP.OnCreateAccount:
                OnCreateAccount((Net_OnCreateAccount)msg);
                
                break;

            case NetOP.OnLoginRequest:
                OnLoginRequest((Net_OnLoginRequest)msg);
                break;

            case NetOP.OnAddFollow:
                OnAddFollow((Net_OnAddFollow)msg);
                break;
        }
    }

    private void OnCreateAccount(Net_OnCreateAccount oca) {
        LobbyScene.Instance.EnableInputs();
        LobbyScene.Instance.ChangeAuthenticationMessage(oca.Information);

    }
    private void OnLoginRequest(Net_OnLoginRequest olr)
    {
        LobbyScene.Instance.ChangeAuthenticationMessage(olr.Information);
        Debug.Log(olr.Success + " ,"+ olr.Information);
        if (olr.Success != 1)
        {
            LobbyScene.Instance.EnableInputs();
        }
        else {
            //Successfull Login
            UnityEngine.SceneManagement.SceneManager.LoadScene("Hub");

            self = new Account();
            self.ActiveConnection = olr.ConnectionId;
            self.Username = olr.Username;
            self.Discriminator = olr.Discriminator;

            token = olr.Token;
        }
    }

    private void OnAddFollow(Net_OnAddFollow oaf){
        HubScene.Instance.AddFollowToUi(oaf.Follow);
    }

    public void SendAddFollow(string usernameOrEmail) {
        Net_AddFollow af = new Net_AddFollow();
        af.Token = token;
        af.UsernameDiscriminatorOrEmail = usernameOrEmail;

        SendServer(af);
    }
    public void SendRemoveFollow(string username)
    {
        Net_RemoveFollow rf = new Net_RemoveFollow();
        rf.Token = token;
        rf.UsernameDiscriminator = username;

        SendServer(rf);
    }
    public void SendRequestFollow()
    {
        Net_RequestFollow rf = new Net_RequestFollow();
        rf.Token = token;

        SendServer(rf);
    }
}
