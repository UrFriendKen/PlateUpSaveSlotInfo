using HarmonyLib;
using Kitchen;
using UnityEngine;

namespace KitchenSaveSlotInfo.Patches
{
    [HarmonyPatch]
    static class LocalViewRouter_Patch
    {
        [HarmonyPatch(typeof(LocalViewRouter), "GetPrefab")]
        [HarmonyPostfix]
        static void GetPrefab_Postfix(ViewType view_type, ref GameObject __result)
        {
            if (__result == null)
                return;

            if (view_type == ViewType.GenericIndicator &&
                __result.GetComponentInChildren<SaveSlotInfoView>() == null)
            {
                int uiLayer = LayerMask.NameToLayer("UI");

                GameObject gO = new GameObject("Save Slot Info");
                gO.layer = uiLayer;
                gO.transform.SetParent(__result.transform);
                gO.transform.Reset();

                GameObject container = new GameObject("Container");
                container.layer = uiLayer;
                container.transform.SetParent(gO.transform);
                container.transform.Reset();

                GameObject anchor = new GameObject("Anchor");
                anchor.layer = uiLayer;
                anchor.transform.SetParent(gO.transform);
                anchor.transform.Reset();
                anchor.transform.localPosition = new Vector3(3f, 0f, 0f);

                SaveSlotInfoView saveSlotInfoView = gO.AddComponent<SaveSlotInfoView>();
                saveSlotInfoView.Anchor = anchor.transform;
                saveSlotInfoView.Container = container.transform;
            }
        }
    }
}
