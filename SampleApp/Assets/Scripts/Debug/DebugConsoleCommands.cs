using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IngameDebugConsole;
using Newtonsoft.Json;
using Privy.Core;
using Privy.Auth;
using Privy.Auth.Models;
using Privy.Wallets;
using UnityEngine;

/// <summary>
/// In-game debug console commands for testing the Privy SDK.
/// All commands are static so they are auto-discovered by IngameDebugConsole's [ConsoleMethod] scanner.
/// Async operations use async void wrappers with try/catch to surface errors in the console.
/// </summary>
public static class DebugConsoleCommands
{
    // ──────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────

    private static async Task<IPrivyUser> GetUserOrThrow()
    {
        var user = await PrivyManager.Instance.GetUser();
        if (user == null)
            throw new Exception("Not logged in. Use privy.email.send / privy.email.login first.");
        return user;
    }

    private static SolanaCluster ResolveCluster(string name)
    {
        switch (name?.Trim().ToLowerInvariant())
        {
            case "mainnet": return SolanaCluster.Mainnet;
            case "testnet": return SolanaCluster.Testnet;
            case "devnet":
            default: return SolanaCluster.Devnet;
        }
    }

    // ──────────────────────────────────────────────
    //  Auth Commands
    // ──────────────────────────────────────────────

