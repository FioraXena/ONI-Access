================================================================================
                    ONI ACCESSIBILITY MOD - README
                    Screen Reader Support for Oxygen Not Included
================================================================================

DESCRIPTION
-----------
This mod makes Oxygen Not Included fully playable for blind players using the
NVDA screen reader. It provides comprehensive keyboard navigation, automatic
speech announcements, and dual-voice support for alerts.


REQUIREMENTS
------------
- Oxygen Not Included (Steam version)
- NVDA Screen Reader (recommended) - https://www.nvaccess.org/
- Windows 10/11 with SAPI voices (fallback)


INSTALLATION
------------
1. Copy the mod folder to:
   Documents\Klei\OxygenNotIncluded\mods\Local\ONIAccessibilityMod

2. Copy nvdaControllerClient64.dll to the mod folder

3. Enable the mod in the game's Mod Manager

4. Restart the game


KEY BINDINGS
------------

GLOBAL CONTROLS:
  F8              - Toggle accessibility mod on/off
  /  (Slash)      - Repeat last SAPI alert

MENU NAVIGATION:
  Arrow Keys      - Navigate between menu items
  Enter/Space     - Activate selected item
  Tab             - Next item
  Shift+Tab       - Previous item
  Escape          - Go back

CHARACTER SELECTION:
  Left/Right      - Navigate between Duplicant pods
  R               - Reroll current Duplicant

WORLD SELECTION:
  S               - Shuffle world seed

IN-GAME (Duplicant Mode OFF):
  1               - Colony vitals
  2               - Active alerts
  3               - Environment info (gas, temperature)
  R               - Resources summary
  T               - Research status
  Y               - Cycle info
  K               - Inspect object at screen center

IN-GAME (Duplicant Mode ON - Press E to toggle):
  E               - Toggle Duplicant Mode
  1-9             - Select Duplicant by number
  Z               - Report selected Duplicant stats
  X               - Report selected Duplicant health
  C               - Report selected Duplicant current task


SPEECH SYSTEM
-------------
The mod uses two speech channels:

1. NVDA (Primary) - Used for focus announcements and user actions
   - Requires nvdaControllerClient64.dll in the mod folder
   - Falls back to SAPI if NVDA is not running

2. SAPI (Secondary) - Used for background alerts
   - Does not interrupt NVDA focus speech
   - Announces notifications, cycle changes, biome changes
   - Press / to repeat the last SAPI announcement


CONFIGURATION
-------------
Settings are saved to accessibility_config.json in the mod folder.
Current options:
- IsEnabled: Master toggle (also F8)
- UseSAPIFallback: Use Windows speech if NVDA unavailable
- AnnounceBiomeChanges: Announce when entering new biomes
- AnnounceNotifications: Speak game notifications
- AnnounceCycleChanges: Announce new cycles
- SAPISpeechRate: SAPI voice speed (0-10)
- InputCooldown: Delay between inputs (seconds)


BUILDING FROM SOURCE
--------------------
1. Open ONIAccessibilityMod.csproj in Visual Studio
2. Update ONIDirectory path to your game installation
3. Build the solution
4. The DLL will be copied to your local mods folder


TROUBLESHOOTING
---------------
- "NVDA not detected" warning: Ensure NVDA is running before starting the game
- No speech at all: Check that System.Speech.dll is accessible
- Keys not working: Press F8 to ensure mod is enabled
- Mod not loading: Check the Player.log for errors


CREDITS
-------
Developed for accessibility in gaming.
Uses Harmony2 for runtime patching.


VERSION
-------
1.0.0 - Initial release

================================================================================
