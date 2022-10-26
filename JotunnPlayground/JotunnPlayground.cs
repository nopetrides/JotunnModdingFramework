// JotunnPlayground
// a Valheim mod skeleton using Jötunn
// 
// File:    JotunnPlayground.cs
// Project: JotunnPlayground

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using System.Text.RegularExpressions;
using System;
using UnityEngine;
using BepInEx.Bootstrap;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

namespace JotunnPlayground
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class JotunnPlayground : BaseUnityPlugin
    {
        public const string PluginGUID = "com.jotunn.JotunnPlayground";
        public const string PluginName = "JotunnPlayground";
        public const string PluginVersion = "0.0.1";
        
        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();
        public static JotunnPlayground Instance;

        internal const string LoggerName = "MyPluginLog";
        internal static ManualLogSource Log;
        internal static MyLogListener LogEcho;

        private ConfigEntry<bool> ShowLoadDoneMessage;
        private ConfigEntry<string> LoadDoneMessage;

        // todo
        private ConfigEntry<Color> MessageColor;
        private ConfigEntry<int> FontSize;
        private ConfigEntry<float> MessageDuration;
        private ConfigEntry<float> FadeOutTime;


        private static GameObject GUIInstance;
        private static Coroutine messageDisplay;

        private void Awake()
        {
            Instance = this;
            Log = new MyManualLogger(LoggerName);
            BepInEx.Logging.Logger.Sources.Add(Log);
            LogEcho = new MyLogListener();
            BepInEx.Logging.Logger.Listeners.Add(LogEcho);
            // Plugin startup logic
            Log.LogInfo($"Plugin {PluginGUID} is loaded!");
            Log.LogInfo("This C# script was coded by Noah Petrides and is built with BepInEx");

            BindConfigs();

            if (ShowLoadDoneMessage.Value)
            {
                Log.LogInfo(LoadDoneMessage.Value);
            }
            GUIManager.OnCustomGUIAvailable += BuildGUIObject;
        }

        private void BindConfigs()
        {
            ShowLoadDoneMessage = Config.Bind("Logging",             // The section under which the option is shown
                                              "ShouldShowLoadDoneMessage", // The key of the configuration option in the configuration file
                                              true,                  // The default value
                                              "Should the plugin show the load complete message"); // Description of the option to show in the config file);
            Log.LogInfo("ShouldShowLoadDoneMessage configured");

            LoadDoneMessage = Config.Bind("Logging",              // The section under which the option is shown
                                              "LoadDoneMessage", // The key of the configuration option in the configuration file
                                              "Plugin loaded and ready",   // The default value
                                              "Display a message when the plugin is ready"); // Description of the option to show in the config file);
            Log.LogInfo("LoadDoneMessage configured");

            MessageColor = Config.Bind("On Screen Messaging",             // The section under which the option is shown
                                             "MessageColor", // The key of the configuration option in the configuration file
                                             new Color(1,1,1,1),                  // The default value
                                             "The color of the messages"); // Description of the option to show in the config file);
            Log.LogInfo("MessageColor configured");

            FontSize = Config.Bind("On Screen Messaging",             // The section under which the option is shown
                                             "FontSize", // The key of the configuration option in the configuration file
                                             20,                  // The default value
                                             "The size of the messages"); // Description of the option to show in the config file);
            Log.LogInfo("FontSize configured");

            MessageDuration = Config.Bind("On Screen Messaging",             // The section under which the option is shown
                                             "MessageDuration", // The key of the configuration option in the configuration file
                                             5f,                  // The default value
                                             "How long the message shows for before fading away"); // Description of the option to show in the config file);
            Log.LogInfo("MessageDuration configured");

            FadeOutTime = Config.Bind("On Screen Messaging",             // The section under which the option is shown
                                             "FadeOutTime", // The key of the configuration option in the configuration file
                                             .5f,                  // The default value
                                             "How quickly the message fades away after its duration"); // Description of the option to show in the config file);
            Log.LogInfo("FadeOutTime configured");
        }

        private void BuildGUIObject()
        {
            if (GUIInstance)
            {
                Log.LogError("The GUI object already exists!");
                return;
            }
            var go = new GameObject("GUI Object");
            GUIInstance = Instantiate<GameObject>(go, GUIManager.CustomGUIFront.transform);
            GUIInstance.AddComponent<CanvasRenderer>();
            GUIInstance.transform.localPosition = Vector3.zero;
            var rt = GUIInstance.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.7f, 0.65f);
            rt.anchorMax = new Vector2(0.97f, 0.65f);
            var text = GUIInstance.AddComponent<Text>();
            text.font = Font.CreateDynamicFontFromOSFont("Arial", FontSize.Value);
            text.text = "";
            text.color = MessageColor.Value;
            text.fontSize = FontSize.Value;
            text.alignment = TextAnchor.MiddleRight;
            text.raycastTarget = false;

            if (ShowLoadDoneMessage.Value)
            {
                QueueMessage(LoadDoneMessage.Value);
            }
        }

        private Queue<string> queuedMessages = new Queue<string>();
        
        public void QueueMessage(string message)
        {
            queuedMessages.Enqueue(message);
            if (messageDisplay == null)
            {
                ShowNextQueuedMessage();
            }
        }

        private void ShowNextQueuedMessage()
        {
            if (queuedMessages.Count > 0)
            {
                messageDisplay = 
                    StartCoroutine(
                        ShowMessageAndFadeAway(queuedMessages.Dequeue()));
            }
        }

        private IEnumerator ShowMessageAndFadeAway(string message)
        {
            float nonFadeTime = MessageDuration.Value;
            float fadeTime = FadeOutTime.Value;
            float time = 0f;
            var tc = GUIInstance.GetComponent<Text>();
            tc.text = message;
            Color ogColor = MessageColor.Value;
            tc.color = ogColor;

            do {
                time += Time.deltaTime;
                if (time > nonFadeTime)
                {
                    var color = tc.color;
                    color.a = Mathf.Lerp(ogColor.a, 0, (time - nonFadeTime) / (fadeTime));
                    tc.color = color;
                }
                yield return null;
            } while (time < nonFadeTime+fadeTime);

            messageDisplay = null;
            ShowNextQueuedMessage();
        }
    }

    internal class MyManualLogger : ManualLogSource
    {

        public MyManualLogger(string sourceName) : base(sourceName)
        {
            // no custom constructor logic yet
        }
    }

    internal class MyLogListener : ILogListener, IDisposable
    {
        internal bool WriteUnityLogs { get; set; } = true;

        public void LogEvent(object sender, LogEventArgs eventArgs)
        {
            if ((sender is MyManualLogger))
            {
                return;
            }

            if (ContainsSoughtInfo(eventArgs.ToString(), out string message))
            {
                JotunnPlayground.Log.LogInfo(message);
                JotunnPlayground.Instance.QueueMessage(message);
            }
        }

        public void Dispose()
        {
        }

        // Add to config
        private bool EchoLogs = true;
        private const string LogQueryDungeon = "Dungeon loaded *";
        private const string LogQuerySpawned = "Spawned ";
        private const string DungeonMessaage = "A Dungeon is nearby";
        private const string SpawnMessage = "{0} {1} appeared nearby";
        /// <summary>
        /// Check a log, and see if we want that info
        /// </summary>
        private bool ContainsSoughtInfo(string log, out string message)
        {
            message = "";
            if (!EchoLogs)
            {
                return false;
            }
            if (Regex.IsMatch(log, LogQueryDungeon))
            {
                message = DungeonMessaage;
                return true;
            }
            if (Regex.IsMatch(log, LogQuerySpawned))
            {
                int mobNameStart = log.IndexOf(LogQuerySpawned) + LogQuerySpawned.Length;
                int mobNameEnd = log.IndexOf(" x ", mobNameStart);
                string mobName = log.Substring(mobNameStart, mobNameEnd - mobNameStart);
                int numberStart = log.LastIndexOf(" ") + 1;
                string mobCount = log.Substring(numberStart).Trim('\r', '\n');
                message = string.Format(SpawnMessage, mobCount, mobName);
                return true;
            }
            return false;
        }
    }
}