    [ConsoleMethod("privy.status", "Get current auth state and user ID if authenticated")]
    public static async void Status()
    {
        try
        {
            var state = await PrivyManager.Instance.GetAuthState();
            Debug.Log($"[privy.status] AuthState: {state}");

            if (state == AuthState.Authenticated)
            {
                var user = await PrivyManager.Instance.GetUser();
                Debug.Log($"[privy.status] User ID: {user?.Id ?? "N/A"}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[privy.status] {ex.Message}");
        }
    }

    [ConsoleMethod("privy.email.send", "Send an OTP code to an email address", "email")]
    public static async void EmailSendCode(string email)
    {
        try
        {
            bool success = await PrivyManager.Instance.Email.SendCode(email);
            Debug.Log($"[privy.email.send] Code sent to {email}: {(success ? "SUCCESS" : "FAILED")}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[privy.email.send] {ex.Message}");
        }
    }

    [ConsoleMethod("privy.email.login", "Login with email and OTP code", "email", "code")]
    public static async void EmailLogin(string email, string code)
    {
        try
        {
            var state = await PrivyManager.Instance.Email.LoginWithCode(email, code);
            Debug.Log($"[privy.email.login] Result: {state}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[privy.email.login] {ex.Message}");
        }
    }

    [ConsoleMethod("privy.sms.send", "Send an OTP code to a phone number (E.164 format)", "phone")]
    public static async void SmsSendCode(string phone)
    {
        try
        {
            bool success = await PrivyManager.Instance.Sms.SendCode(phone);
            Debug.Log($"[privy.sms.send] Code sent to {phone}: {(success ? "SUCCESS" : "FAILED")}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[privy.sms.send] {ex.Message}");
        }
    }

    [ConsoleMethod("privy.sms.login", "Login with phone number and OTP code", "phone", "code")]
    public static async void SmsLogin(string phone, string code)
    {
        try
        {
            var state = await PrivyManager.Instance.Sms.LoginWithCode(phone, code);
            Debug.Log($"[privy.sms.login] Result: {state}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[privy.sms.login] {ex.Message}");
        }
    }

    [ConsoleMethod("privy.logout", "Log out the current user")]
    public static void Logout()
    {
        try
        {
            PrivyManager.Instance.Logout();
            Debug.Log("[privy.logout] Logged out successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[privy.logout] {ex.Message}");
        }
    }

    [ConsoleMethod("privy.user", "Show current user info: ID, linked accounts, wallets")]
    public static async void UserInfo()
    {
        try
        {
            var user = await GetUserOrThrow();

            Debug.Log($"[privy.user] User ID: {user.Id}");
            Debug.Log($"[privy.user] Linked accounts: {user.LinkedAccounts.Length}");

            var ethWallets = user.EmbeddedEthereumWallets;
            Debug.Log($"[privy.user] Ethereum wallets: {ethWallets.Length}");
            for (int i = 0; i < ethWallets.Length; i++)
            {
                Debug.Log($"  [{i}] {ethWallets[i].Address} (HD index: {ethWallets[i].HdWalletIndex}, recovery: {ethWallets[i].RecoveryMethod})");
            }

            var solWallets = user.EmbeddedSolanaWallets;
            Debug.Log($"[privy.user] Solana wallets: {solWallets.Length}");
            for (int i = 0; i < solWallets.Length; i++)
            {
                Debug.Log($"  [{i}] {solWallets[i].Address} (wallet index: {solWallets[i].WalletIndex}, recovery: {solWallets[i].RecoveryMethod})");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[privy.user] {ex.Message}");
        }
    }

    // ──────────────────────────────────────────────
    //  Ethereum Wallet Commands
    // ──────────────────────────────────────────────

    [ConsoleMethod("privy.eth.create", "Create a primary Ethereum embedded wallet")]
    public static async void EthCreateWallet()
    {
        try
        {
            var user = await GetUserOrThrow();
            var wallet = await user.CreateWallet(allowAdditional: false);
            Debug.Log($"[privy.eth.create] Wallet created: {wallet.Address}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[privy.eth.create] {ex.Message}");
        }
    }

    [ConsoleMethod("privy.eth.create_additional", "Create an additional Ethereum embedded wallet")]
    public static async void EthCreateAdditionalWallet()
    {
        try
        {
            var user = await GetUserOrThrow();
            var wallet = await user.CreateWallet(allowAdditional: true);
            Debug.Log($"[privy.eth.create_additional] Wallet created: {wallet.Address}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[privy.eth.create_additional] {ex.Message}");
        }
    }

    [ConsoleMethod("privy.eth.create_at_index", "Create an Ethereum wallet at a specific HD index", "hdIndex")]
    public static async void EthCreateWalletAtIndex(int hdIndex)
    {
        try
        {
            var user = await GetUserOrThrow();
            var wallet = await user.CreateWalletAtHdIndex(hdWalletIndex: hdIndex);
            Debug.Log($"[privy.eth.create_at_index] Wallet created: {wallet.Address} (HD index: {wallet.HdWalletIndex})");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[privy.eth.create_at_index] {ex.Message}");
        }
    }

    [ConsoleMethod("privy.eth.wallets", "List all Ethereum embedded wallets")]
    public static async void EthListWallets()
    {
        try
        {
            var user = await GetUserOrThrow();
            var wallets = user.EmbeddedEthereumWallets;

            if (wallets.Length == 0)
            {
                Debug.Log("[privy.eth.wallets] No Ethereum wallets found.");
                return;
            }

            Debug.Log($"[privy.eth.wallets] {wallets.Length} wallet(s):");
            for (int i = 0; i < wallets.Length; i++)
            {
                Debug.Log($"  [{i}] {wallets[i].Address} | HD index: {wallets[i].HdWalletIndex} | Recovery: {wallets[i].RecoveryMethod}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[privy.eth.wallets] {ex.Message}");
        }
    }

    [ConsoleMethod("privy.eth.sign", "Sign a message with an Ethereum wallet (personal_sign)", "walletIndex", "message")]
    public static async void EthPersonalSign(int walletIndex, string message)
    {
        try
        {
            var user = await GetUserOrThrow();
            var wallets = user.EmbeddedEthereumWallets;
            if (walletIndex < 0 || walletIndex >= wallets.Length)
                throw new Exception($"Wallet index {walletIndex} out of range (0..{wallets.Length - 1}).");

            var wallet = wallets[walletIndex];
            var rpcRequest = new RpcRequest
            {
                Method = "personal_sign",
                Params = new[] { message, wallet.Address }
            };

            var response = await wallet.RpcProvider.Request(rpcRequest);
            Debug.Log($"[privy.eth.sign] Signature: {response.Data}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[privy.eth.sign] {ex.Message}");
        }
    }

    [ConsoleMethod("privy.eth.sign_typed", "Sign EIP-712 typed data (hardcoded test payload)", "walletIndex")]
    public static async void EthSignTypedData(int walletIndex)
    {
        try
        {
            var user = await GetUserOrThrow();
            var wallets = user.EmbeddedEthereumWallets;
            if (walletIndex < 0 || walletIndex >= wallets.Length)
                throw new Exception($"Wallet index {walletIndex} out of range (0..{wallets.Length - 1}).");

            var wallet = wallets[walletIndex];

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

            var rpcRequest = new RpcRequest
            {
                Method = "eth_signTypedData_v4",
                Params = new[] { wallet.Address, encodedParam }
            };

            var response = await wallet.RpcProvider.Request(rpcRequest);
            Debug.Log($"[privy.eth.sign_typed] Signature: {response.Data}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[privy.eth.sign_typed] {ex.Message}");
        }
    }

    [ConsoleMethod("privy.eth.sign_tx", "Sign a hardcoded test transaction (Sepolia)", "walletIndex")]
    public static async void EthSignTransaction(int walletIndex)
    {
        try
        {
            var user = await GetUserOrThrow();
            var wallets = user.EmbeddedEthereumWallets;
            if (walletIndex < 0 || walletIndex >= wallets.Length)
                throw new Exception($"Wallet index {walletIndex} out of range (0..{wallets.Length - 1}).");

            var wallet = wallets[walletIndex];
            string transactionJson =
                $"{{\"from\":\"{wallet.Address}\",\"to\":\"0x742D35Cc6634C0532925A3b844BC9e7095F49e22\",\"value\":\"0x9184e72a000\",\"chainId\":11155111}}";

            var rpcRequest = new RpcRequest
            {
                Method = "eth_signTransaction",
                Params = new[] { transactionJson }
            };

            var response = await wallet.RpcProvider.Request(rpcRequest);
            Debug.Log($"[privy.eth.sign_tx] Signed transaction: {response.Data}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[privy.eth.sign_tx] {ex.Message}");
        }
    }

    // ──────────────────────────────────────────────
    //  Solana Wallet Commands
    // ──────────────────────────────────────────────

    [ConsoleMethod("privy.sol.create", "Create a primary Solana embedded wallet")]
    public static async void SolCreateWallet()
    {
        try
        {
            var user = await GetUserOrThrow();
            var wallet = await user.CreateSolanaWallet(allowAdditional: false);
            Debug.Log($"[privy.sol.create] Wallet created: {wallet.Address}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[privy.sol.create] {ex.Message}");
        }
    }

    [ConsoleMethod("privy.sol.create_additional", "Create an additional Solana embedded wallet")]
    public static async void SolCreateAdditionalWallet()
    {
        try
        {
            var user = await GetUserOrThrow();
            var wallet = await user.CreateSolanaWallet(allowAdditional: true);
            Debug.Log($"[privy.sol.create_additional] Wallet created: {wallet.Address}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[privy.sol.create_additional] {ex.Message}");
        }
    }

    [ConsoleMethod("privy.sol.wallets", "List all Solana embedded wallets")]
    public static async void SolListWallets()
    {
        try
        {
            var user = await GetUserOrThrow();
            var wallets = user.EmbeddedSolanaWallets;

            if (wallets.Length == 0)
            {
                Debug.Log("[privy.sol.wallets] No Solana wallets found.");
                return;
            }

            Debug.Log($"[privy.sol.wallets] {wallets.Length} wallet(s):");
            for (int i = 0; i < wallets.Length; i++)
            {
                Debug.Log($"  [{i}] {wallets[i].Address} | Wallet index: {wallets[i].WalletIndex} | Recovery: {wallets[i].RecoveryMethod}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[privy.sol.wallets] {ex.Message}");
        }
    }

    [ConsoleMethod("privy.sol.sign_message", "Sign a Base64-encoded message with a Solana wallet", "walletIndex", "base64Message")]
    public static async void SolSignMessage(int walletIndex, string base64Message)
    {
        try
        {
            var user = await GetUserOrThrow();
            var wallets = user.EmbeddedSolanaWallets;
            if (walletIndex < 0 || walletIndex >= wallets.Length)
                throw new Exception($"Wallet index {walletIndex} out of range (0..{wallets.Length - 1}).");

            // Default test message: "A message to sign"
            if (string.IsNullOrWhiteSpace(base64Message))
                base64Message = "QSBtZXNzYWdlIHRvIHNpZ24=";

            var signature = await wallets[walletIndex].EmbeddedSolanaWalletProvider.SignMessage(base64Message);
            Debug.Log($"[privy.sol.sign_message] Signature: {signature}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[privy.sol.sign_message] {ex.Message}");
        }
    }

    [ConsoleMethod("privy.sol.sign_tx", "Sign a Solana transaction (Base64) without broadcasting", "walletIndex", "base64Transaction")]
    public static async void SolSignTransaction(int walletIndex, string base64Transaction)
    {
        try
        {
            var user = await GetUserOrThrow();
            var wallets = user.EmbeddedSolanaWallets;
            if (walletIndex < 0 || walletIndex >= wallets.Length)
                throw new Exception($"Wallet index {walletIndex} out of range (0..{wallets.Length - 1}).");

            var signedTx = await wallets[walletIndex].EmbeddedSolanaWalletProvider.SignTransaction(base64Transaction);
            Debug.Log($"[privy.sol.sign_tx] Signed transaction (Base64): {signedTx}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[privy.sol.sign_tx] {ex.Message}");
        }
    }

    [ConsoleMethod("privy.sol.sign_send_tx", "Sign and send a Solana transaction (Base64) on Devnet", "walletIndex", "base64Transaction")]
    public static async void SolSignAndSendTransaction(int walletIndex, string base64Transaction)
    {
        try
        {
            var user = await GetUserOrThrow();
            var wallets = user.EmbeddedSolanaWallets;
            if (walletIndex < 0 || walletIndex >= wallets.Length)
                throw new Exception($"Wallet index {walletIndex} out of range (0..{wallets.Length - 1}).");

            var txHash = await wallets[walletIndex].EmbeddedSolanaWalletProvider
                .SignAndSendTransaction(base64Transaction, SolanaCluster.Devnet);
            Debug.Log($"[privy.sol.sign_send_tx] Transaction hash: {txHash}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[privy.sol.sign_send_tx] {ex.Message}");
        }
    }

    [ConsoleMethod("privy.sol.sign_send_tx_cluster", "Sign and send a Solana transaction on a specific cluster", "walletIndex", "base64Transaction", "cluster (mainnet/devnet/testnet)")]
    public static async void SolSignAndSendTransactionCluster(int walletIndex, string base64Transaction, string cluster)
    {
        try
        {
            var user = await GetUserOrThrow();
            var wallets = user.EmbeddedSolanaWallets;
            if (walletIndex < 0 || walletIndex >= wallets.Length)
                throw new Exception($"Wallet index {walletIndex} out of range (0..{wallets.Length - 1}).");

            var resolvedCluster = ResolveCluster(cluster);
            Debug.Log($"[privy.sol.sign_send_tx_cluster] Using cluster: {resolvedCluster.Caip2} ({resolvedCluster.RpcUrl})");

            var txHash = await wallets[walletIndex].EmbeddedSolanaWalletProvider
                .SignAndSendTransaction(base64Transaction, resolvedCluster);
            Debug.Log($"[privy.sol.sign_send_tx_cluster] Transaction hash: {txHash}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[privy.sol.sign_send_tx_cluster] {ex.Message}");
        }
    }
}
