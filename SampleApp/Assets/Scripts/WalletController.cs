using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;
using Privy.Core;
using Privy.Wallets;
using Privy.Auth.Models;
using Privy.Utils;

public class WalletController : MonoBehaviour
{
    public GameObject walletUI;
    public Button createWalletButton;
    public Button personalSignButton;
    public Button signedTypeDataV4Button;
    public Button signTransactionButton;
    public Button backButton;
    
    public Button createSolanaWalletButton;
    public Toggle allowAdditionalSolanaWalletsToggle;
    public Button solanaSignButton;
    public TextMeshProUGUI solanaSignature;

    public Toggle allowAdditionalWalletsToggle;

    public TextMeshProUGUI walletAddress;
    public TextMeshProUGUI signature;
    public TextMeshProUGUI typedV4Sig;

    public TMP_Dropdown walletsDropdown;
    public TMP_Dropdown solanaWalletsDropdown;

    public Button createWalletWithIndexButton;
    public TMP_InputField hdWalletIndexInput;

    private IPrivyUser _privyUser;

    private void Awake()
    {
        createWalletButton.onClick.AddListener(OnCreateWalletButtonClick);
        createSolanaWalletButton.onClick.AddListener(OnCreateSolanaWalletButtonClick);
        personalSignButton.onClick.AddListener(OnPersonalSignButtonClick);
        signedTypeDataV4Button.onClick.AddListener(OnSignedTypeDataV4ButtonClick);
        signTransactionButton.onClick.AddListener(OnSignTransactionButtonClick);
        backButton.onClick.AddListener(OnBackButtonClick);
        walletsDropdown.onValueChanged.AddListener(SelectWalletDropdownOption);
        createWalletWithIndexButton.onClick.AddListener(OnCreateWalletWithIndex);
        
        solanaSignButton.onClick.AddListener(OnSolanaSignButtonClick);
    }

    private async void OnCreateWalletButtonClick()
    {
        try
        {
            IPrivyUser privyUser = await PrivyManager.Instance.GetUser();

            if (privyUser != null)
            {
                bool allowAdditionalWallets = allowAdditionalWalletsToggle.isOn;
                IEmbeddedEthereumWallet wallet = await privyUser.CreateWallet(allowAdditionalWallets);
                Debug.Log("New wallet created with address: " + wallet.Address);

                RefreshWalletDropdownOptions();
                // Select the last entry, as it is the newest.
                walletsDropdown.value = privyUser.EmbeddedEthereumWallets.Length - 1;
            }
        }
        catch (PrivyWalletException ex)
        {
            Debug.LogError($"Could not create wallet due to error: {ex.Error} {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Could not create wallet: {ex.Message}");
        }
    }

    private async void OnPersonalSignButtonClick()
    {
        try
        {
            IEmbeddedEthereumWallet embeddedWallet = SelectedWallet;

            var rpcRequest = new RpcRequest
            {
                Method = "personal_sign",
                Params = new string[] { "A message to sign", embeddedWallet.Address }
            };

            RpcResponse personalSignResponse = await embeddedWallet.RpcProvider.Request(rpcRequest);

            Debug.Log("Personal Sign Response: " + personalSignResponse.Data);
            signature.text = "Last sig:" + personalSignResponse.Data;
        }
        catch (PrivyWalletException ex)
        {
            Debug.LogError($"Could not sign message due to error: {ex.Error} {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Could not sign message: {ex.Message}");
        }
    }

