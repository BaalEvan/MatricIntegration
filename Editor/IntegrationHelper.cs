using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Text;
using Newtonsoft.Json.Linq;

public class MatricIntegration
{
    public static string DECK_ID = "154a619a-bc4d-4389-a9a5-9e9ef9585277";
    public static string PAGE_ID = PAGE_ID_HORIZONTAL;
    public static string PAGE_ID_VERTICAL = "83d9d4a9-600c-4d51-8820-606428ef59c2";
    public static string PAGE_ID_HORIZONTAL = "6007bd84-61b1-4569-8854-0502d241285e";
    public static string SMILE_WINK = "😉";
    public static string APP_NAME = "Unity Console";
    public static int API_PORT = 50300;
    public static string PIN = "8661";

    public static string CLIENT_ID;
    public static IntegrationHelper mtrx;
}

public class IntegrationHelper {
    UdpClient udpClient;
    IPEndPoint serverEP = new IPEndPoint(IPAddress.Loopback, API_PORT);
    public static int API_PORT = MatricIntegration.API_PORT;
    public static int UDP_LISTENER_PORT = 50302;
    public Action<string> ParseMessage;
    private bool isKilled;

        public IntegrationHelper(Action<string> parseMethod)
        {
            ParseMessage = parseMethod;
            udpClient = new UdpClient(UDP_LISTENER_PORT);
            udpClient.BeginReceive(new AsyncCallback(ReceiveCallback), null);
        }

        public void Kill()
        {
            isKilled = true;
        }
    
        public void ReceiveCallback(IAsyncResult ar){
            IPEndPoint ep = new IPEndPoint(serverEP.Address, serverEP.Port);
            byte[] receiveBytes = udpClient.EndReceive(ar, ref ep);
            string receiveString = Encoding.ASCII.GetString(receiveBytes);

            ParseMessage(receiveString);

            if (isKilled)
            {
                udpClient.Close();
                udpClient.Dispose();
            }
            else 
                udpClient.BeginReceive(new AsyncCallback(ReceiveCallback), null);
        }



        /// <summary>
        /// Initiates the connection
        /// </summary>
        public void Connect() {
            string msg = $@"
            {{""command"":""CONNECT"", 
                ""appName"":""{MatricIntegration.APP_NAME}""}}
            ";
            UDPSend(msg);
        }

        /// <summary>
        ///Requests the list of connected MATRIC clients
        /// </summary>
        public void GetConnectedClients()
        {
            string msg = $@"
            {{""command"":""GETCONNECTEDCLIENTS"",
            ""appName"":""{MatricIntegration.APP_NAME}"",
            ""appPIN"":""{MatricIntegration.PIN}""}}
            ";
            UDPSend(msg);
        }

        /// <summary>
        /// Sets various button properties
        /// </summary>
        /// <param name="clientId">Target client id</param>
        /// <param name="buttonId">Id of the button we want to change</param>
        /// <param name="text">Button text to set</param>
        /// <param name="textcolorOn">Button text color in pressed state</param>
        /// <param name="textcolorOff">Button text color in normal state</param>
        /// <param name="backgroundcolorOff">Button background color in normal state</param>
        /// <param name="backgroundcolorOn">Button background color in pressed state</param>
        /// <param name="fontSize">Button relative font size - string: small, medium, large, xlarge, xxlarge, xxxlarge</param>
        /// <param name="imageOn">Button image in pressed state</param>
        /// <param name="imageOff">Button image in normal state</param>
        /// <param name="buttonName">Button name (preferred way to reference buttons, rather then by id)</param>
        public void SetButtonProperties(string clientId, string buttonName = null, string text = null, string textcolorOn = null, string textcolorOff = null,  
            string backgroundcolorOff = null, string backgroundcolorOn = null, string imageOn = null, string imageOff = null, string fontSize = null, string buttonId = null)
        {
            //Remark: if we do not want to change a particular property, we will send it as null
            string msg = $@"
            {{""command"":""SETBUTTONPROPS"", 
                ""appName"":""{MatricIntegration.APP_NAME}"", 
                ""appPIN"":""{MatricIntegration.PIN}"", 
                ""clientId"":""{clientId}"", 
                ""buttonId"":""{buttonId}"",
                ""buttonName"": ""{buttonName}"",
                    ""data"":{{
                        ""imageOff"": { (imageOff == null ? "null" : "\"" + imageOff + "\"") }, 
                        ""imageOn"":  { (imageOn == null ? "null" : "\"" + imageOff + "\"") }, 
                        ""textcolorOn"": { (textcolorOn == null ? "null" : "\"" + textcolorOn + "\"")}, 
                        ""textcolorOff"":{ (textcolorOff == null ? "null" : "\"" + textcolorOff + "\"")}, 
                        ""backgroundcolorOn"": { (backgroundcolorOn == null ? "null" : "\"" + backgroundcolorOn + "\"")}, 
                        ""backgroundcolorOff"":{ (backgroundcolorOff == null ? "null" : "\"" + backgroundcolorOff + "\"")}, 
                        ""fontSize"":{ (fontSize == null ? "null" : "\"" + fontSize + "\"")},
                        ""text"":{ (text == null ? "null" : "\"" + text + "\"")}
                    }}
            }}";
            UDPSend(msg);
        }

