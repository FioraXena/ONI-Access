using System.Runtime.InteropServices;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using KMod;

namespace ONIAccessibilityMod
{
    //==========================================================================
    // ONI ACCESS - Phase 1: Main Menu (Modular Approach)
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
            try { nvdaController_speakText(text); } catch { }
        }
    }

    // State Engine - tracks current game state
    public enum GameState
    {
        None,
        MainMenu,
        ColonySetup,
        InGame,
        Paused
    }

    // Virtual Menu Item
    public class VirtualMenuItem
    {
        public string Name;
        public System.Action Execute;

        public VirtualMenuItem(string name, System.Action execute)
        {
            Name = name;
            Execute = execute;
        }
    }

    //==========================================================================
    // Virtual Navigator - handles all menu navigation
    //==========================================================================
    public static class VirtualNavigator
    {
        public static GameState CurrentState = GameState.None;
        public static List<VirtualMenuItem> CurrentMenu = new List<VirtualMenuItem>();
        public static int CurrentIndex = 0;
        public static bool MenuActive = false;

        // Phase 1: Main Menu
        public static void ActivateMainMenu()
        {
            CurrentState = GameState.MainMenu;
            CurrentMenu.Clear();

            // Virtual menu items with button mappings
            CurrentMenu.Add(new VirtualMenuItem("Resume Game", () => ClickButton("Button_ResumeGame")));
            CurrentMenu.Add(new VirtualMenuItem("New Game", () => ClickButton("Button_NewGame")));
            CurrentMenu.Add(new VirtualMenuItem("Load Game", () => ClickButton("Button_LoadGame")));
            CurrentMenu.Add(new VirtualMenuItem("Options", () => ClickButton("Button_Options")));
            CurrentMenu.Add(new VirtualMenuItem("Mods", () => ClickButton("Button_Mods")));
            CurrentMenu.Add(new VirtualMenuItem("Quit Game", () => ClickButton("Button_QuitGame")));

            CurrentIndex = 0;
            MenuActive = true;

            NVDA.Speak("Main Menu");
            AnnounceCurrentItem();
        }

        public static void NavigateDown()
        {
            if (!MenuActive || CurrentMenu.Count == 0) return;
            CurrentIndex = (CurrentIndex + 1) % CurrentMenu.Count;
            AnnounceCurrentItem();
        }

        public static void NavigateUp()
        {
            if (!MenuActive || CurrentMenu.Count == 0) return;
            CurrentIndex = (CurrentIndex - 1 + CurrentMenu.Count) % CurrentMenu.Count;
            AnnounceCurrentItem();
        }

        public static void ExecuteCurrent()
        {
            if (!MenuActive || CurrentIndex < 0 || CurrentIndex >= CurrentMenu.Count) return;
            var item = CurrentMenu[CurrentIndex];
            Debug.Log("[A11Y] Executing: " + item.Name);
            if (item.Execute != null) item.Execute();
        }

        public static void AnnounceCurrentItem()
        {
            if (CurrentIndex >= 0 && CurrentIndex < CurrentMenu.Count)
            {
                NVDA.Speak(CurrentMenu[CurrentIndex].Name);
            }
        }

        public static void Deactivate()
        {
            MenuActive = false;
            CurrentMenu.Clear();
            CurrentState = GameState.None;
        }

        public static void ClickButton(string buttonName)
        {
            foreach (var btn in Object.FindObjectsOfType<KButton>())
            {
                if (btn != null && btn.gameObject.name == buttonName)
                {
                    Debug.Log("[A11Y] Clicking: " + buttonName);
                    btn.SignalClick(KKeyCode.Mouse0);
                    return;
                }
            }
            Debug.Log("[A11Y] Button not found: " + buttonName);
        }
    }

    //==========================================================================
    // Input Handler - captures keyboard input via Update loop
    //==========================================================================
    public class A11YInputHandler : MonoBehaviour
    {
        public static A11YInputHandler Instance;
        private float lastInputTime = 0f;
        private const float INPUT_DELAY = 0.15f; // Prevent rapid repeat

        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[A11Y] Input handler created and marked persistent");
        }

        void Update()
        {
            // Log periodically to confirm Update is running
            if (Time.frameCount % 300 == 0)
            {
                Debug.Log("[A11Y] Input handler Update running, MenuActive=" + VirtualNavigator.MenuActive);
            }

            if (!VirtualNavigator.MenuActive) return;

            // Rate limit input
            if (Time.time - lastInputTime < INPUT_DELAY) return;

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                Debug.Log("[A11Y] Down arrow pressed");
                lastInputTime = Time.time;
                VirtualNavigator.NavigateDown();
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                Debug.Log("[A11Y] Up arrow pressed");
                lastInputTime = Time.time;
                VirtualNavigator.NavigateUp();
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                Debug.Log("[A11Y] Enter pressed");
                lastInputTime = Time.time;
                VirtualNavigator.ExecuteCurrent();
            }
            else if (Input.GetKeyDown(KeyCode.Backspace))
            {
                Debug.Log("[A11Y] Backspace pressed");
                lastInputTime = Time.time;
                VirtualNavigator.Deactivate();
                NVDA.Speak("Menu closed");
            }
        }
    }

    //==========================================================================
    // Mod Entry Point - initializes Harmony patches
    //==========================================================================
    public class Mod : UserMod2
    {
        private static bool patched = false;

        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            ApplyPatches(harmony);
        }

        public static void ApplyPatches(Harmony harmony)
        {
            if (patched) return;
            patched = true;
            Debug.Log("[A11Y] ONI Access loading...");
            harmony.PatchAll();
            Debug.Log("[A11Y] Patches applied");
        }
    }

    //==========================================================================
    // Harmony Patches
    //==========================================================================
    [HarmonyPatch(typeof(MainMenu), "OnSpawn")]
    public class MainMenuPatch
    {
        static void Postfix()
        {
            Debug.Log("[A11Y] MainMenu detected - creating input handler");

            // Create input handler if needed
            if (A11YInputHandler.Instance == null)
            {
                var go = new GameObject("A11YInput");
                go.AddComponent<A11YInputHandler>();
                Debug.Log("[A11Y] Input handler GameObject created");
            }
            else
            {
                Debug.Log("[A11Y] Input handler already exists");
            }

            // Activate virtual main menu
            VirtualNavigator.ActivateMainMenu();
        }
    }
}
