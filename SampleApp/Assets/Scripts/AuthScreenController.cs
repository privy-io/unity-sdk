using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;
using Privy;
using Privy.Core;
using Privy.Auth;
using Privy.Auth.Models;
using Privy.Utils;

/// <summary>
/// Controls all auth screens: send-code, login-with-code (both Email and SMS),
/// and the authorized/home screen which includes SMS link, unlink and update examples.
/// </summary>
public class AuthScreenController : MonoBehaviour
{
    // ── Screen roots (referenced by UIManager for registration) ────────────
    public GameObject sendCodeUI;

    /// <summary>Optional label updated to show "Email" or "Phone Number" based on login method.</summary>
    public TMP_Text sendCodeInputLabel;

    /// <summary>Reused for both email address and E.164 phone number input.</summary>
    public TMP_InputField emailInputField;
    public Button logOutButton;
    public Button sendCodeButton;

    public Button walletButton;
    public GameObject loginWithCodeUI;
    public TMP_InputField codeInputField;
    public Button loginWithCodeButton;

    // ── Authorized screen ────────────────────────────────────────────────────
    public GameObject authorizedUI;
    public TextMeshProUGUI userObject;
    public TextMeshProUGUI walletAddress;
    public Button getAccessTokenButton;
    public Button getIdentityTokenButton;

    // ── SMS link / unlink / update (wire up in Inspector on authorizedUI) ────
    /// <summary>Input field for phone number used in link / unlink / update operations.</summary>
    public TMP_InputField smsPhoneInputField;
    /// <summary>Input field for the OTP code used in link and update operations.</summary>
    public TMP_InputField smsCodeInputField;
    /// <summary>Sends an OTP to smsPhoneInputField then enables linkSmsButton.</summary>
    public Button smsSendCodeForLinkButton;
    /// <summary>Links the phone number using the code in smsCodeInputField.</summary>
    public Button linkSmsButton;
    /// <summary>Unlinks the phone number in smsPhoneInputField from the current user.</summary>
    public Button unlinkSmsButton;
    /// <summary>Updates the linked phone number to the value in smsPhoneInputField using smsCodeInputField.</summary>
    public Button updateSmsPhoneButton;

    // ── Login method state ───────────────────────────────────────────────────
    public enum LoginMethod { Email, SMS }
    private LoginMethod _currentLoginMethod = LoginMethod.Email;

    private void Awake()
    {
        sendCodeButton.onClick.AddListener(OnSendCodeButtonClick);
        loginWithCodeButton.onClick.AddListener(OnLoginWithCodeButtonClick);
        logOutButton.onClick.AddListener(OnLogOutButtonClick);
        walletButton.onClick.AddListener(OnWalletButtonClick);
        getAccessTokenButton.onClick.AddListener(OnGetAccessTokenButtonClick);
        getIdentityTokenButton.onClick.AddListener(OnGetIdentityTokenButtonClick);

        // SMS link / unlink / update — only wire if assigned in Inspector
        if (smsSendCodeForLinkButton != null)
            smsSendCodeForLinkButton.onClick.AddListener(OnSmsSendCodeForLinkButtonClick);
        if (linkSmsButton != null)
            linkSmsButton.onClick.AddListener(OnLinkSmsButtonClick);
        if (unlinkSmsButton != null)
            unlinkSmsButton.onClick.AddListener(OnUnlinkSmsButtonClick);
        if (updateSmsPhoneButton != null)
            updateSmsPhoneButton.onClick.AddListener(OnUpdateSmsPhoneButtonClick);

        PrivyManager.Instance.AuthStateChanged += OnAuthStateChange;
    }

    private void OnDestroy()
    {
        // remove event handler to prevent memory leaks when this object is destroyed
        if (PrivyManager.Instance != null)
        {
            PrivyManager.Instance.AuthStateChanged -= OnAuthStateChange;
        }
    }

    // ── Login method switching ───────────────────────────────────────────────

