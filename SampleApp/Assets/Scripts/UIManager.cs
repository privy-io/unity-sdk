using UnityEngine;
using Newtonsoft.Json;
using Privy;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public InitialScreenController initialScreenController;
    public AuthScreenController authScreenController;
    public WalletController walletScreenController;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != null)
        {
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }
    }

    public void ShowInitialScreen()
    {
        initialScreenController.initialUI.SetActive(true);
        authScreenController.sendCodeUI.SetActive(false);
        authScreenController.loginWithCodeUI.SetActive(false);
        authScreenController.authorizedUI.SetActive(false);
    }

    public void ShowSendCodeScreen()
    {
        initialScreenController.initialUI.SetActive(false);
        authScreenController.sendCodeUI.SetActive(true);
        authScreenController.loginWithCodeUI.SetActive(false);
        authScreenController.authorizedUI.SetActive(false);
    }

    public void ShowLoginWithCodeScreen()
    {
        initialScreenController.initialUI.SetActive(false);
        authScreenController.sendCodeUI.SetActive(false);
        authScreenController.loginWithCodeUI.SetActive(true);
        authScreenController.authorizedUI.SetActive(false);
    }

    public void ShowWalletUI()
    {
        initialScreenController.initialUI.SetActive(false);
        authScreenController.authorizedUI.SetActive(false);
        walletScreenController.ShowWalletUI();
    }

    public async void ShowAuthorizedScreen()
    {
        //Update the user object
        PrivyUser user = await PrivyManager.Instance.GetUser();
        if ( user != null ) {
            authScreenController.userObject.text = JsonConvert.SerializeObject(user);        
        };

        initialScreenController.initialUI.SetActive(false);
        authScreenController.authorizedUI.SetActive(true);
        walletScreenController.HideWalletUI();
    }

}
