using BepInEx;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using Nosebleed.Pancake.Models;
using Nosebleed.Pancake.GameConfig;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ComboClickMod;

[BepInPlugin("com.comboclick.mod", "Combo Click Mod", "1.0.0")]
public class ComboClickPlugin : BasePlugin
{
    public static ComboClickPlugin Instance { get; private set; }

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
}

public class ComboClickBehaviour : MonoBehaviour
{
    public ComboClickBehaviour(System.IntPtr ptr) : base(ptr) { }

    private bool _lastRightState;

    private void Update()
    {
        if (Mouse.current == null) return;
        var rightDown = Mouse.current.rightButton.isPressed;
        if (rightDown && !_lastRightState)
            TryPlayComboCard();
        _lastRightState = rightDown;
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

        while (Unbox<bool>(_moveNextMethod.Invoke(en, null)))
        {
            var go = _currentProp.GetValue(en);
            if (go == null) continue;
            var av = _activeSelfProp.GetValue(go);
            if (av != null && Unbox<bool>(av))
                return true;
        }
        return false;
    }

    static unsafe T Unbox<T>(Il2CppSystem.Object v) where T : unmanaged => v.Unbox<T>();
}
