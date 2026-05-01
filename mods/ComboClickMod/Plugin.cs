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
                    var name = ReadCardName(card);
                    if (string.IsNullOrEmpty(name) || SpriteCache.ContainsKey(name)) continue;

                    unsafe
                    {
                        var group = GetIl2CppField(card, "cardGroup");
                        if (group == null) continue;
                        long ptr = *(long*)(group.Pointer.ToInt64() + 0x68);
                        if (ptr != 0)
                        {
                            SpriteCache[name] = new Sprite((System.IntPtr)ptr);
                        }
                    }
                }
                Log.LogInfo($"[ComboClickMod] Loaded {SpriteCache.Count} card sprites");
                break;
            }
        }
        catch (System.Exception ex) { Log.LogError($"[ComboClickMod] Sprite extraction error: {ex}"); }
    }

    static Il2CppSystem.Object GetProp(Il2CppSystem.Object obj, string name)
        => obj.GetIl2CppType().GetProperty(name)?.GetValue(obj);

    static Il2CppSystem.Object GetIl2CppField(Il2CppSystem.Object obj, string name)
        => obj.GetIl2CppType().GetField(name,
            Il2CppSystem.Reflection.BindingFlags.Public |
            Il2CppSystem.Reflection.BindingFlags.NonPublic |
            Il2CppSystem.Reflection.BindingFlags.Instance)?.GetValue(obj);

    static string ReadCardName(Il2CppSystem.Object obj)
    {
        if (obj == null) return null;
        var v = GetProp(obj, "Name");
        if (v == null) return null;
        return Il2CppInterop.Runtime.IL2CPP.Il2CppStringToManaged(v.Pointer);
    }

    static List<Il2CppSystem.Object> ReadIl2CppList(Il2CppSystem.Object listObj)
    {
        var r = new List<Il2CppSystem.Object>();
        if (listObj == null) return r;
        var en = listObj.GetIl2CppType().GetMethod("GetEnumerator").Invoke(listObj, null);
        var et = en.GetIl2CppType();
        var moveNext = et.GetMethod("MoveNext");
        var current = et.GetProperty("Current");
        while (Unbox<bool>(moveNext.Invoke(en, null)))
            r.Add(current.GetValue(en));
        return r;
    }

    public static unsafe T Unbox<T>(Il2CppSystem.Object v) where T : unmanaged => v.Unbox<T>();
}

public class ComboClickBehaviour : MonoBehaviour
{
    public ComboClickBehaviour(System.IntPtr ptr) : base(ptr) { }

    private bool _lastRightState;
    private bool _lastBackquoteState;
    private bool _autoMode;
    private bool _indicatorReady;
    private GameObject _indicatorCanvas;
    private Image _indicatorImage;
    private Sprite _comboSprite;
    private Sprite _autoSprite;

    private void TryCreateIndicator()
    {
        _indicatorReady = true;
        try
        {
            ComboClickPlugin.Instance.EnsureSprites();
            ComboClickPlugin.SpriteCache.TryGetValue("飞刀", out _comboSprite);
            ComboClickPlugin.SpriteCache.TryGetValue("千刃", out _autoSprite);

            _indicatorCanvas = new GameObject("ComboIndicatorCanvas");
            var canvas = _indicatorCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            Object.DontDestroyOnLoad(_indicatorCanvas);

            var imgGo = new GameObject("ComboIndicatorImg");
            imgGo.transform.SetParent(_indicatorCanvas.transform, false);
            _indicatorImage = imgGo.AddComponent<Image>();
            _indicatorImage.preserveAspect = true;

            var rt = imgGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(20, 20);
            rt.sizeDelta = new Vector2(48, 48);

            UpdateIndicator();
        }
        catch (System.Exception ex)
        {
            ComboClickPlugin.Instance.Log.LogError($"[ComboClickMod] Indicator error: {ex}");
        }
    }

    private void UpdateIndicator()
    {
        if (_indicatorImage == null) return;
        var sprite = _autoMode ? _autoSprite : _comboSprite;
        if (sprite != null)
        {
            _indicatorImage.sprite = sprite;
            _indicatorImage.enabled = true;
        }
        else
        {
            _indicatorImage.enabled = false;
        }
    }

    private void Update()
    {
        if (!_indicatorReady) TryCreateIndicator();

        if (Keyboard.current != null)
        {
            var bqDown = Keyboard.current.backquoteKey.isPressed;
            if (bqDown && !_lastBackquoteState)
            {
                _autoMode = !_autoMode;
                UpdateIndicator();
                ComboClickPlugin.Instance.Log.LogInfo(_autoMode
                    ? "[ComboClickMod] Auto mode ON"
                    : "[ComboClickMod] Combo mode ON");
            }
            _lastBackquoteState = bqDown;
        }

        if (Mouse.current == null) return;
        var rightDown = Mouse.current.rightButton.isPressed;
        if (rightDown && !_lastRightState)
        {
            if (_autoMode)
                TryPlayLowestCostCard();
            else
                TryPlayComboCard();
        }
        _lastRightState = rightDown;
    }

