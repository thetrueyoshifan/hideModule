using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace hideModule
{
    public static class QMStuff
    {
        private static BoxCollider QuickMenuBackGroundRefrence;
        public static BoxCollider QuickMenuBackGround()
        {
            if (QuickMenuBackGroundRefrence == null)
            {
                QuickMenuBackGroundRefrence = GetQuickMenuInstance().GetComponent<BoxCollider>();
            }
            return QuickMenuBackGroundRefrence;
        }


        private static GameObject SingleButtonRefrence;
        public static GameObject SingleButtonTemplate()
        {
            if (SingleButtonRefrence == null)
            {
                SingleButtonRefrence = QMStuff.GetQuickMenuInstance().transform.Find("ShortcutMenu/WorldsButton").gameObject;
            }
            return SingleButtonRefrence;
        }

        private static GameObject ToggleButtonRefrence;
        public static GameObject ToggleButtonTemplate()
        {
            if (ToggleButtonRefrence == null)
            {
                ToggleButtonRefrence = QMStuff.GetQuickMenuInstance().transform.Find("UserInteractMenu/BlockButton").gameObject;
            }
            return ToggleButtonRefrence;
        }

        private static Transform NestedButtonRefrence;
        public static Transform NestedMenuTemplate()
        {
            if (NestedButtonRefrence == null)
            {
                NestedButtonRefrence = QMStuff.GetQuickMenuInstance().transform.Find("CameraMenu");
            }
            return NestedButtonRefrence;
        }

        // <3 VRCTools
        private static VRCUiManager vrcuimInstance;
        public static VRCUiManager GetVRCUiMInstance()
        {
            if (vrcuimInstance == null)
            {
                MethodInfo method = typeof(VRCUiManager).GetMethod("get_Instance", BindingFlags.Static | BindingFlags.Public);
                if (method == null)
                {
                    return null;
                }
                vrcuimInstance = (VRCUiManager)method.Invoke(null, new object[0]);
            }
            return vrcuimInstance;
        }

        private static QuickMenu quickmenuInstance;
        private static FieldInfo currentPageGetter;
        private static FieldInfo quickmenuContextualDisplayGetter;

        public static QuickMenu GetQuickMenuInstance()
        {
            if (quickmenuInstance == null)
            {
                MethodInfo quickMenuInstanceGetter = typeof(QuickMenu).GetMethod("get_Instance", BindingFlags.Public | BindingFlags.Static);
                if (quickMenuInstanceGetter == null)
                    return null;
                quickmenuInstance = ((QuickMenu)quickMenuInstanceGetter.Invoke(null, new object[] { }));
            }
            return quickmenuInstance;
        }

        //Copied from QuickMenu
        public static IEnumerator PlaceUiAfterPause()
        {
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            GetVRCUiMInstance().PlaceUi();
            GameObject.Find("UserInterface/MenuContent/Backdrop/Header").gameObject.SetActive(false);
            yield break;
        }

        //Partial reproduction of SetMenuIndex from QuickMenu
        public static void ShowQuickmenuPage(string pagename)
        {
            QuickMenu quickmenu = GetQuickMenuInstance();
            Transform pageTransform = quickmenu?.transform.Find(pagename);
            if (pageTransform == null)
            {
                Console.WriteLine("[QuickMenuUtils] pageTransform is null !");
            }

            if (currentPageGetter == null)
            {
                GameObject shortcutMenu = quickmenu.transform.Find("ShortcutMenu").gameObject;
                if (!shortcutMenu.activeInHierarchy)
                    shortcutMenu = quickmenu.transform.Find("UserInteractMenu").gameObject;

                FieldInfo[] fis = typeof(QuickMenu).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where((fi) => fi.FieldType == typeof(GameObject)).ToArray();
                //emmVRCLoader.Logger.Log("[QuickMenuUtils] GameObject Fields in QuickMenu:");
                int count = 0;
                foreach (FieldInfo fi in fis)
                {
                    GameObject value = fi.GetValue(quickmenu) as GameObject;
                    if (value == shortcutMenu && ++count == 2)
                    {
                        //emmVRCLoader.Logger.Log("[QuickMenuUtils] currentPage field: " + fi.Name);
                        currentPageGetter = fi;
                        break;
                    }
                }
                if (currentPageGetter == null)
                {
                    Console.WriteLine("[QuickMenuUtils] Unable to find field currentPage in QuickMenu");
                    return;
                }
            }

            ((GameObject)currentPageGetter.GetValue(quickmenu))?.SetActive(false);

            GameObject infoBar = GetQuickMenuInstance().transform.Find("QuickMenu_NewElements/_InfoBar").gameObject;
            infoBar.SetActive(pagename == "ShortcutMenu");

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

            if (pagename == "ShortcutMenu")
            {
                SetIndex(0);
            }
            else if (pagename == "UserInteractMenu")
            {
                SetIndex(3);
            }
            else
            {
                SetIndex(-1);
            }
        }

        private static FieldInfo _CurrentIndex = null;
        public static void SetIndex(int index)
        {
            if (_CurrentIndex == null)
            {
                _CurrentIndex = typeof(QuickMenu).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                                                 .First(x => !x.IsStatic && (x.FieldType.Name == typeof(int).Name));
                if (_CurrentIndex == null)
                {
                    Console.WriteLine("[QuickMenuUtils] Index reflection is null");
                    return;
                }
            }

            _CurrentIndex.SetValue(GetQuickMenuInstance(), index);
        }
    }
}
