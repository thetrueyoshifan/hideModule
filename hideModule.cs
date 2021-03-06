using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRCModLoader;
using VRCTools;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

namespace hideModule
{
    [VRCModInfo("hideModule", "1.0.1", "Emilia")]
    class main : VRCMod
    {
        bool hidingMods = false;

        private static FieldInfo currentPageGetter;
        private static FieldInfo quickmenuContextualDisplayGetter;

        private static float moddedInfoBarSize = 0;
        private static float moddedInfoBarOffset = 0;

        Type emmVRCAPI;


        bool avatarFav = false;
        bool emmVRC = false;

        Button.ButtonClickedEvent vrcmlSettingsButtonAction = null;
        void OnApplicationStart()
        {
            // Check if the game started with "--hidemods", and if so, block mods to start with
            if (Environment.CommandLine.Contains("--hidemods")) hidingMods = true;
            if (hidingMods) VRCModLogger.Log("[hideModule] The client was started with mods hidden. Press CTRL+R at any time to show supported mod menus.");

            // Mod detection to add compatibility, while also avoiding breaking things if these mods are not present
            if (ModManager.Mods.Find(x => x.Name == "AvatarFav") != null)
                avatarFav = true;
            // Using reflection to access emmVRC
            emmVRCAPI = null;
            emmVRC = AppDomain.CurrentDomain.GetAssemblies().Any(a =>
            {
                emmVRCAPI = a.GetType("emmVRC.emmVRC");
                return emmVRCAPI != null;
            });
        }
        void OnLevelWasInitialized(int level)
        {
            if (level != 1) return;
            // Grab the current button action for the VRCML Settings button, so we can set it back whenever we need
            vrcmlSettingsButtonAction = QMStuff.GetQuickMenuInstance().transform.Find("ShortcutMenu/SettingsButton").GetComponentInChildren<Button>().onClick;
            // If mods are already supposed to be hidden at this point, make it so
            if (hidingMods)
                hideMods();
        }
        void OnUpdate()
        {
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.R))
            {
                if (hidingMods)
                {
                    hidingMods = false;
                    showMods();
                }
                else if (!hidingMods)
                {
                    hidingMods = true;
                    hideMods();
                }
            }
        }
        void hideMods()
        {
            try
            {
                // Disables the VRCModNetwork Status text in the Quick Menu
                QMStuff.GetQuickMenuInstance().transform.Find("ShortcutMenu/VRCModNetworkStatusText").gameObject.SetActive(false);

                // Clears the current listeners for the Settings button, in order to add one mimmicking the vanilla VRChat Settings button
                QMStuff.GetQuickMenuInstance().transform.Find("ShortcutMenu/SettingsButton").GetComponentInChildren<Button>().onClick = new Button.ButtonClickedEvent();
                // Actually add the mimmicking action
                QMStuff.GetQuickMenuInstance().transform.Find("ShortcutMenu/SettingsButton").GetComponentInChildren<Button>().onClick.AddListener(delegate ()
                {
                    QMStuff.GetQuickMenuInstance().MainMenu(3);
                });
                // Update the button's text and tooltip
                QMStuff.GetQuickMenuInstance().transform.Find("ShortcutMenu/SettingsButton").GetComponentInChildren<Text>().text = "Settings";
                QMStuff.GetQuickMenuInstance().transform.Find("ShortcutMenu/SettingsButton").GetComponent<UiTooltip>().text = "Tune Control, Audio and Video Settings. Log Out or Quit.";

                // Attempts to adjust the info bar background to the vanilla position. This function changes, depending on if emmVRC is installed, so a switch case is used here.
                Transform infobarpanelTransform = QMStuff.GetQuickMenuInstance().transform.Find("QuickMenu_NewElements/_InfoBar/Panel");
                RectTransform infobarpanelRectTransform = infobarpanelTransform.GetComponent<RectTransform>();
                if (infobarpanelRectTransform.sizeDelta.y != 0 || infobarpanelRectTransform.anchoredPosition.y != 0)
                {
                    moddedInfoBarSize = infobarpanelRectTransform.sizeDelta.y;
                    moddedInfoBarOffset = infobarpanelRectTransform.anchoredPosition.y;
                    infobarpanelRectTransform.sizeDelta = new Vector2(infobarpanelRectTransform.sizeDelta.x, 0);
                    infobarpanelRectTransform.anchoredPosition = new Vector2(infobarpanelRectTransform.anchoredPosition.x, 0);
                }
            }
            catch (Exception ex)
            {
                VRCModLogger.Log("[hideModule] An error occured while hiding mods: " + ex.ToString());
            }

            // Now for the support modules. At the moment, only emmVRC is supported by this. It has a function built in to do everything automatically, so we just need to call it.
            if (emmVRC)
            {
                emmVRCAPI.GetMethod("hideEmmVRC").Invoke(null, null);
            }
            if (avatarFav)
            {

                VRCUiManagerUtils.OnPageShown += (page) =>
                {
                    if (page.GetType() == typeof(VRC.UI.PageAvatar) && hidingMods)
                    {
                        ModManager.StartCoroutine(hideAvatarFav());
                    }
                };
            }

        }
        static IEnumerator hideAvatarFav()
        {
            yield return new WaitForSeconds(0.25f);
            UiInputField searchBar = VRCUiManagerUtils.GetVRCUiManager().GetComponentInChildren<VRCUiPageHeader>(true).searchBar;
            if (searchBar != null)
            {
                searchBar.editButton.interactable = false;
            }
            GameObject.Find("UserInterface/MenuContent/Screens/Avatar/Vertical Scroll View/Viewport/Content/Favorite Avatar List(Clone)").SetActive(false);
            GameObject.Find("UserInterface/MenuContent/Screens/Avatar/ToggleFavorite").transform.localScale = new Vector3(0, 0, 0);
        }
        void showMods()
        {
            try
            {
                // Enables the VRCModNetwork Status text in the Quick Menu
                QMStuff.GetQuickMenuInstance().transform.Find("ShortcutMenu/VRCModNetworkStatusText").gameObject.SetActive(true);

                // Resets the listener for the Settings button back to VRCML's action
                QMStuff.GetQuickMenuInstance().transform.Find("ShortcutMenu/SettingsButton").GetComponentInChildren<Button>().onClick = new Button.ButtonClickedEvent();
                QMStuff.GetQuickMenuInstance().transform.Find("ShortcutMenu/SettingsButton").GetComponentInChildren<Button>().onClick.AddListener(delegate () { ShowQuickmenuPage("SettingsMenu"); });

                // Update the button's text and tooltip
                QMStuff.GetQuickMenuInstance().transform.Find("ShortcutMenu/SettingsButton").GetComponentInChildren<Text>().text = "Mod/Game\nSettings";
                QMStuff.GetQuickMenuInstance().transform.Find("ShortcutMenu/SettingsButton").GetComponent<UiTooltip>().text = "Tune Control, Audio, Video and Mod Settings. Log Out or Quit.";

                // Attempts to adjust the info bar background to the modded position. This function changes, depending on if emmVRC is installed, so a switch case is used here.
                Transform infobarpanelTransform = QMStuff.GetQuickMenuInstance().transform.Find("QuickMenu_NewElements/_InfoBar/Panel");
                RectTransform infobarpanelRectTransform = infobarpanelTransform.GetComponent<RectTransform>();
                if (moddedInfoBarSize != 0 || moddedInfoBarOffset != 0)
                {
                    infobarpanelRectTransform.sizeDelta = new Vector2(infobarpanelRectTransform.sizeDelta.x, moddedInfoBarSize);
                    infobarpanelRectTransform.anchoredPosition = new Vector2(infobarpanelRectTransform.anchoredPosition.x, moddedInfoBarOffset);
                    moddedInfoBarSize = 0;
                    moddedInfoBarOffset = 0;
                }
            }
            catch (Exception ex)
            {
                VRCModLogger.Log("[hideModule] An error occured while showing mods: " + ex.ToString());
            }
            // Now for the support modules. At the moment, only emmVRC is supported by this. It has a function built in to do everything automatically, so we just need to call it.
            // TODO: Fix emmVRC support!
            if (emmVRC)
            {
                emmVRCAPI.GetMethod("showEmmVRC").Invoke(null, null);
            }
            if (avatarFav)
            {
                GameObject.Find("UserInterface/MenuContent/Screens/Avatar/ToggleFavorite").transform.localScale = new Vector3(1, 1, 1);
            }
        }

        // VRCTools ShowQuickMenuPage function
        internal static void ShowQuickmenuPage(string pagename)
        {
            QuickMenu quickmenu = QMStuff.GetQuickMenuInstance();
            Transform pageTransform = quickmenu?.transform.Find(pagename);
            if (pageTransform == null)
            {
                VRCModLogger.LogError("[QMStuff] pageTransform is null !");
            }

            if (currentPageGetter == null)
            {
                GameObject shortcutMenu = quickmenu.transform.Find("ShortcutMenu").gameObject;
                FieldInfo[] fis = typeof(QuickMenu).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where((fi) => fi.FieldType == typeof(GameObject)).ToArray();
                VRCModLogger.Log("[QMStuff] GameObject Fields in QuickMenu:");
                int count = 0;
                foreach (FieldInfo fi in fis)
                {
                    GameObject value = fi.GetValue(quickmenu) as GameObject;
                    if (value == shortcutMenu && ++count == 2)
                    {
                        VRCModLogger.Log("[QMStuff] currentPage field: " + fi.Name);
                        currentPageGetter = fi;
                        break;
                    }
                }
                if (currentPageGetter == null)
                {
                    VRCModLogger.LogError("[QMStuff] Unable to find field currentPage in QuickMenu");
                    return;
                }
            }

                    ((GameObject)currentPageGetter.GetValue(quickmenu))?.SetActive(false);
            QMStuff.GetQuickMenuInstance().transform.Find("QuickMenu_NewElements/_InfoBar").gameObject.SetActive(false);

            if (quickmenuContextualDisplayGetter != null)
                quickmenuContextualDisplayGetter = typeof(QuickMenu).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault((fi) => fi.FieldType == typeof(QuickMenuContextualDisplay));
            QuickMenuContextualDisplay quickmenuContextualDisplay = quickmenuContextualDisplayGetter?.GetValue(quickmenu) as QuickMenuContextualDisplay;
            if (quickmenuContextualDisplay != null)
            {
                currentPageGetter.SetValue(quickmenu, pageTransform.gameObject);
                typeof(QuickMenuContextualDisplay).GetMethod("SetDefaultContext", BindingFlags.Public | BindingFlags.Instance).Invoke(quickmenuContextualDisplay, new object[] { 0, null, null }); // This is the only way to pass the unknown enum type value
            }

            currentPageGetter.SetValue(quickmenu, pageTransform.gameObject);
            typeof(QuickMenu).GetMethod("SetContext", BindingFlags.Public | BindingFlags.Instance).Invoke(quickmenu, new object[] { 1, null, null }); // This is the only way to pass the unknown enum type value
            pageTransform.gameObject.SetActive(true);
        }
    }
}