    private void TryPlayLowestCostCard()
    {
        try
        {
            var handPile = GameObject.Find("HandPile");
            if (handPile == null) return;

            var playerModel = FindObjectOfType<PlayerModel>();
            if (playerModel == null) return;

            var cards = handPile.GetComponentsInChildren<CardModel>(true);

            CardModel bestCard = null;
            int bestCost = int.MaxValue;

            foreach (var card in cards)
            {
                if (card == null) continue;
                var config = card.CardConfig;
                if (config == null) continue;

                var cost = config.manaCost;
                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestCard = card;
                }
            }

            if (bestCard != null)
                playerModel.TryPlayCard(bestCard, true);
        }
        catch { }
    }

    private void TryPlayComboCard()
    {
        try
        {
            var handPile = GameObject.Find("HandPile");
            if (handPile == null) return;

            var playerModel = FindObjectOfType<PlayerModel>();
            if (playerModel == null) return;

            var cards = handPile.GetComponentsInChildren<CardModel>(true);

            CardModel bestNormal = null;
            int bestNormalCost = int.MaxValue;
            CardModel bestWild = null;
            int bestWildCost = int.MaxValue;

            foreach (var card in cards)
            {
                if (card == null) continue;
                var config = card.CardConfig;
                if (config == null) continue;
                if (!IsComboHighlighted(card)) continue;

                var cost = config.manaCost;
                if (IsWildCard(card))
                {
                    if (cost < bestWildCost)
                    {
                        bestWildCost = cost;
                        bestWild = card;
                    }
                }
                else
                {
                    if (cost < bestNormalCost)
                    {
                        bestNormalCost = cost;
                        bestNormal = card;
                    }
                }
            }

            var bestCard = bestNormal ?? bestWild;
            if (bestCard != null)
                playerModel.TryPlayCard(bestCard, true);
        }
        catch { }
    }

    private static bool IsWildCard(CardModel card)
    {
        try
        {
            var costType = card.CardCostType;
            if (costType == null) return false;
            return costType.GetIl2CppType().FullName.Contains("WildCostType");
        }
        catch { }
        return false;
    }

    private static bool IsComboHighlighted(CardModel card)
    {
        try
        {
            var cv = card.CardView;
            if (cv == null) return false;
            return ListHasActive(cv, "_comboElements");
        }
        catch { }
        return false;
    }

    private static Il2CppSystem.Reflection.PropertyInfo _activeSelfProp;
    private static Il2CppSystem.Reflection.MethodInfo _getEnumeratorMethod;
    private static Il2CppSystem.Reflection.MethodInfo _moveNextMethod;
    private static Il2CppSystem.Reflection.PropertyInfo _currentProp;

    private static bool ListHasActive(MonoBehaviour cv, string fieldName)
    {
        var field = cv.GetIl2CppType().GetField(fieldName,
            Il2CppSystem.Reflection.BindingFlags.Public |
            Il2CppSystem.Reflection.BindingFlags.NonPublic |
            Il2CppSystem.Reflection.BindingFlags.Instance);
        if (field == null) return false;

        var list = field.GetValue(cv);
        if (list == null) return false;

        if (_activeSelfProp == null)
        {
            var goType = Il2CppSystem.Type.GetType("UnityEngine.GameObject, UnityEngine.CoreModule", false);
            _activeSelfProp = goType?.GetProperty("activeSelf");
            if (_activeSelfProp == null) return false;
        }

        if (_getEnumeratorMethod == null)
            _getEnumeratorMethod = list.GetIl2CppType().GetMethod("GetEnumerator");

        var en = _getEnumeratorMethod.Invoke(list, null);
        if (en == null) return false;
        var et = en.GetIl2CppType();

        if (_moveNextMethod == null)
            _moveNextMethod = et.GetMethod("MoveNext");
        if (_currentProp == null)
            _currentProp = et.GetProperty("Current");

        while (ComboClickPlugin.Unbox<bool>(_moveNextMethod.Invoke(en, null)))
        {
            var go = _currentProp.GetValue(en);
            if (go == null) continue;
            var av = _activeSelfProp.GetValue(go);
            if (av != null && ComboClickPlugin.Unbox<bool>(av))
                return true;
        }
        return false;
    }
}
