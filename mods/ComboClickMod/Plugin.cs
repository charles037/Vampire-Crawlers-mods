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

            CardModel bestCard = null;
            int bestCost = int.MaxValue;

            foreach (var card in cards)
            {
                if (card == null) continue;
                var config = card.CardConfig;
                if (config == null) continue;
                if (!IsComboHighlighted(card)) continue;

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

    private static Il2CppSystem.Reflection.FieldInfo _comboElementsField;
    private static Il2CppSystem.Reflection.PropertyInfo _activeSelfProp;
    private static Il2CppSystem.Reflection.MethodInfo _getEnumeratorMethod;
    private static Il2CppSystem.Reflection.MethodInfo _moveNextMethod;
    private static Il2CppSystem.Reflection.PropertyInfo _currentProp;

    private static bool IsComboHighlighted(CardModel card)
    {
        try
        {
            var cv = card.CardView;
            if (cv == null) return false;

            if (_comboElementsField == null)
            {
                _comboElementsField = cv.GetIl2CppType().GetField("_comboElements",
                    Il2CppSystem.Reflection.BindingFlags.Public |
                    Il2CppSystem.Reflection.BindingFlags.NonPublic |
                    Il2CppSystem.Reflection.BindingFlags.Instance);
                if (_comboElementsField == null) return false;
            }

            var list = _comboElementsField.GetValue(cv);
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
        }
        catch { }
        return false;
    }

    static unsafe T Unbox<T>(Il2CppSystem.Object v) where T : unmanaged => v.Unbox<T>();
}
