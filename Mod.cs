using System.Runtime.InteropServices;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using KMod;

namespace ONIAccessibilityMod
{
    //==========================================================================
    // ONI ACCESS - Phase 1: Main Menu
    // Arrow keys use Unity Input, Enter uses KInputController
    //==========================================================================

    // NVDA Bridge - speaks text via NVDA screen reader
    public static class NVDA
    {
        [DllImport("nvdaControllerClient64.dll", CharSet = CharSet.Unicode)]
        public static extern int nvdaController_speakText(string text);

        public static void Speak(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            Debug.Log("[A11Y] Speaking: " + text);
            try
            {
                int result = nvdaController_speakText(text);
                if (result != 0)
                {
                    Debug.LogWarning("[A11Y] NVDA returned error: " + result);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[A11Y] NVDA exception: " + ex.Message);
            }
        }
    }

    // State Engine
    public enum GameState
    {
        None,
        MainMenu,
        Loading,
        ColonySetup,
        InGame,
        Paused
    }

    // Virtual Menu Item
    public class VirtualMenuItem
    {
        public string Name;
        public string ActivationPhrase;
        public System.Action Execute;

        public VirtualMenuItem(string name, string activationPhrase, System.Action execute)
        {
            Name = name;
            ActivationPhrase = activationPhrase;
            Execute = execute;
        }
    }

    //==========================================================================
    // Virtual Navigator
    //==========================================================================
    public static class VirtualNavigator
    {
        public static GameState CurrentState = GameState.None;
        public static List<VirtualMenuItem> CurrentMenu = new List<VirtualMenuItem>();
        public static int CurrentIndex = 0;
        public static bool MenuActive = false;

        public static void ActivateMainMenu()
        {
            Debug.Log("[A11Y] Activating Main Menu navigation");
            CurrentState = GameState.MainMenu;
            CurrentMenu.Clear();

            // Menu items matching ONI's main menu
            CurrentMenu.Add(new VirtualMenuItem("Resume Game", "Resuming Game", () => ClickButton("ResumeGame")));
            CurrentMenu.Add(new VirtualMenuItem("New Game", "Starting New Game", () => ClickButton("NewGame")));
            CurrentMenu.Add(new VirtualMenuItem("Load Game", "Opening Load Game", () => ClickButton("LoadGame")));
            CurrentMenu.Add(new VirtualMenuItem("Colony Summaries", "Opening Colony Summaries", () => ClickButton("ColonySummaries")));
            CurrentMenu.Add(new VirtualMenuItem("Mods", "Opening Mods", () => ClickButton("Mods")));
            CurrentMenu.Add(new VirtualMenuItem("Options", "Opening Options", () => ClickButton("Options")));
            CurrentMenu.Add(new VirtualMenuItem("Quit to Desktop", "Quitting Game", () => ClickButton("QuitToDesktop")));

            CurrentIndex = 0;
            MenuActive = true;

            Debug.Log("[A11Y] Menu has " + CurrentMenu.Count + " items");
            NVDA.Speak("Main Menu. " + CurrentMenu[CurrentIndex].Name);
        }

        public static void NavigateDown()
        {
            if (!MenuActive || CurrentMenu.Count == 0) return;
            CurrentIndex = (CurrentIndex + 1) % CurrentMenu.Count;
            Debug.Log("[A11Y] Navigate Down - Index: " + CurrentIndex);
            AnnounceCurrentItem();
        }

        public static void NavigateUp()
        {
            if (!MenuActive || CurrentMenu.Count == 0) return;
            CurrentIndex = (CurrentIndex - 1 + CurrentMenu.Count) % CurrentMenu.Count;
            Debug.Log("[A11Y] Navigate Up - Index: " + CurrentIndex);
            AnnounceCurrentItem();
        }

        public static void ExecuteCurrent()
        {
            if (!MenuActive || CurrentIndex < 0 || CurrentIndex >= CurrentMenu.Count) return;
            var item = CurrentMenu[CurrentIndex];
            Debug.Log("[A11Y] Executing: " + item.Name);
            NVDA.Speak(item.ActivationPhrase);
            MenuActive = false;
            if (item.Execute != null) item.Execute();
        }

        public static void AnnounceCurrentItem()
        {
            if (CurrentIndex >= 0 && CurrentIndex < CurrentMenu.Count)
            {
                Debug.Log("[A11Y] Announcing: " + CurrentMenu[CurrentIndex].Name);
                NVDA.Speak(CurrentMenu[CurrentIndex].Name);
            }
        }

