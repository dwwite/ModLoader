using NeoModLoader.General;
using NeoModLoader.utils;
using UnityEngine;

namespace NeoModLoader.ui;

internal static class UIManager
{
    public static void init()
    {
        InformationWindow.CreateWindow("Information", "Information Title");
        NewModListWindow.CreateAndInit("NeoModList");
        ExternalModHotLoadWindow.CreateAndInit("ExternalMods", new Vector2(650, 320));
        WorkshopModListWindow.CreateAndInit("WorkshopMods");
        ModUploadWindow.CreateAndInit("ModUpload");
        ModUploadingProgressWindow.CreateAndInit("ModUploadingProgress");
        ModUploadAuthenticationWindow.CreateAndInit("ModUploadAuthentication");
        ModConfigureWindow.CreateAndInit("ModConfigure");
        PowerButtonCreator.AddButtonToTab(
            PowerButtonCreator.CreateWindowButton("NML_ModsList", "NeoModList",
                                                  InternalResourcesGetter.GetIcon()),
            PowerButtonCreator.GetTab(PowerTabNames.Main),
          22);
    }
}
