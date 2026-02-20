using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;
using Privy;

public class AuthScreenController : MonoBehaviour
{
    public GameObject initialUI;
    public GameObject sendCodeUI;
    public TMP_InputField emailInputField;
    public Button logOutButton;
    public Button sendCodeButton;

    public Button walletButton;
    public GameObject loginWithCodeUI;
    public TMP_InputField codeInputField;
    public Button loginWithCodeButton;

    public GameObject authorizedUI;
    public TextMeshProUGUI userObject;
    public TextMeshProUGUI walletAddress;
    public Button getAccessTokenButton;
    public Button getIdentityTokenButton;

    private void Awake()
    {
        sendCodeButton.onClick.AddListener(OnSendCodeButtonClick);
        loginWithCodeButton.onClick.AddListener(OnLoginWithCodeButtonClick);
        logOutButton.onClick.AddListener(OnLogOutButtonClick);
        walletButton.onClick.AddListener(OnWalletButtonClick); // Attach this event listener
        getAccessTokenButton.onClick.AddListener(OnGetAccessTokenButtonClick);
        getIdentityTokenButton.onClick.AddListener(OnGetIdentityTokenButtonClick);
        PrivyManager.Instance.SetAuthStateChangeCallback(OnAuthStateChange);
    }

    private void OnAuthStateChange(AuthState state)
    {
        if (state == AuthState.Authenticated)
        {
            ShowAuthorizedScreen();
        }
    }

    private async void ShowAuthorizedScreen()
    {
        initialUI.SetActive(false);
        sendCodeUI.SetActive(false);
        loginWithCodeUI.SetActive(false);
        authorizedUI.SetActive(true);

        PrivyUser user = await PrivyManager.Instance.GetUser();
        userObject.text = JsonConvert.SerializeObject(user);
        Debug.Log("User is authenticated: " + user!.Id);
    }

    private async void OnSendCodeButtonClick()
    {
        string email = emailInputField.text;
        if (!string.IsNullOrEmpty(email))
        {
            bool success = await PrivyManager.Instance.Email.SendCode(email);
            if (success)
            {
                UIManager.Instance.ShowLoginWithCodeScreen();
            }
            else
            {
                Debug.LogError("Failed to send code.");
            }
        }
        else
        {
            Debug.LogError("Email is empty.");
        }
    }

    private async void OnLoginWithCodeButtonClick()
    {
        string email = emailInputField.text;
        string code = codeInputField.text;
        if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(code))
        {
            try
            {
                await PrivyManager.Instance.Email.LoginWithCode(email, code);
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to log in with code: " + ex.Message);
            }
        }
        else
        {
            Debug.LogError("Email or code is empty.");
        }
    }

    private void OnWalletButtonClick()
    {
        UIManager.Instance.ShowWalletUI(); // Transition to the Wallet UI
    }

    private async void OnGetAccessTokenButtonClick()
    {
        var user = await PrivyManager.Instance.GetUser();
        string accessToken = await user!.GetAccessToken();
        Debug.Log("Access token: " + accessToken);
    }

    private async void OnGetIdentityTokenButtonClick()
    {
        var user = await PrivyManager.Instance.GetUser();
        string identityToken = await user!.GetIdentityToken();
        Debug.Log("Identity token: " + identityToken);
    }

    private void OnLogOutButtonClick()
    {
        PrivyManager.Instance.Logout();
        UIManager.Instance.ShowInitialScreen();
    }
}
