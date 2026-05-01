using BepInEx;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using Nosebleed.Pancake.Models;
using Nosebleed.Pancake.GameConfig;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace ComboClickMod;

[BepInPlugin("com.comboclick.mod", "Combo Click Mod", "1.0.0")]
public class ComboClickPlugin : BasePlugin
{
    public static ComboClickPlugin Instance { get; private set; }
    public static Dictionary<string, Sprite> SpriteCache = new();

    public override void Load()
    {
        Instance = this;
        ClassInjector.RegisterTypeInIl2Cpp(typeof(ComboClickBehaviour));
        var go = new GameObject("ComboClickBehaviourObj");
        go.AddComponent<ComboClickBehaviour>();
        Object.DontDestroyOnLoad(go);
        Log.LogInfo("ComboClickMod loaded.");
    }
    public override bool Unload() => true;

    internal bool _spritesExtracted;
    internal void EnsureSprites()
    {
        if (_spritesExtracted) return;
        _spritesExtracted = true;
        ExtractSprites();
    }

    internal void ExtractSprites()
    {
        try
        {
            foreach (var so in Resources.FindObjectsOfTypeAll<ScriptableObject>())
            {
                if (so.name != "CardDatabase") continue;
                var cards = ReadIl2CppList(GetProp(so, "Assets"));
                foreach (var card in cards)
                {
                    if (card == null) continue;
                    var n = ReadCardName(card);
                    if (string.IsNullOrEmpty(n) || n == "?" || SpriteCache.ContainsKey(n)) continue;
                    unsafe
                    {
                        var g = GetIl2CppField(card, "cardGroup");
                        if (g == null) continue;
                        long p = *(long*)(g.Pointer.ToInt64() + 0x68);
                        if (p != 0) SpriteCache[n] = new Sprite((System.IntPtr)p);
                    }
                }
                Log.LogInfo($"[ComboClickMod] Loaded {SpriteCache.Count} sprites");
                break;
            }
        }
        catch (System.Exception ex) { Log.LogError($"[ComboClickMod] Sprite: {ex}"); }
    }

    static string ReadCardName(Il2CppSystem.Object obj)
    {
        if (obj == null) return "?";
        try { var c = new CardConfig(obj.Pointer); var n = c.Name; if (!string.IsNullOrEmpty(n) && !n.StartsWith("No translation")) return n; } catch { }
        return "?";
    }
    static Il2CppSystem.Object GetProp(Il2CppSystem.Object o, string n) => o.GetIl2CppType().GetProperty(n)?.GetValue(o);
    static Il2CppSystem.Object GetIl2CppField(Il2CppSystem.Object o, string n) => o.GetIl2CppType().GetField(n, Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Instance)?.GetValue(o);
    static List<Il2CppSystem.Object> ReadIl2CppList(Il2CppSystem.Object lo)
    {
        var r = new List<Il2CppSystem.Object>();
        if (lo == null) return r;
        var en = lo.GetIl2CppType().GetMethod("GetEnumerator").Invoke(lo, null);
        var et = en.GetIl2CppType();
        while (Unbox<bool>(et.GetMethod("MoveNext").Invoke(en, null)))
            r.Add(et.GetProperty("Current").GetValue(en));
        return r;
    }
    public static unsafe T Unbox<T>(Il2CppSystem.Object v) where T : unmanaged => v.Unbox<T>();
}

public class ComboClickBehaviour : MonoBehaviour
{
    public ComboClickBehaviour(System.IntPtr ptr) : base(ptr) { }

    private bool _lastRightState, _lastBqState, _autoMode, _indicatorReady;
    private GameObject _indicatorCanvas;
    private Image _indicatorImage;
    private Sprite _comboSprite, _autoSprite;
    private int _cooldown;

    private void TryCreateIndicator()
    {
        _indicatorReady = true;
        try
        {
            ComboClickPlugin.Instance.EnsureSprites();
            ComboClickPlugin.SpriteCache.TryGetValue("飞刀", out _comboSprite);
            ComboClickPlugin.SpriteCache.TryGetValue("千刃", out _autoSprite);
            _indicatorCanvas = new GameObject("ComboIndicatorCanvas");
            var cv = _indicatorCanvas.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 1000;
            Object.DontDestroyOnLoad(_indicatorCanvas);
            var ig = new GameObject("ComboIndicatorImg");
            ig.transform.SetParent(_indicatorCanvas.transform, false);
            _indicatorImage = ig.AddComponent<Image>();
            _indicatorImage.preserveAspect = true;
            var rt = ig.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(20, 20);
            rt.sizeDelta = new Vector2(48, 48);
            UpdateIndicator();
        }
        catch (System.Exception ex) { ComboClickPlugin.Instance.Log.LogError($"[ComboClickMod] UI: {ex}"); }
    }

    private void UpdateIndicator()
    {
        if (_indicatorImage == null) return;
        var s = _autoMode ? _autoSprite : _comboSprite;
        _indicatorImage.sprite = s;
        _indicatorImage.enabled = s != null;
    }

