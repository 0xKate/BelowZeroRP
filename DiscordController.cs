using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BelowZeroRP
{
    enum GameState
    {
        Menu,
        Loading,
        Playing,            
    }

    struct GameDetails
    {
        public GameState State;
        public string Biome;
        public int Depth;
        public Discord.Activity Activity;

        public GameDetails(long clientID)
        {
            Activity = new Discord.Activity
            {
                Type = Discord.ActivityType.Playing,
                ApplicationId = clientID,
                Name = "Subnautica: Below Zero",
                State = "Main Menu",
                Details = "In the main menu.",
            };
        }
        
        public void Build()
        {
            if (State == GameState.Playing && Player.main != null)
            {
                var sub = Player.main.GetCurrentSub();
                if (sub != null)
                {
                    Activity.State = $"In {sub.name}";
                }
                else if (Player.main.GetVehicle() is var vehicle && vehicle != null)
                {
                    Activity.State = $"In {vehicle.name}";
                }
                else if (Player.main.IsUnderwaterForSwimming())
                {
                    Activity.State = "Swimming";
                }
                else if (!Player.main.IsSwimming())
                {
                    Activity.State = "On Foot";
                }

                Biome = Player.main.GetBiomeString();
                Depth = (int)Mathf.Round(Player.main.GetDepth());

                Activity.Details = $"Depth: {Depth}\nBiome: {Biome}";

            }
            else if (State == GameState.Loading)
            {
                {
                    Activity.State = "Loading";
                    Activity.Details = "Waiting for the pengling to cross!";
                }
            }
            else if (State == GameState.Menu)
            {
                Activity.State = "Main Menu";
                Activity.Details = "In the main menu.";
            }

        }
    }

    internal class DiscordController : MonoBehaviour
    {
        public const long CLIENT_ID = 1304353949335162921;


        public Discord.Discord API;
        public Discord.ActivityManager ActivityManager;

        private GameDetails ActivityInfo;

        public DiscordController()
        {
            ActivityInfo = new GameDetails(CLIENT_ID);
            API = new Discord.Discord(CLIENT_ID, (UInt64)Discord.CreateFlags.Default);
            ActivityManager = API.GetActivityManager();
        }

        public void OnEnable()
        {
            SceneManager.activeSceneChanged += OnSceneChange;
            //Player.main.playerBiomeEnterEvent += OnBiomeChange;
        }

        public void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnSceneChange;
            //Player.main.playerBiomeEnterEvent += OnBiomeChange;
        }

        private void OnBiomeChange(string last, string next)
        {
            Plugin.Logger.LogWarning($"LastBiome:{last}");
            Plugin.Logger.LogWarning($"NextBiome:{next}");
            ActivityInfo.Biome = next;
        }

        private void OnSceneChange(Scene last, Scene next)
        {
            Plugin.Logger.LogWarning($"LastScene:{last.name}");
            Plugin.Logger.LogWarning($"NextScene:{next.name}");

            switch (next.name)
            {
                case "Main":
                    ActivityInfo.State = GameState.Playing;
                    break;

                case "PreStartScreen":
                    ActivityInfo.State = GameState.Loading;
                    break;

                case "StartScreen":
                    ActivityInfo.State = GameState.Menu;
                    break;

                default:
                    ActivityInfo.State = GameState.Loading;
                    break;
            }
        }

        private float updateInterval = 0.1f; // 0.1 seconds for 10 times per second
        private float timeSinceLastUpdate = 0f;
        public void Update()
        {
            // Accumulate time since the last update
            timeSinceLastUpdate += Time.deltaTime;

            // Check if enough time has passed
            if (timeSinceLastUpdate >= updateInterval)
            {
                // Reset the timer
                timeSinceLastUpdate = 0f;

                //// Perform the update logic
                ActivityInfo.Build();

                ActivityManager.UpdateActivity(ActivityInfo.Activity, (result) =>
                {
                    if (result != Discord.Result.Ok)
                    {
                        Plugin.Logger.LogWarning($"Failed to update activity: {result}");
                    }
                });

                API.RunCallbacks();
            }
        }
    }
}