        public static void Deactivate()
        {
            Debug.Log("[A11Y] Deactivating menu");
            MenuActive = false;
            CurrentMenu.Clear();
            CurrentState = GameState.None;
        }

        public static void SetState(GameState state)
        {
            CurrentState = state;
            Debug.Log("[A11Y] State changed to: " + state);
        }

        public static void ClickButton(string buttonName)
        {
            Debug.Log("[A11Y] Looking for button: " + buttonName);

            string[] namesToTry = new string[]
            {
                buttonName,
                "Button_" + buttonName,
                buttonName + "Button"
            };

            foreach (var btn in Object.FindObjectsOfType<KButton>())
            {
                if (btn == null) continue;
                string objName = btn.gameObject.name;

                foreach (string nameToTry in namesToTry)
                {
                    if (objName.Contains(nameToTry))
                    {
                        Debug.Log("[A11Y] Found and clicking: " + objName);
                        btn.SignalClick(KKeyCode.Mouse0);
                        return;
                    }
                }
            }

            Debug.LogWarning("[A11Y] Button not found: " + buttonName);
        }
    }

    //==========================================================================
    // Input Handler - MonoBehaviour for arrow keys (Unity Input)
    // Arrow keys don't go through KInputController, so we use Update()
    //==========================================================================
    public class A11YInputHandler : MonoBehaviour
    {
        public static A11YInputHandler Instance;
        private float lastInputTime = 0f;
        private const float INPUT_DELAY = 0.2f;

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[A11Y] Input handler created");
        }

        void Update()
        {
            if (!VirtualNavigator.MenuActive) return;
            if (Time.unscaledTime - lastInputTime < INPUT_DELAY) return;

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                Debug.Log("[A11Y] Unity Input: Down Arrow");
                lastInputTime = Time.unscaledTime;
                VirtualNavigator.NavigateDown();
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                Debug.Log("[A11Y] Unity Input: Up Arrow");
                lastInputTime = Time.unscaledTime;
                VirtualNavigator.NavigateUp();
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                Debug.Log("[A11Y] Unity Input: Enter");
                lastInputTime = Time.unscaledTime;
                VirtualNavigator.ExecuteCurrent();
            }
            else if (Input.GetKeyDown(KeyCode.Backspace))
            {
                Debug.Log("[A11Y] Unity Input: Backspace");
                lastInputTime = Time.unscaledTime;
                VirtualNavigator.Deactivate();
                NVDA.Speak("Menu closed");
            }
        }
    }

    //==========================================================================
    // Mod Entry Point
    //==========================================================================
    public class Mod : UserMod2
    {
        public static Harmony HarmonyInstance;

        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            HarmonyInstance = harmony;
            Debug.Log("[A11Y] ========================================");
            Debug.Log("[A11Y] ONI Access Mod Loading...");
            Debug.Log("[A11Y] ========================================");
            harmony.PatchAll();
            Debug.Log("[A11Y] All Harmony patches applied");
        }
    }

    //==========================================================================
    // Main Menu Detection - creates input handler and activates menu
    //==========================================================================
    [HarmonyPatch(typeof(MainMenu), "OnSpawn")]
    public class MainMenuPatch
    {
        static void Postfix(MainMenu __instance)
        {
            Debug.Log("[A11Y] MainMenu.OnSpawn detected!");

            // Create input handler if needed
            if (A11YInputHandler.Instance == null)
            {
                var go = new GameObject("A11YInputHandler");
                go.AddComponent<A11YInputHandler>();
                Debug.Log("[A11Y] Created input handler GameObject");
            }

            __instance.StartCoroutine(ActivateAfterDelay());
        }

        static System.Collections.IEnumerator ActivateAfterDelay()
        {
            yield return new WaitForSeconds(0.5f);
            VirtualNavigator.ActivateMainMenu();
        }
    }

    //==========================================================================
    // Game Load Complete Detection
    //==========================================================================
    [HarmonyPatch(typeof(Game), "OnSpawn")]
    public class GameSpawnPatch
    {
        static void Postfix()
        {
            Debug.Log("[A11Y] Colony loaded");
            VirtualNavigator.SetState(GameState.InGame);
            NVDA.Speak("Colony Loaded");
        }
    }
}
