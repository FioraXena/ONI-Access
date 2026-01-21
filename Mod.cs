using System.Runtime.InteropServices;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using KMod;

namespace ONIAccessibilityMod
{
    //==========================================================================
    // ONI ACCESS - Main Menu and New Game Navigation
    //==========================================================================

    // NVDA Bridge
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
                    Debug.LogWarning("[A11Y] NVDA error: " + result);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[A11Y] NVDA exception: " + ex.Message);
            }
        }
    }

    // Game States
    public enum GameState
    {
        None,
        MainMenu,
        SubMenu,
        NewGame,
        Loading,
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
        public static string CurrentMenuName = "";

        public static void ActivateMainMenu()
        {
            Debug.Log("[A11Y] Activating Main Menu");
            CurrentState = GameState.MainMenu;
            CurrentMenuName = "Main Menu";
            CurrentMenu.Clear();

            CurrentMenu.Add(new VirtualMenuItem("Resume Game", "Resuming Game", () => ClickButton("ResumeGame")));
            CurrentMenu.Add(new VirtualMenuItem("New Game", "Starting New Game", () => ClickButton("NewGame")));
            CurrentMenu.Add(new VirtualMenuItem("Load Game", "Opening Load Game", () => ClickButton("LoadGame")));
            CurrentMenu.Add(new VirtualMenuItem("Colony Summaries", "Opening Colony Summaries", () => ClickButton("ColonySummaries")));
            CurrentMenu.Add(new VirtualMenuItem("Supply Closet", "Opening Supply Closet", () => ClickButton("SupplyCloset")));
            CurrentMenu.Add(new VirtualMenuItem("Mods", "Opening Mods", () => { CurrentState = GameState.SubMenu; ClickButton("Mods"); }));
            CurrentMenu.Add(new VirtualMenuItem("Options", "Opening Options", () => { CurrentState = GameState.SubMenu; ClickButton("Options"); }));
            CurrentMenu.Add(new VirtualMenuItem("Quit to Desktop", "Quitting Game", () => ClickButton("QuitToDesktop")));

            CurrentIndex = 0;
            MenuActive = true;
            NVDA.Speak("Main Menu. " + CurrentMenu[CurrentIndex].Name);
        }

        public static void ActivateNewGameMenu()
        {
            Debug.Log("[A11Y] Activating New Game Menu");
            CurrentState = GameState.NewGame;
            CurrentMenuName = "New Game";
            CurrentMenu.Clear();

            // New Game menu items - these will need button name verification
            CurrentMenu.Add(new VirtualMenuItem("Survival", "Selecting Survival", () => ClickButton("Survival")));
            CurrentMenu.Add(new VirtualMenuItem("No Sweat", "Selecting No Sweat", () => ClickButton("NoSweat")));
            CurrentMenu.Add(new VirtualMenuItem("Custom Game", "Selecting Custom Game", () => ClickButton("CustomGame")));
            CurrentMenu.Add(new VirtualMenuItem("Back", "Going Back", () => GoBack()));

            CurrentIndex = 0;
            MenuActive = true;
            NVDA.Speak("New Game. " + CurrentMenu[CurrentIndex].Name);
        }

        public static void NavigateDown()
        {
            if (!MenuActive || CurrentMenu.Count == 0) return;
            CurrentIndex = (CurrentIndex + 1) % CurrentMenu.Count;
            Debug.Log("[A11Y] Down - Index: " + CurrentIndex);
            AnnounceCurrentItem();
        }

        public static void NavigateUp()
        {
            if (!MenuActive || CurrentMenu.Count == 0) return;
            CurrentIndex = (CurrentIndex - 1 + CurrentMenu.Count) % CurrentMenu.Count;
            Debug.Log("[A11Y] Up - Index: " + CurrentIndex);
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
                NVDA.Speak(CurrentMenu[CurrentIndex].Name);
            }
        }

        public static void GoBack()
        {
            Debug.Log("[A11Y] Going back from state: " + CurrentState);

            // Try to click common back/close buttons
            string[] backButtons = new string[]
            {
                "CloseButton", "Close", "BackButton", "Back",
                "CancelButton", "Cancel", "DoneButton", "Done",
                "Button_Close", "Button_Back", "Button_Cancel"
            };

            foreach (string btnName in backButtons)
            {
                if (TryClickButton(btnName))
                {
                    NVDA.Speak("Going back");
                    return;
                }
            }

            // If no back button found, try Escape key simulation
            Debug.Log("[A11Y] No back button found, simulating Escape");
            NVDA.Speak("Going back");
        }

        public static void Deactivate()
        {
            Debug.Log("[A11Y] Deactivating menu");
            MenuActive = false;
            CurrentMenu.Clear();
        }

        public static void SetState(GameState state)
        {
            CurrentState = state;
            Debug.Log("[A11Y] State: " + state);
        }

        public static void ClickButton(string buttonName)
        {
            if (!TryClickButton(buttonName))
            {
                Debug.LogWarning("[A11Y] Button not found: " + buttonName);
                LogAllButtons();
            }
        }

        public static bool TryClickButton(string buttonName)
        {
            string[] namesToTry = new string[]
            {
                buttonName,
                "Button_" + buttonName,
                buttonName + "Button"
            };

            foreach (var btn in Object.FindObjectsOfType<KButton>())
            {
                if (btn == null || !btn.gameObject.activeInHierarchy) continue;
                string objName = btn.gameObject.name;

                foreach (string nameToTry in namesToTry)
                {
                    if (objName.Contains(nameToTry))
                    {
                        Debug.Log("[A11Y] Clicking: " + objName);
                        btn.SignalClick(KKeyCode.Mouse0);
                        return true;
                    }
                }
            }
            return false;
        }

        public static void LogAllButtons()
        {
            Debug.Log("[A11Y] Active buttons:");
            foreach (var btn in Object.FindObjectsOfType<KButton>())
            {
                if (btn != null && btn.gameObject.activeInHierarchy)
                {
                    Debug.Log("[A11Y]   - " + btn.gameObject.name);
                }
            }
        }
    }

    //==========================================================================
    // Input Handler
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
            if (Time.unscaledTime - lastInputTime < INPUT_DELAY) return;

            // Handle backspace for going back (works even when menu not active)
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                Debug.Log("[A11Y] Backspace pressed, state: " + VirtualNavigator.CurrentState);
                lastInputTime = Time.unscaledTime;

                if (VirtualNavigator.CurrentState == GameState.SubMenu)
                {
                    VirtualNavigator.GoBack();
                    // Re-activate main menu after a delay
                    StartCoroutine(ReactivateMainMenuAfterDelay());
                }
                else if (VirtualNavigator.MenuActive)
                {
                    VirtualNavigator.Deactivate();
                    NVDA.Speak("Menu closed");
                }
                return;
            }

            // Other keys only work when menu is active
            if (!VirtualNavigator.MenuActive) return;

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                lastInputTime = Time.unscaledTime;
                VirtualNavigator.NavigateDown();
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                lastInputTime = Time.unscaledTime;
                VirtualNavigator.NavigateUp();
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                lastInputTime = Time.unscaledTime;
                VirtualNavigator.ExecuteCurrent();
            }
        }

        System.Collections.IEnumerator ReactivateMainMenuAfterDelay()
        {
            yield return new WaitForSeconds(0.5f);
            if (VirtualNavigator.CurrentState != GameState.InGame)
            {
                VirtualNavigator.ActivateMainMenu();
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
            Debug.Log("[A11Y] ONI Access Mod Loading...");
            harmony.PatchAll();
            Debug.Log("[A11Y] Patches applied");
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
            Debug.Log("[A11Y] MainMenu detected");

            if (A11YInputHandler.Instance == null)
            {
                var go = new GameObject("A11YInputHandler");
                go.AddComponent<A11YInputHandler>();
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
    // New Game Screen Detection
    //==========================================================================
    [HarmonyPatch(typeof(NewGameFlow), "OnSpawn")]
    public class NewGameFlowPatch
    {
        static void Postfix()
        {
            Debug.Log("[A11Y] NewGameFlow detected");
            // Delay to let UI initialize
            if (A11YInputHandler.Instance != null)
            {
                A11YInputHandler.Instance.StartCoroutine(ActivateNewGameAfterDelay());
            }
        }

        static System.Collections.IEnumerator ActivateNewGameAfterDelay()
        {
            yield return new WaitForSeconds(0.5f);
            VirtualNavigator.ActivateNewGameMenu();
        }
    }

    //==========================================================================
    // Game Load Complete
    //==========================================================================
    [HarmonyPatch(typeof(Game), "OnSpawn")]
    public class GameSpawnPatch
    {
        static void Postfix()
        {
            Debug.Log("[A11Y] Colony loaded");
            VirtualNavigator.SetState(GameState.InGame);
            VirtualNavigator.Deactivate();
            NVDA.Speak("Colony Loaded");
        }
    }
}