        /// <summary>
        /// Sets the active page
        /// </summary>
        /// <param name="clientId">Target client id</param>
        /// <param name="pageId">Page id</param>
        public void SetActivePage(string clientId, string pageId)
        { 
            string msg = $@"
            {{""command"":""SETACTIVEPAGE"", 
                ""appName"":""{MatricIntegration.APP_NAME}"", 
                ""appPIN"":""{MatricIntegration.PIN}"", 
                ""clientId"":""{clientId}"", 
                ""pageId"":""{pageId}""}}
            ";
            UDPSend(msg);
        }

        /// <summary>
        /// Sets deck and optionally page
        /// </summary>
        /// <param name="clientId">Target client id</param>
        /// <param name="deckId">deck id</param>
        /// <param name="pageId">page id</param>
        public void SetDeck(string clientId, string deckId, string pageId) {
            string msg = $@"
            {{""command"":""SETDECK"", 
                ""appName"":""{MatricIntegration.APP_NAME}"", 
                ""appPIN"":""{MatricIntegration.PIN}"", 
                ""clientId"":""{clientId}"", 
                ""deckId"":""{deckId}"",
                ""pageId"":""{pageId}""}}
            ";
            UDPSend(msg);
        }

        public class SetControlStateItem
        {
            public string controlId;
            public string controlName;
            public string state;

            public SetControlStateItem(string _controlId, string state, string _controlName = null)
            {
                this.controlId = _controlId;
                this.state = state;
                this.controlName = _controlName;
            }
        }

        public class VisualStateItem {
            public string buttonId;
            public string buttonName;
            public string state;

            public VisualStateItem(string buttonId, string state, string buttonName = null)
            {
                this.buttonId = buttonId;
                this.state = state;
                this.buttonName = buttonName;
            }
        }

        public void SetButtonsVisualState(string clientId, List<VisualStateItem> list) {
            string btnList = "";

            for (int i = 0; i < list.Count; i++) {
                VisualStateItem item = list[i];
                if (i != 0) {
                    btnList += ",";
                }
                btnList += $@"{{""buttonId"":""{ item.buttonId}"", ""buttonName"": ""{item.buttonName}"", ""state"":""{item.state}""}}";
            }

            string msg = $@"
            {{""command"":""SETBUTTONSVISUALSTATE"", 
                ""appName"":""{MatricIntegration.APP_NAME}"", 
                ""appPIN"":""{MatricIntegration.PIN}"", 
                ""clientId"":""{clientId}"", 
                ""data"":[{btnList}]
            }}
            ";
            UDPSend(msg);
        }

        public void SetControlsState(string clientId, List<SetControlStateItem> list)
        {
            string ctlList = "";

            for (int i = 0; i < list.Count; i++)
            {
                SetControlStateItem item = list[i];
                if (i != 0)
                {
                    ctlList += ",";
                }
                ctlList += $@"{{""controlId"":""{ item.controlId}"", ""controlName"": ""{item.controlName}"", ""state"":{item.state}}}";
            }

            string msg = $@"
            {{""command"":""SETCONTROLSSTATE"", 
                ""appName"":""{MatricIntegration.APP_NAME}"", 
                ""appPIN"":""{MatricIntegration.PIN}"", 
                ""clientId"":""{clientId}"", 
                ""data"":[{ctlList}]
            }}
            ";
            UDPSend(msg);
        }

        /// <summary>
        /// Sends UDP message to Matric server
        /// </summary>
        void UDPSend(string message)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            udpClient.Send(bytes, bytes.Length, serverEP);
       }
    }