    private void Update()
    {
        if (!_indicatorReady) TryCreateIndicator();
        if (Keyboard.current != null)
        {
            var bq = Keyboard.current.backquoteKey.isPressed;
            if (bq && !_lastBqState) { _autoMode = !_autoMode; UpdateIndicator(); ComboClickPlugin.Instance.Log.LogInfo($"[ComboClickMod] Mode={(_autoMode?"千刃":"飞刀")}"); }
            _lastBqState = bq;
        }
        if (_cooldown > 0) _cooldown--;
        if (Mouse.current == null) return;
        var rd = Mouse.current.rightButton.isPressed;
        if (rd && !_lastRightState && _cooldown <= 0)
        {
            if (_autoMode) TryPlayCard(true); else TryPlayCard(false);
        }
        _lastRightState = rd;
    }

    private void TryPlayCard(bool allowFallback)
    {
        var log = ComboClickPlugin.Instance.Log;
        try
        {
            var hp = GameObject.Find("HandPile"); if (hp == null) return;
            var pm = FindObjectOfType<PlayerModel>(); if (pm == null) return;
            var cards = hp.GetComponentsInChildren<CardModel>(true);
            log.LogInfo($"[ComboClickMod] Mode={(_autoMode?"千刃":"飞刀")} hand={cards.Length}");
            CardModel bn = null, bw = null, fb = null;
            int bnc = int.MaxValue, bwc = int.MaxValue, fbc = int.MaxValue;
            foreach (var card in cards)
            {
                if (card == null) continue;
                var cfg = card.CardConfig; if (cfg == null) continue;
                if (!card.gameObject.activeInHierarchy) continue;
                if (card.IsCopyWithDestroy) continue;
                var cost = cfg.manaCost;
                var hi = IsComboHighlighted(card);
                var wi = IsWildCard(card);

                bool consumable = HasDestroyEffect(cfg);
                if (consumable) continue;
                if (allowFallback && cost < fbc) { fbc = cost; fb = card; }
                if (!hi) continue;
                if (wi) { if (cost < bwc) { bwc = cost; bw = card; } }
                else { if (cost < bnc) { bnc = cost; bn = card; } }
            }
            var best = bn ?? bw;
            if (best == null && allowFallback) best = fb;
            if (best != null) { log.LogInfo($"[ComboClickMod] PLAY: {best.CardConfig.Name}"); pm.TryPlayCard(best, true); }
            else log.LogInfo("[ComboClickMod] NO PLAY");
            _cooldown = 10;
        }
        catch (System.Exception ex) { log.LogError($"[ComboClickMod] Error: {ex}"); }
    }

    private static bool HasDestroyEffect(CardConfig cfg)
    {
        try
        {
            var f = cfg.GetIl2CppType().GetField("onPlayEffect", Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Instance);
            if (f == null) return false;
            var arr = f.GetValue(cfg); if (arr == null) return false;
            var en = arr.GetIl2CppType().GetMethod("GetEnumerator").Invoke(arr, null);
            if (en == null) return false;
            var et = en.GetIl2CppType();
            var mn = et.GetMethod("MoveNext");
            var cp = et.GetProperty("Current");
            while (ComboClickPlugin.Unbox<bool>(mn.Invoke(en, null)))
            {
                var item = cp.GetValue(en);
                if (item == null) continue;
                if (item.GetIl2CppType().Name.Contains("Destroy")) return true;
            }
        }
        catch { }
        return false;
    }

    private static bool IsWildCard(CardModel c)
    {
        try { var ct = c.CardCostType; return ct != null && ct.GetIl2CppType().FullName.Contains("WildCostType"); } catch { }
        return false;
    }

    private static bool IsComboHighlighted(CardModel c)
    {
        try { var cv = c.CardView; return cv != null && ListHasActive(cv, "_comboElements"); } catch { }
        return false;
    }

    private static Il2CppSystem.Reflection.PropertyInfo _activeSelfProp;
    private static Il2CppSystem.Reflection.MethodInfo _getEnumM, _moveNextM;
    private static Il2CppSystem.Reflection.PropertyInfo _currentP;

    private static bool ListHasActive(MonoBehaviour cv, string fn)
    {
        var f = cv.GetIl2CppType().GetField(fn, Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.Instance);
        if (f == null) return false;
        var list = f.GetValue(cv); if (list == null) return false;
        if (_activeSelfProp == null) { var gt = Il2CppSystem.Type.GetType("UnityEngine.GameObject, UnityEngine.CoreModule", false); _activeSelfProp = gt?.GetProperty("activeSelf"); if (_activeSelfProp == null) return false; }
        if (_getEnumM == null) _getEnumM = list.GetIl2CppType().GetMethod("GetEnumerator");
        var en = _getEnumM.Invoke(list, null); if (en == null) return false;
        var et = en.GetIl2CppType();
        if (_moveNextM == null) _moveNextM = et.GetMethod("MoveNext");
        if (_currentP == null) _currentP = et.GetProperty("Current");
        while (ComboClickPlugin.Unbox<bool>(_moveNextM.Invoke(en, null)))
        {
            var go = _currentP.GetValue(en); if (go == null) continue;
            var av = _activeSelfProp.GetValue(go);
            if (av != null && ComboClickPlugin.Unbox<bool>(av)) return true;
        }
        return false;
    }
}
