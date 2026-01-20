using System.Runtime.InteropServices;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using KMod;

namespace ONIAccessibilityMod
{
    //==========================================================================
    // ONI ACCESS - Phase 1: Main Menu
    // Uses Harmony to intercept ONI's input system directly
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
                nvdaController_speakText(text);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[A11Y] NVDA speak failed: " + ex.Message);
            }
        }
    }

    // State Engine - tracks current game state
    public enum GameState
    {
        None,
        MainMenu,
        Loading,
        ColonySetup,
        InGame,
        Paused
    }

    // Virtual Menu Item with custom activation phrase
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
            Debug.Log("[A11Y] Activating Main Menu navigation");
            CurrentState = GameState.MainMenu;
            CurrentMenu.Clear();

            // Virtual menu items with natural activation phrases
            CurrentMenu.Add(new VirtualMenuItem("Resume Game", "Resuming Game", () => ClickButton("ResumeGame")));
            CurrentMenu.Add(new VirtualMenuItem("New Game", "Starting New Game", () => ClickButton("NewGame")));
            CurrentMenu.Add(new VirtualMenuItem("Load Game", "Opening Load Game", () => ClickButton("LoadGame")));
            CurrentMenu.Add(new VirtualMenuItem("Options", "Opening Options", () => ClickButton("Options")));
            CurrentMenu.Add(new VirtualMenuItem("Mods", "Opening Mods", () => ClickButton("Mods")));
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
            Debug.Log("[A11Y] Navigate Down - Index: " + CurrentIndex + " of " + CurrentMenu.Count);
            AnnounceCurrentItem();
        }

        public static void NavigateUp()
        {
            if (!MenuActive || CurrentMenu.Count == 0) return;
            CurrentIndex = (CurrentIndex - 1 + CurrentMenu.Count) % CurrentMenu.Count;
            Debug.Log("[A11Y] Navigate Up - Index: " + CurrentIndex + " of " + CurrentMenu.Count);
            AnnounceCurrentItem();
        }

        public static void ExecuteCurrent()
        {
            if (!MenuActive || CurrentIndex < 0 || CurrentIndex >= CurrentMenu.Count) return;
            var item = CurrentMenu[CurrentIndex];
            Debug.Log("[A11Y] Executing: " + item.Name);

            // Use custom activation phrase
            NVDA.Speak(item.ActivationPhrase);

            // Deactivate menu before executing to avoid interference
            MenuActive = false;

            if (item.Execute != null) item.Execute();
        }

        public static void AnnounceCurrentItem()
        {
            if (CurrentIndex >= 0 && CurrentIndex < CurrentMenu.Count)
            {
                string announcement = CurrentMenu[CurrentIndex].Name;
                Debug.Log("[A11Y] Announcing: " + announcement);
                NVDA.Speak(announcement);
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

            // Try multiple naming conventions
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
                        Debug.Log("[A11Y] Found and clicking button: " + objName);
                        btn.SignalClick(KKeyCode.Mouse0);
                        return;
                    }
                }
            }

            Debug.LogWarning("[A11Y] Button not found: " + buttonName);

            // Log all available buttons for debugging
            Debug.Log("[A11Y] Available buttons:");
            foreach (var btn in Object.FindObjectsOfType<KButton>())
            {
                if (btn != null)
                {
                    Debug.Log("[A11Y]   - " + btn.gameObject.name);
                }
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
    // Input Interceptor - Harmony patch on KInputController
    // This intercepts input BEFORE ONI processes it
    //==========================================================================
    [HarmonyPatch(typeof(KInputController), nameof(KInputController.QueueButtonEvent))]
    public class InputInterceptorPatch
    {
        static bool Prefix(KInputController.KeyDef key_def, bool is_down)
        {
            // Only intercept key down events
            if (!is_down) return true;

            KKeyCode keyCode = key_def.mKeyCode;

            // Handle Space during loading
            if (VirtualNavigator.CurrentState == GameState.Loading && keyCode == KKeyCode.Space)
            {
                Debug.Log("[A11Y] Space pressed during loading");
                NVDA.Speak("Still loading");
                return false;
            }

            // Only intercept navigation when menu is active
            if (!VirtualNavigator.MenuActive) return true;

            // Handle navigation keys
            if (keyCode == KKeyCode.DownArrow)
            {
                Debug.Log("[A11Y] Intercepted: Down Arrow");
                VirtualNavigator.NavigateDown();
                return false; // Block game from processing
            }
            else if (keyCode == KKeyCode.UpArrow)
            {
                Debug.Log("[A11Y] Intercepted: Up Arrow");
                VirtualNavigator.NavigateUp();
                return false;
            }
            else if (keyCode == KKeyCode.Return || keyCode == KKeyCode.KeypadEnter)
            {
                Debug.Log("[A11Y] Intercepted: Enter");
                VirtualNavigator.ExecuteCurrent();
                return false;
            }
            else if (keyCode == KKeyCode.Backspace)
            {
                // Backspace goes back in menus
                Debug.Log("[A11Y] Intercepted: Backspace");
                VirtualNavigator.Deactivate();
                NVDA.Speak("Menu closed");
                return false;
            }
            // Note: Escape is NOT intercepted - it should open pause menu in-game

            return true; // Let other keys pass through
        }
    }

    //==========================================================================
    // Main Menu Detection
    //==========================================================================
    [HarmonyPatch(typeof(MainMenu), "OnSpawn")]
    public class MainMenuPatch
    {
        static void Postfix(MainMenu __instance)
        {
            Debug.Log("[A11Y] ========================================");
            Debug.Log("[A11Y] MainMenu.OnSpawn detected!");
            Debug.Log("[A11Y] ========================================");

            // Small delay to let menu fully initialize
            __instance.StartCoroutine(ActivateAfterDelay());
        }

        static System.Collections.IEnumerator ActivateAfterDelay()
        {
            yield return new WaitForSeconds(0.5f);
            VirtualNavigator.ActivateMainMenu();
        }
    }

    //==========================================================================
    // Loading Screen Detection
    //==========================================================================
    [HarmonyPatch(typeof(LoadingOverlay), "OnSpawn")]
    public class LoadingOverlayPatch
    {
        static void Postfix()
        {
            Debug.Log("[A11Y] Loading screen started");
            VirtualNavigator.SetState(GameState.Loading);
            NVDA.Speak("Loading");
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
            Debug.Log("[A11Y] Game.OnSpawn - Colony loaded");
            VirtualNavigator.SetState(GameState.InGame);
            NVDA.Speak("Colony Loaded");
        }
    }
}