    private async void OnSignedTypeDataV4ButtonClick()
    {
        try
        {
            // Data Initialization
            var param = new Param
            {
                Types = new ParamTypes
                {
                    EIP712Domain = new List<DomainType>
                        {
                            new DomainType { Name = "name", Type = "string" },
                            new DomainType { Name = "version", Type = "string" },
                            new DomainType { Name = "chainId", Type = "uint256" },
                            new DomainType { Name = "verifyingContract", Type = "address" }
                        },
                    Person = new List<DomainType>
                        {
                            new DomainType { Name = "name", Type = "string" },
                            new DomainType { Name = "wallet", Type = "address" }
                        },
                    Mail = new List<DomainType>
                        {
                            new DomainType { Name = "from", Type = "Person" },
                            new DomainType { Name = "to", Type = "Person" },
                            new DomainType { Name = "contents", Type = "string" }
                        }
                },
                PrimaryType = "Mail",
                Domain = new Domain
                    {
                        Name = "Ether Mail",
                        Version = "1",
                        ChainId = 1,
                        VerifyingContract = "0xCcCCccccCCCCcCCCCCCcCcCccCcCCCcCcccccccC"
                    },
                Message = new Message
                    {
                        From = new Message.W { Name = "Cow", Wallet = "0xCD2a3d9F938E13CD947Ec05AbC7FE734Df8DD826" },
                        To = new Message.W { Name = "Bob", Wallet = "0xbBbBBBBbbBBBbbbBbbBbbbbBBbBbbbbBbBbbBBbB" },
                        Contents = "Hello, Bob!"
                    }
            };

            string encodedParam = JsonConvert.SerializeObject(param);

            Debug.Log("Encoded Param: " + encodedParam);

            IEmbeddedEthereumWallet embeddedWallet = SelectedWallet;

            var rpcRequest = new RpcRequest
            {
                Method = "eth_signTypedData_v4",
                Params = new string[] { embeddedWallet.Address, encodedParam }
            };

            RpcResponse signedTypeDataV4Response = await embeddedWallet.RpcProvider.Request(rpcRequest);

            Debug.Log("Signed Type Data V4 Response: " + signedTypeDataV4Response.Data);
            typedV4Sig.text = "Last sig:" + signedTypeDataV4Response.Data;
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to sign typed data: " + ex.Message);
        }
    }

    private async void OnSignTransactionButtonClick()
    {
        try
        {
            var address = SelectedWallet.Address;
            string transactionJson =
                $"{{\"from\":\"{address}\",\"to\":\"0x742D35Cc6634C0532925A3b844BC9e7095F49e22\",\"value\":\"0x9184e72a000\",\"chainId\":11155111}}";

            var rpcRequest = new RpcRequest
            {
                Method = "eth_signTransaction",
                Params = new string[] { transactionJson }
            };

            RpcResponse signTransactionResponse = await SelectedWallet.RpcProvider.Request(rpcRequest);

            Debug.Log("Signed Transaction Response: " + signTransactionResponse.Data);
            typedV4Sig.text = "Last sig:" + signTransactionResponse.Data;
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to sign transaction: " + ex.Message);
        }
    }

    private async void OnCreateSolanaWalletButtonClick()
    {
        try
        {
            var user = await PrivyManager.Instance.GetUser();

            var allowAdditionalWallets = allowAdditionalSolanaWalletsToggle.isOn;
            var wallet = await user!.CreateSolanaWallet(allowAdditionalWallets);
            Debug.Log("New SOL wallet created with address: " + wallet.Address);

            RefreshWalletDropdownOptions();
            // Select the last entry, as it is the newest.
            solanaWalletsDropdown.value = user.EmbeddedSolanaWallets.Length - 1;
        }
        catch (PrivyWalletException ex)
        {
            Debug.LogError($"Could not create wallet due to error: {ex.Error} {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Could not create wallet: {ex.Message}");
        }
    }

    private async void OnSolanaSignButtonClick()
    {
        try
        {
            var wallet = SelectedSolanaWallet;

            var signature = await wallet.EmbeddedSolanaWalletProvider.SignMessage("QSBtZXNzYWdlIHRvIHNpZ24=");

            Debug.Log("Solana Sign Response: " + signature);
            solanaSignature.text = "Last sig: " + signature;
        }
        catch (PrivyWalletException ex)
        {
            Debug.LogError($"Could not sign message due to error: {ex.Error} {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Could not sign message: {ex.Message}");
        }
    }

