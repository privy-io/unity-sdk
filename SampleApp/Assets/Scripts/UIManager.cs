using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central UI orchestrator that manages screen transitions via a <see cref="UIStateMachine"/>.
/// Screens are registered with their root GameObjects and optional lifecycle callbacks.
/// All screen visibility is controlled here — individual controllers should not call
/// <c>SetActive</c> on screen roots directly.
/// <para>
/// <b>Extending:</b> To add a new screen, add an entry to <see cref="UIScreenId"/>,
/// register it in <see cref="RegisterScreens"/> (or call <see cref="RegisterScreen"/> at runtime),
/// and navigate to it with <see cref="NavigateTo"/>.
/// </para>
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Screen Controllers")]
    public InitialScreenController initialScreenController;
    public AuthScreenController authScreenController;
    public WalletController walletScreenController;

    /// <summary>The state machine tracking the current screen and navigation history.</summary>
    public UIStateMachine StateMachine { get; private set; }

    // tracks whether RegisterScreens has been called already (avoids double registration)
    private bool _screensRegistered;

    // ── Screen registration ─────────────────────────────────────────────────

    private readonly Dictionary<UIScreenId, ScreenRegistration> _screens =
        new Dictionary<UIScreenId, ScreenRegistration>();

    /// <summary>
    /// Holds the root GameObject and optional lifecycle callbacks for a registered screen.
    /// </summary>
    public struct ScreenRegistration
    {
        public GameObject Root;
        public Action OnShow;
        public Action OnHide;
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            StateMachine = new UIStateMachine();
            StateMachine.OnScreenTransition += HandleScreenTransition;

            // register panels early so navigation from Awake (e.g. auth callback)
            // succeeds rather than logging an error.
            RegisterScreens();
        }
        else
        {
            Debug.LogWarning("UIManager: Duplicate instance destroyed.");
            Destroy(this);
        }
    }

    private void Start()
    {
        // only show the default if nothing else has been displayed yet
        if (StateMachine.CurrentScreen == UIScreenId.None)
            NavigateTo(UIScreenId.Initial);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            if (StateMachine != null)
                StateMachine.OnScreenTransition -= HandleScreenTransition;
            Instance = null;
        }
    }

    // ── Internal screen registration ─────────────────────────────────────────

    /// <summary>
    /// Registers all known screens from child controllers.
    /// Called once during <c>Start</c> after all <c>Awake</c> methods have executed.
    /// </summary>
    private void RegisterScreens()
    {
        RegisterScreen(UIScreenId.Initial,
            initialScreenController.initialUI);

        RegisterScreen(UIScreenId.SendCode,
            authScreenController.sendCodeUI);

        RegisterScreen(UIScreenId.LoginWithCode,
            authScreenController.loginWithCodeUI);

        RegisterScreen(UIScreenId.Authorized,
            authScreenController.authorizedUI,
            onShow: () => authScreenController.OnAuthorizedScreenShown());

        RegisterScreen(UIScreenId.Wallet,
            walletScreenController.walletUI,
            onShow: () => walletScreenController.OnWalletScreenShown(),
            onHide: () => walletScreenController.OnWalletScreenHidden());
    }

    // ── Public registration API ──────────────────────────────────────────────

    /// <summary>
    /// Register a screen panel with the UI system.
    /// Use this to add custom screens at runtime.
    /// </summary>
    /// <param name="id">Unique screen identifier.</param>
    /// <param name="root">The root GameObject to toggle active/inactive.</param>
    /// <param name="onShow">Optional callback invoked after the screen becomes active.</param>
    /// <param name="onHide">Optional callback invoked after the screen becomes inactive.</param>
    public void RegisterScreen(UIScreenId id, GameObject root, Action onShow = null, Action onHide = null)
    {
        if (root == null)
        {
            Debug.LogWarning($"UIManager: Cannot register screen '{id}' with a null root GameObject.");
            return;
        }

        _screens[id] = new ScreenRegistration
        {
            Root = root,
            OnShow = onShow,
            OnHide = onHide
        };

        // All screens start hidden; the initial screen is activated via NavigateTo in Start.
        root.SetActive(false);
    }

    /// <summary>Remove a screen registration (for dynamically managed panels).</summary>
    public void UnregisterScreen(UIScreenId id)
    {
        _screens.Remove(id);
    }

    // ── Transition handler ───────────────────────────────────────────────────

    private void HandleScreenTransition(UIScreenId from, UIScreenId to)
    {
        // Defensively hide ALL registered screens except the target
        foreach (var kvp in _screens)
        {
            if (kvp.Key != to)
                kvp.Value.Root.SetActive(false);
        }

        // Notify the screen we are leaving
        if (from != UIScreenId.None && _screens.TryGetValue(from, out var fromEntry))
        {
            fromEntry.OnHide?.Invoke();
        }

        // Show and notify the target screen
        if (_screens.TryGetValue(to, out var toEntry))
        {
            toEntry.Root.SetActive(true);
            toEntry.OnShow?.Invoke();
        }
        else
        {
            Debug.LogError($"UIManager: Screen '{to}' is not registered. " +
                           "Call RegisterScreen() before navigating to it.");
        }
    }

    // ── Navigation API ───────────────────────────────────────────────────────

    /// <summary>Navigate to a screen by its identifier.</summary>
    public void NavigateTo(UIScreenId screen)
    {
        if (!_screensRegistered)
            RegisterScreens();   // lazy fallback

        StateMachine.TransitionTo(screen);
    }

    /// <summary>Navigate back to the previous screen via the history stack.</summary>
    public void GoBack()
    {
        if (!StateMachine.GoBack())
            Debug.LogWarning("UIManager: No screen history to navigate back to.");
    }

    /// <summary>The currently active screen.</summary>
    public UIScreenId CurrentScreen => StateMachine.CurrentScreen;

    // ── Backward-compatible convenience methods ──────────────────────────────
    // Existing call-sites (button handlers, auth callbacks, etc.) continue to work.

    public void ShowInitialScreen()
    {
        StateMachine.ClearHistory();
        NavigateTo(UIScreenId.Initial);
    }

    /// <summary>
    /// Shows the send-code screen configured for the given login method.
    /// </summary>
    public void ShowSendCodeScreen(
        AuthScreenController.LoginMethod method = AuthScreenController.LoginMethod.Email)
    {
        authScreenController.SetLoginMethod(method);
        NavigateTo(UIScreenId.SendCode);
    }

    public void ShowLoginWithCodeScreen()
    {
        NavigateTo(UIScreenId.LoginWithCode);
    }

    public void ShowWalletUI()
    {
        NavigateTo(UIScreenId.Wallet);
    }

    public void ShowAuthorizedScreen()
    {
        StateMachine.ClearHistory();

        // If already on the authorized screen, just refresh the data display
        if (StateMachine.CurrentScreen == UIScreenId.Authorized)
        {
            authScreenController.OnAuthorizedScreenShown();
            return;
        }

        NavigateTo(UIScreenId.Authorized);
    }
}