    /// <summary>Called by UIManager before showing the send-code screen.</summary>
    internal void SetLoginMethod(LoginMethod method)
    {
        _currentLoginMethod = method;

        if (sendCodeInputLabel != null)
            sendCodeInputLabel.text = method == LoginMethod.SMS ? "Phone Number (E.164):" : "Email:";

        // Update the input field placeholder text
        var placeholder = emailInputField.placeholder as TMP_Text;
        if (placeholder != null)
            placeholder.text = method == LoginMethod.SMS
                ? "Enter phone number (+15551234567)"
                : "Enter email address";
    }

    // ── Auth state ───────────────────────────────────────────────────────────

    private void OnAuthStateChange(AuthState state)
    {
        if (state == AuthState.Authenticated)
            UIManager.Instance.ShowAuthorizedScreen();
    }

    /// <summary>
    /// Lifecycle callback invoked by UIManager when the authorized screen becomes visible.
    /// Fetches fresh user data and updates the display.
    /// </summary>
    public async void OnAuthorizedScreenShown()
    {
        try
        {
            IPrivyUser user = await PrivyManager.Instance.GetUser();
            if (user != null)
            {
                UpdateUserDisplay(user);
                Debug.Log("User is authenticated: " + user.Id);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to load user data: " + ex.Message);
        }
    }

    /// <summary>
    /// Update the user-info display on the authorized screen.
    /// Can be called externally to refresh after link/unlink operations.
    /// </summary>
    public void UpdateUserDisplay(IPrivyUser user)
    {
        if (user != null)
            userObject.text = JsonConvert.SerializeObject(user);
    }

    // ── Send code (email or SMS) ─────────────────────────────────────────────

    private bool IsValidE164(string phone)
    {
        // simple regex for E.164: + followed by 1–15 digits
        return System.Text.RegularExpressions.Regex.IsMatch(phone, @"^\+[1-9]\d{1,14}$");
    }

    private async void OnSendCodeButtonClick()
    {
        string input = emailInputField.text;
        if (string.IsNullOrEmpty(input))
        {
            Debug.LogError(_currentLoginMethod == LoginMethod.SMS ? "Phone number is empty." : "Email is empty.");
            return;
        }

        if (_currentLoginMethod == LoginMethod.SMS)
        {
            if (!IsValidE164(input))
            {
                Debug.LogError("Phone number must be in E.164 format (e.g. +15551234567)");
                return;
            }

            try
            {
                bool success = await PrivyManager.Instance.Sms.SendCode(input);
                if (success)
                    UIManager.Instance.ShowLoginWithCodeScreen();
                else
                    Debug.LogError("Failed to send SMS code.");
            }
            catch (PrivyAuthenticationException ex) when (ex.Error == AuthenticationError.InvalidPhoneNumber)
            {
                Debug.LogError("Server rejected phone number as invalid: " + ex.Message);
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to send SMS code: " + ex.Message);
            }
        }
        else
        {
            bool success = await PrivyManager.Instance.Email.SendCode(input);
            if (success)
                UIManager.Instance.ShowLoginWithCodeScreen();
            else
                Debug.LogError("Failed to send email code.");
        }
    }

    // ── Login with code (email or SMS) ───────────────────────────────────────

    private async void OnLoginWithCodeButtonClick()
    {
        string input = emailInputField.text;
        string code = codeInputField.text;

        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(code))
        {
            Debug.LogError("Input or code is empty.");
            return;
        }

        try
        {
            if (_currentLoginMethod == LoginMethod.SMS)
                await PrivyManager.Instance.Sms.LoginWithCode(input, code);
            else
                await PrivyManager.Instance.Email.LoginWithCode(input, code);
        }
        catch (PrivyAuthenticationException ex) when (ex.Error == AuthenticationError.IncorrectOtpCode)
        {
            Debug.LogError("Incorrect OTP code — please try again.");
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to log in with code: " + ex.Message);
        }
    }

    // ── SMS link (send code step) ────────────────────────────────────────────

    private async void OnSmsSendCodeForLinkButtonClick()
    {
        string phone = smsPhoneInputField != null ? smsPhoneInputField.text : string.Empty;
        if (string.IsNullOrEmpty(phone))
        {
            Debug.LogError("Phone number is empty.");
            return;
        }

        try
        {
            bool sent = await PrivyManager.Instance.Sms.SendCode(phone);
            Debug.Log(sent ? $"SMS code sent to {phone}" : $"Failed to send SMS code to {phone}");
        }
        catch (Exception ex)
        {
            Debug.LogError("SMS send code error: " + ex.Message);
        }
    }

