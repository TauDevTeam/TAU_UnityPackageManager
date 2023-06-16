using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TAU_OpenGlove;
using UnityEngine;


public enum HubClientStatus
{
    DISCONNECTED,
    INIT_CONNECTION,
    HUB_FOUND,
    WAIT_OK,
    LOOP_RECEIVE,
    ERROR
}


public class UDP_Client : MonoBehaviour
{
    public HubDataParcer parcer;

    private bool quitProcess = false;

    public string hubIP = "192.168.1.59";
    const ushort hubPort = 19920;
    byte[] _responseBuffer = new byte[4096];
    private Socket _tauSocket;


    [Header("DEBUG")]
    public bool printIncomingBytes = false;

    public float drqSendTimer = 0f;



    public bool autoStartClient = false;
    private void Start()
    {
        if (autoStartClient)
        {
            SetupHUBServer();
        }
    }

    private void Update()
    {
        if (quitProcess == false && Input.GetKeyDown(KeyCode.Escape))
        {
            StopClient();
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            SendDRQ();
        }

        if (_tauSocket.Connected)
        {
            drqSendTimer -= Time.unscaledDeltaTime;

            if (drqSendTimer <= 0f)
            {
                drqSendTimer = 20f;
                SendDRQ();
            }
        }
    }


    #region CLIENT_COMMUNICATION

    [ContextMenu("CREATE SOCKET")]
    private void SetupHUBServer()
    {
        Debug.Log("SetupHUBServer");
        _tauSocket = new Socket
        (
            AddressFamily.InterNetwork,
            SocketType.Dgram,
            ProtocolType.Udp
        );

        _tauSocket.Blocking = false;

        try
        {
            _tauSocket.Connect(new IPEndPoint(IPAddress.Parse(hubIP), hubPort));
        }
        catch (SocketException ex)
        {
            Debug.Log(ex.Message);
        }

        Debug.Log("Connected...");

        SendDRQ();

        _tauSocket.BeginReceive(_responseBuffer, 0, _responseBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
    }


    [ContextMenu("SEND DRQ")]
    public void SendDRQ()
    {
        Debug.Log("SendDRQ...");
        string message = "drq101";
        byte[] drq_payload_bytes = Encoding.UTF8.GetBytes(message);
        SendData(drq_payload_bytes);
    }


    private void ReceiveCallback(IAsyncResult AR)
    {
        int recieved = _tauSocket.EndReceive(AR);

        if (recieved <= 0)
            return;

        byte[] recData = new byte[recieved];
        Buffer.BlockCopy(_responseBuffer, 0, recData, 0, recieved);

        _tauSocket.BeginReceive(_responseBuffer, 0, _responseBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);

        if (printIncomingBytes)
        {
            string s = ByteArrayToStringV2(recData);
            Debug.Log("Incoming bytes: ");
            Helpers.PrintByteArray(recData);
            Debug.Log(s);
        }

        //  Debug.Log(Convert.ToChar(recData[0]) + "" + Convert.ToChar(recData[1]));
        if (recData[0] == 'a' && recData[1] == '2')
        {
            if (parcer != null)
            {
                parcer.ParceData(recData);
            }
        }
    }

    #endregion
    #region UTILS

    public static string ByteArrayToString(byte[] ba)
    {
        StringBuilder hex = new StringBuilder(ba.Length * 2);
        foreach (byte b in ba)
            hex.AppendFormat("{0:x2} ", b);
        return hex.ToString();
    }

    public static string ByteArrayToStringV2(byte[] bytes)
    {
        var sb = new StringBuilder("{ ");
        foreach (var b in bytes)
        {
            sb.Append(b + ", ");
        }
        sb.Append("}");

        return sb.ToString();
    }

    #endregion
    #region STOP_APPLICATION

    void StopClient()
    {
        quitProcess = true;
        StartCoroutine(StopClient_process());
    }


    IEnumerator StopClient_process()
    {
        _tauSocket.Close();
        yield return new WaitForSecondsRealtime(0.5f);

        if (Application.platform == RuntimePlatform.WindowsPlayer)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
        else
        {
            Application.Quit();
        }
    }


    private void SendData(byte[] data)
    {
        SocketAsyncEventArgs socketAsyncData = new SocketAsyncEventArgs();
        socketAsyncData.SetBuffer(data, 0, data.Length);
        _tauSocket.SendAsync(socketAsyncData);
    }

    #endregion
}
