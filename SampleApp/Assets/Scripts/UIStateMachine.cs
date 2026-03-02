using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure C# state machine for UI screen navigation.
/// Tracks the current screen, fires transition events, and maintains a back-navigation history stack.
/// <para>
/// Usage: Owned by <see cref="UIManager"/>. All screen transitions go through this class
/// so the application always knows which screen is active.
/// </para>
/// </summary>
public class UIStateMachine
{
    /// <summary>The currently visible screen.</summary>
    public UIScreenId CurrentScreen { get; private set; } = UIScreenId.None;

    /// <summary>The screen that was visible immediately before the current one.</summary>
    public UIScreenId PreviousScreen { get; private set; } = UIScreenId.None;

    private readonly Stack<UIScreenId> _history = new Stack<UIScreenId>();

    /// <summary>
    /// Raised when a screen transition occurs.
    /// Parameters: (fromScreen, toScreen).
    /// </summary>
    public event Action<UIScreenId, UIScreenId> OnScreenTransition;

    /// <summary>
    /// Transition to a target screen, pushing the current screen onto the history stack.
    /// </summary>
    /// <returns><c>true</c> if the transition was performed; <c>false</c> if already on the target screen.</returns>
    public bool TransitionTo(UIScreenId target)
    {
        if (target == CurrentScreen)
        {
            Debug.LogWarning($"UIStateMachine: Already on screen '{target}'.");
            return false;
        }

        PreviousScreen = CurrentScreen;

        if (CurrentScreen != UIScreenId.None)
            _history.Push(CurrentScreen);

        CurrentScreen = target;
        OnScreenTransition?.Invoke(PreviousScreen, CurrentScreen);
        return true;
    }

    /// <summary>
    /// Navigate back to the previous screen in the history stack.
    /// </summary>
    /// <returns><c>true</c> if the navigation succeeded; <c>false</c> if the history is empty.</returns>
    public bool GoBack()
    {
        if (_history.Count == 0)
            return false;

        PreviousScreen = CurrentScreen;
        CurrentScreen = _history.Pop();
        OnScreenTransition?.Invoke(PreviousScreen, CurrentScreen);
        return true;
    }

    /// <summary>
    /// Clear the navigation history (e.g., after login to prevent
    /// back-navigating into login screens).
    /// </summary>
    public void ClearHistory()
    {
        _history.Clear();
    }

    /// <summary>
    /// Reset the state machine to its initial idle state.
    /// </summary>
    public void Reset()
    {
        CurrentScreen = UIScreenId.None;
        PreviousScreen = UIScreenId.None;
        _history.Clear();
    }

    /// <summary>Number of screens in the back-navigation history.</summary>
    public int HistoryDepth => _history.Count;
}
