using System;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RagnavikCompat;

// Both upstream transpilers replace the same GetComponent call. Repair their
// resolvers instead, so either transpiler order keeps both cart layouts working.
internal static class CartSignCompatibility
{
    internal const string CartsGuid = "Azumatt.CraftyCarts";
    internal const string BumperGuid = "Azumatt.BoardersBumperBlurbs";
    private static Harmony? patches;
    private static Type? cartType, bumperType;

    internal static void Enable(Harmony owner, ManualLogSource log)
    {
        if (!Chainloader.PluginInfos.TryGetValue(CartsGuid, out var carts) ||
            !Chainloader.PluginInfos.TryGetValue(BumperGuid, out var bumpers) ||
            carts.Metadata.Version != new System.Version(3, 2, 3) ||
            bumpers.Metadata.Version != new System.Version(1, 0, 3))
        {
            log.LogInfo("Cart sign bridge skipped: requires CraftyCarts 3.2.3 and BoardersBumperBlurbs 1.0.3.");
            return;
        }
        patches = new Harmony(owner.Id + ".cartsigns");
        try
        {
            var ca = carts.Instance.GetType().Assembly;
            var ba = bumpers.Instance.GetType().Assembly;
            cartType = ca.GetType("CraftyCartsRemake.CraftyCart");
            bumperType = ba.GetType("BoardersBumperBlurbs.Game.BumperSticker");
            var cartResolver = Find(ca, "CraftyCartsRemake.Sign_Awake_Transpiler", "GetComponentFromSelfOrParent", typeof(Component));
            var bumperResolver = Find(ba, "BoardersBumperBlurbs.Game.Sign_Awake_Patch", "ResolveZNetView", typeof(Component));
            var conversion = Find(ca, "CraftyCartsRemake.SignAwakePatch2", "Prefix", typeof(Sign));
            if (cartType == null || bumperType == null ||
                !typeof(Component).IsAssignableFrom(cartType) || !typeof(Component).IsAssignableFrom(bumperType) ||
                cartResolver?.ReturnType != typeof(ZNetView) || bumperResolver?.ReturnType != typeof(ZNetView) ||
                conversion?.ReturnType != typeof(void))
                throw new InvalidOperationException("Upstream sign API changed; review this module before enabling it.");
            patches.Patch(cartResolver, postfix: new HarmonyMethod(typeof(CartSignCompatibility), nameof(ResolveMissingView)));
            patches.Patch(bumperResolver, postfix: new HarmonyMethod(typeof(CartSignCompatibility), nameof(ResolveMissingView)));
            patches.Patch(conversion, prefix: new HarmonyMethod(typeof(CartSignCompatibility), nameof(ConvertCartText)));
            log.LogInfo("Cart sign bridge active: both bumper layouts share their owning cart's network view; CraftyCarts text uses the vanilla sign font.");
        }
        catch (Exception e)
        {
            Disable();
            log.LogWarning($"Cart sign bridge disabled: {e.Message}");
        }
    }

    private static MethodInfo? Find(Assembly assembly, string typeName, string name, Type argument)
    {
        var type = assembly.GetType(typeName);
        var method = type == null ? null : AccessTools.DeclaredMethod(type, name, new[] { argument });
        return method?.IsStatic == true ? method : null;
    }

    internal static void Disable() { patches?.UnpatchSelf(); patches = null; }

    private static bool IsCraftySign(Component self) => self is Sign && cartType != null &&
        self.transform.parent != null && self.transform.parent.name == "BumperSticker" &&
        self.GetComponentInParent(cartType) != null;

    private static void ResolveMissingView(Component __0, ref ZNetView __result)
    {
        if (__result != null || __0 == null) return;
        if (IsCraftySign(__0) || (__0 is Sign && bumperType != null && __0.GetComponent(bumperType) != null))
            __result = __0.GetComponentInParent<ZNetView>();
    }

    // The upstream converter adds TMP to an active object, causing TMP.Awake to
    // request the missing default font. Assign a real game font while inactive.
    private static bool ConvertCartText(Sign __0)
    {
        if (__0 == null || !IsCraftySign(__0)) return true;
        var oldTransform = Utils.FindChild(__0.transform, "Text");
        var old = oldTransform != null ? oldTransform.GetComponent<Text>() : null;
        var prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab("sign") : null;
        var template = prefab != null ? prefab.GetComponent<Sign>() : null;
        var font = template != null && template.m_textWidget != null ? template.m_textWidget.font : null;
        if (oldTransform == null || old == null || font == null) return true;

        var replacement = new GameObject("Text", typeof(RectTransform));
        replacement.SetActive(false);
        var rect = (RectTransform)replacement.transform;
        rect.SetParent(oldTransform.parent, false);
        rect.SetSiblingIndex(oldTransform.GetSiblingIndex());
        rect.anchorMin = old.rectTransform.anchorMin;
        rect.anchorMax = old.rectTransform.anchorMax;
        rect.pivot = old.rectTransform.pivot;
        rect.sizeDelta = old.rectTransform.sizeDelta;
        rect.anchoredPosition3D = old.rectTransform.anchoredPosition3D;
        rect.localRotation = oldTransform.localRotation;
        rect.localScale = oldTransform.localScale;
        replacement.layer = old.gameObject.layer;
        var text = replacement.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = old.text;
        text.fontSize = old.fontSize;
        text.color = old.color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = old.raycastTarget;
        __0.m_textWidget = text;
        var wasActive = old.gameObject.activeSelf;
        old.gameObject.SetActive(false);
        UnityEngine.Object.Destroy(old.gameObject);
        replacement.SetActive(wasActive);
        return false;
    }
}
