/// <summary>
/// Identifies each screen/panel in the application UI.
/// Add new entries here when creating additional screens.
/// </summary>
public enum UIScreenId
{
    /// <summary>No screen is active.</summary>
    None = 0,

    /// <summary>The initial login-options screen.</summary>
    Initial,

    /// <summary>The send-code screen (email or SMS input).</summary>
    SendCode,

    /// <summary>The OTP verification screen.</summary>
    LoginWithCode,

    /// <summary>The authenticated user's home/dashboard screen.</summary>
    Authorized,

    /// <summary>The wallet management screen.</summary>
    Wallet
}
