using FlyingWormConsole3;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class ConsoleInform
{
    private static ConsoleInform instance;

    public Dictionary<LogType, int> catchedLogs;
    private readonly int logsByRef;
    private readonly IntegrationHelper helper;
    public const string LOG_BTN = "ed9452ae-f6fe-4c4e-8b2e-8a06508fb837";
    public const string WARNING_BTN = "a220f2fc-ea06-4f2a-8557-a4610d61b241";
    public const string ERROR_BTN = "dc64658f-83bc-453e-b3cb-0cdec5609a02";
    public const string ERROR_NAME_TEXT = "d8c9bb2b-ca97-4d8a-8bc4-9694c8def2c6";
    public const string ERROR_SCRIPT_TEXT = "071b32fc-30d7-4ac1-9343-cfcb993fc956";
    public const string PLAY_BTN = "29edd50b-d2b6-408e-bc11-fd4e85d486d6";
    public const string PAUSE_BTN = "86614230-f530-4241-92a1-3d6b153d4ea9";
    public const string SPEED_CONTROL = "c2ed4ca1-c9c3-4405-a2d5-705d0832926a";

    private string errorLogName;
    private string errorLogStack;

    public bool shouldOpenConsole;
    public bool playModeChanged;
    public bool pauseModeChanged;
    public bool speedChanged;
    public int newSpeed;
    private ConsolePro3Window window;
    public ConsoleInform()
    {
        helper = new IntegrationHelper(ParseMessage);
        helper.GetConnectedClients();

        catchedLogs = new Dictionary<LogType, int>();
        catchedLogs.Add(LogType.Error, 0);
        catchedLogs.Add(LogType.Warning, 0);
        catchedLogs.Add(LogType.Log, 0);

        Application.logMessageReceived += ApplicationOnLogMessageReceived;
        EditorApplication.update += Update;
        Application.quitting += Dispose;
        AssemblyReloadEvents.beforeAssemblyReload += Dispose;

        helper.SetDeck(MatricIntegration.CLIENT_ID, MatricIntegration.DECK_ID, MatricIntegration.PAGE_ID);
    }
    [MenuItem("Error/Error")]
    public static void Error()
    {
        Debug.LogError("Error showed");
    }

    [MenuItem("Error/Exception")]
    public static void Exception()
    {
        Debug.LogException(new NullReferenceException("I'm not found"));
    }

    private void Dispose()
    {
        helper.Kill();
        Application.logMessageReceived -= ApplicationOnLogMessageReceived;
        EditorApplication.update -= Update;
        Application.quitting -= Dispose;
        AssemblyReloadEvents.beforeAssemblyReload -= Dispose;
    }

    public void ParseMessage(string json)
    {
        var obj = JObject.Parse(json);
        var messageType = obj.GetValue("MessageType").ToString();

        switch (messageType)
        {
            case "ClientList":
                UpdateClientsList(obj.GetValue("MessageData").ToString());
                break;
            case "ControlInteraction":
                try
                {
                    OnControlInteraction(obj.GetValue("MessageData").ToString());

                    var data = JObject.Parse(obj.GetValue("MessageData").ToString());

                    if (data["MessageData"]["ControlId"]?.ToString() == ERROR_BTN)
                    {
                        shouldOpenConsole = true;
                    }
                    if (data["MessageData"]["ControlId"]?.ToString() == PLAY_BTN)
                    {
                        playModeChanged = true;
                    }

                    if (data["MessageData"]["ControlId"]?.ToString() == PAUSE_BTN)
                    {
                        pauseModeChanged = true;
                    }


                    if (data["MessageData"]["ControlId"]?.ToString() == SPEED_CONTROL)
                    {
                        speedChanged = true;
                        newSpeed = int.Parse(data["MessageData"]["Data"]["next"].ToString());
                    }


                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                break;

        }
    }


    public void Update()
    {

        if (speedChanged)
        {
            Time.timeScale = newSpeed switch
            {
                0 => 0,
                1 => 1,
                2 => 3,
                3 => 5,
                4 => 10,
                _ => Time.timeScale
            };

            speedChanged = false;
        }

        if (pauseModeChanged)
        {
            EditorApplication.isPaused = !EditorApplication.isPaused;
            pauseModeChanged = false;
            SetCounters();
        }

        if (playModeChanged)
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.EnterPlaymode();
            }
            else
            {
                EditorApplication.ExitPlaymode();
            }
            pauseModeChanged = false;
            SetCounters();

        }


        if (!shouldOpenConsole)
            return;

        if (window == null)
        {
            if (EditorWindow.HasOpenInstances<ConsolePro3Window>())
            {
                window = EditorWindow.GetWindow<ConsolePro3Window>();
                window.Close();
                window = null;
            }

            else
            {
                window = ScriptableObject.CreateInstance(typeof(ConsolePro3Window)) as ConsolePro3Window;
                window.position = new Rect(50, 50, Screen.currentResolution.width - 100, Screen.currentResolution.height - 100);
                window.ShowPopup();

            }

        }
        else
        {
            window.Close();
            window = null;
        }

        shouldOpenConsole = false;
    }

    public void OnControlInteraction(string data)
    {
        Debug.Log("Control interaction:");
        Debug.Log(data);


    }

    public void UpdateClientsList(string json)
    {
        var connectedClients = (JArray)JsonConvert.DeserializeObject(json);


        if (connectedClients.Count == 0)
        {
            Debug.Log("No connected devices found, make sure your smartphone/tablet is connected\nPress any key to exit");
            return;
        }

        Debug.Log("Found devices:");

        foreach (JObject client in connectedClients)
        {
            Debug.Log($@"{client.GetValue("clientID")} {client.GetValue("clientName")}");
        }

        MatricIntegration.CLIENT_ID = ((JObject)connectedClients[0]).GetValue("clientId").ToString();
        Debug.Log("Starting demo on first device");
    }

    public void GetCounts()
    {
        object[] arguments = { catchedLogs[LogType.Error], catchedLogs[LogType.Warning], catchedLogs[LogType.Log] };

        var assembly = Assembly.GetAssembly(typeof(Editor));
        var type = assembly.GetType("UnityEditor.LogEntries");
        var method = type.GetMethod("GetCountsByType");
        method.Invoke(new object(), arguments);
        catchedLogs[LogType.Error] = (int)arguments[0];
        catchedLogs[LogType.Warning] = (int)arguments[1];
        catchedLogs[LogType.Log] = (int)arguments[2];
    }
    [MenuItem("Error/Initialize")]

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void Initialize()
    {
        if (instance != null)
        {
            instance.Dispose();
        }
        instance = new ConsoleInform();

    }

    private void ApplicationOnLogMessageReceived(string condition, string stacktrace, LogType type)

    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            errorLogName = condition;
            errorLogStack = stacktrace;
        }

        SetCounters();
    }

    private void SetCounters()
    {
        GetCounts();

        helper.SetButtonsVisualState(MatricIntegration.CLIENT_ID,
            new List<IntegrationHelper.VisualStateItem> { new(ERROR_BTN, catchedLogs[LogType.Error] > 0 ? "on" : "off") });

        helper.SetButtonProperties(MatricIntegration.CLIENT_ID, buttonId: ERROR_BTN, text: catchedLogs[LogType.Error].ToString());

        helper.SetButtonProperties(MatricIntegration.CLIENT_ID, buttonId: WARNING_BTN, text: catchedLogs[LogType.Warning].ToString());

        helper.SetButtonProperties(MatricIntegration.CLIENT_ID, buttonId: LOG_BTN, text: catchedLogs[LogType.Log].ToString());


        helper.SetButtonProperties(MatricIntegration.CLIENT_ID, buttonId: ERROR_NAME_TEXT, text: errorLogName);

        helper.SetButtonProperties(MatricIntegration.CLIENT_ID, buttonId: ERROR_SCRIPT_TEXT, text: errorLogStack);

        helper.SetButtonsVisualState(MatricIntegration.CLIENT_ID,
            new List<IntegrationHelper.VisualStateItem> { new(PAUSE_BTN, EditorApplication.isPaused ? "on" : "off") });
        helper.SetButtonsVisualState(MatricIntegration.CLIENT_ID,
            new List<IntegrationHelper.VisualStateItem> { new(PLAY_BTN, EditorApplication.isPlaying ? "on" : "off") });

    }
}