    private void OnBackButtonClick()
    {
        UIManager.Instance.GoBack();
    }

    private IEmbeddedEthereumWallet SelectedWallet {
        get {
            int selectedWalletIndex = walletsDropdown.value;
            var embeddedWallets = _privyUser.EmbeddedEthereumWallets;
            return embeddedWallets[selectedWalletIndex];
        }
    }

    private IEmbeddedSolanaWallet SelectedSolanaWallet {
        get
        {
            int index = solanaWalletsDropdown.value;
            return _privyUser.EmbeddedSolanaWallets[index];
        }
    }

    private void SelectWalletDropdownOption(int index)
    {
        IEmbeddedEthereumWallet embeddedWallet = _privyUser.EmbeddedEthereumWallets[index];
        if ( embeddedWallet != null) {
            walletAddress.text = "Address:" + embeddedWallet.Address;
        }
    }

    private void RefreshWalletDropdownOptions()
    {
        List<string> walletAddresses = _privyUser.EmbeddedEthereumWallets.Select(wallet =>
            $"{wallet.Address.Substring(0, 6)}...{wallet.Address.Substring(wallet.Address.Length - 4)}").ToList();
        walletsDropdown.ClearOptions();
        walletsDropdown.AddOptions(walletAddresses);

        List<string> solanaWalletAddresses = _privyUser.EmbeddedSolanaWallets.Select(wallet =>
            $"{wallet.Address.Substring(0, 6)}...{wallet.Address.Substring(wallet.Address.Length - 4)}").ToList();
        solanaWalletsDropdown.ClearOptions();
        solanaWalletsDropdown.AddOptions(solanaWalletAddresses);
    }
    
    private async void OnCreateWalletWithIndex()
    {
        try
        {
            // Convert input to an int
            int hdWalletIndex = int.Parse(hdWalletIndexInput.text);

            if (_privyUser != null)
            {
                Debug.Log($"Attempting to create HD wallet at index: {hdWalletIndex}");
                IEmbeddedEthereumWallet wallet = await _privyUser.CreateWalletAtHdIndex(hdWalletIndex: hdWalletIndex);
                
                Debug.Log($"Wallet received with address: {wallet.Address} and index: {wallet.HdWalletIndex}");

                RefreshWalletDropdownOptions();
                // Select the last entry, as it is the newest.
                walletsDropdown.value = _privyUser.EmbeddedEthereumWallets.Length - 1;
            }
        }
        catch (PrivyWalletException ex)
        {
            Debug.LogError($"Could not create wallet due to error: {ex.Error} {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Could not create wallet: {ex.Message}");
        }
    }

    /// <summary>
    /// Lifecycle callback invoked by UIManager when the wallet screen becomes visible.
    /// Fetches fresh user and wallet data, then updates the display.
    /// </summary>
    public async void OnWalletScreenShown()
    {
        try
        {
            IPrivyUser user = await PrivyManager.Instance.GetUser();
            if (user == null)
            {
                Debug.LogError("Must be logged in to access the wallet.");
                UIManager.Instance.GoBack();
                return;
            }

            _privyUser = user;
            IEmbeddedEthereumWallet embeddedWallet = user.EmbeddedEthereumWallets.FirstOrDefault();
            walletAddress.text = embeddedWallet != null
                ? "Address:" + embeddedWallet.Address
                : "No wallets";

            RefreshWalletDropdownOptions();
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to load wallet data: " + ex.Message);
        }
    }

    /// <summary>
    /// Lifecycle callback invoked by UIManager when the wallet screen is hidden.
    /// </summary>
    public void OnWalletScreenHidden()
    {
        // Cleanup when leaving the wallet screen (if needed).
    }

    // Backward-compatibility wrappers — prefer UIManager.Instance.ShowWalletUI() / GoBack()
    public void ShowWalletUI() => UIManager.Instance.ShowWalletUI();
    public void HideWalletUI() => walletUI.SetActive(false);
}