    // ── SMS link (verify code step) ──────────────────────────────────────────

    private async void OnLinkSmsButtonClick()
    {
        string phone = smsPhoneInputField != null ? smsPhoneInputField.text : string.Empty;
        string code = smsCodeInputField != null ? smsCodeInputField.text : string.Empty;

        if (string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(code))
        {
            Debug.LogError("Phone number or SMS code is empty.");
            return;
        }

        try
        {
            await PrivyManager.Instance.Sms.Link(phone, code);
            IPrivyUser user = await PrivyManager.Instance.GetUser();
            UpdateUserDisplay(user);
            Debug.Log($"Phone {phone} linked successfully.");
        }
        catch (PrivyAuthenticationException ex) when (ex.Error == AuthenticationError.IncorrectOtpCode)
        {
            Debug.LogError("Incorrect OTP code for link.");
        }
        catch (Exception ex)
        {
            Debug.LogError("SMS link error: " + ex.Message);
        }
    }

    // ── SMS unlink ───────────────────────────────────────────────────────────

    private async void OnUnlinkSmsButtonClick()
    {
        string phone = smsPhoneInputField != null ? smsPhoneInputField.text : string.Empty;
        if (string.IsNullOrEmpty(phone))
        {
            Debug.LogError("Phone number is empty.");
            return;
        }
        if (!IsValidE164(phone))
        {
            Debug.LogError("Phone number must be in E.164 format (e.g. +15551234567)");
            return;
        }

        try
        {
            await PrivyManager.Instance.Sms.Unlink(phone);
            IPrivyUser user = await PrivyManager.Instance.GetUser();
            UpdateUserDisplay(user);
            Debug.Log($"Phone {phone} unlinked successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError("SMS unlink error: " + ex.Message);
        }
    }

    // ── SMS update phone number ──────────────────────────────────────────────

    private async void OnUpdateSmsPhoneButtonClick()
    {
        string phone = smsPhoneInputField != null ? smsPhoneInputField.text : string.Empty;
        string code = smsCodeInputField != null ? smsCodeInputField.text : string.Empty;

        if (string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(code))
        {
            Debug.LogError("Phone number or SMS code is empty.");
            return;
        }

        try
        {
            await PrivyManager.Instance.Sms.UpdatePhoneNumber(phone, code);
            IPrivyUser user = await PrivyManager.Instance.GetUser();
            UpdateUserDisplay(user);
            Debug.Log($"Phone number updated to {phone}.");
        }
        catch (PrivyAuthenticationException ex) when (ex.Error == AuthenticationError.IncorrectOtpCode)
        {
            Debug.LogError("Incorrect OTP code for phone update.");
        }
        catch (Exception ex)
        {
            Debug.LogError("SMS update phone error: " + ex.Message);
        }
    }

    // ── Misc ─────────────────────────────────────────────────────────────────

    private void OnWalletButtonClick()
    {
        UIManager.Instance.ShowWalletUI();
    }

    private async void OnGetAccessTokenButtonClick()
    {
        IPrivyUser user = await PrivyManager.Instance.GetUser();
        if (user == null)
        {
            Debug.LogWarning("No authenticated user available to fetch access token.");
            return;
        }

        try
        {
            string accessToken = await user.GetAccessToken();
            Debug.Log("Access token: " + accessToken);
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to get access token: " + ex.Message);
        }
    }

    private async void OnGetIdentityTokenButtonClick()
    {
        IPrivyUser user = await PrivyManager.Instance.GetUser();
        if (user == null)
        {
            Debug.LogWarning("No authenticated user available to fetch identity token.");
            return;
        }

        try
        {
            string identityToken = await user.GetIdentityToken();
            Debug.Log("Identity token: " + identityToken);
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to get identity token: " + ex.Message);
        }
    }

    private void OnLogOutButtonClick()
    {
        PrivyManager.Instance.Logout();
        UIManager.Instance.ShowInitialScreen();
    }
}
