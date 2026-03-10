using System;
using Privy.Auth.Models;
using Privy.Utils;

namespace Privy.Auth.Mapping
{
    internal static class LinkedAccountResponseMapper
    {
        private static LinkedAccountType ParseType(string type)
        {
            if (string.IsNullOrEmpty(type))
                return LinkedAccountType.Unknown;

            // normalize common server representations which may use underscores or hyphens
            string normalized = type.Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "embedded_ethereum":
                case "embeddedethereum":
                case "embedded-ethereum":
                    return LinkedAccountType.EmbeddedEthereum;
                case "embedded_solana":
                case "embeddedsolana":
                case "embedded-solana":
                    return LinkedAccountType.EmbeddedSolana;
                case "external_wallet":
                case "externalwallet":
                case "external-wallet":
                    return LinkedAccountType.ExternalWallet;
                default:
                    break;
            }

            // fall back to case‑insensitive enum parse for other values
            if (Enum.TryParse<LinkedAccountType>(type, true, out var result))
                return result;

            return LinkedAccountType.Unknown;
        }

        private static DateTimeOffset FromUnixEpoch(long epoch)
        {
            if (epoch <= 0)
                return default;
            // guess if the value is milliseconds or seconds
            if (epoch > 1_000_000_000_000) // approx > 2001-09-09 in ms
                return DateTimeOffset.FromUnixTimeMilliseconds(epoch);
            return DateTimeOffset.FromUnixTimeSeconds(epoch);
        }

        public static PrivyLinkedAccount MapToPublic(LinkedAccountResponse response)
        {
            PrivyLogger.Debug($"Mapping response type: {response.GetType().Name}");

            if (response is WalletAccountResponse walletResponse)
            {
                var isEmbeddedWallet = walletResponse.WalletClientType == "privy" &&
                                       walletResponse.ConnectorType == "embedded";

                if (!isEmbeddedWallet)
                {
                    return new ExternalWalletAccount
                    {
                        Type = ParseType(walletResponse.Type),
                        Address = walletResponse.Address,
                        VerifiedAt = FromUnixEpoch(walletResponse.VerifiedAt),
                        FirstVerifiedAt = FromUnixEpoch(walletResponse.FirstVerifiedAt),
                        LatestVerifiedAt = FromUnixEpoch(walletResponse.LatestVerifiedAt),
                        ChainType = walletResponse.ChainType,
                        WalletClientType = walletResponse.WalletClientType,
                        ConnectorType = walletResponse.ConnectorType
                    };
                }

                if (walletResponse.ChainType == "solana")
                {
                    return new PrivyEmbeddedSolanaWalletAccount
                    {
                        Id = walletResponse.Id,
                        Type = ParseType(walletResponse.Type),
                        Address = walletResponse.Address,
                        VerifiedAt = FromUnixEpoch(walletResponse.VerifiedAt),
                        FirstVerifiedAt = FromUnixEpoch(walletResponse.FirstVerifiedAt),
                        LatestVerifiedAt = FromUnixEpoch(walletResponse.LatestVerifiedAt),
                        Imported = walletResponse.Imported,
                        WalletIndex = walletResponse.WalletIndex,
                        RecoveryMethod = walletResponse.RecoveryMethod
                    };
                }

                return new PrivyEmbeddedWalletAccount
                {
                    Id = walletResponse.Id,
                    Type = ParseType(walletResponse.Type),
                    Address = walletResponse.Address,
                    VerifiedAt = FromUnixEpoch(walletResponse.VerifiedAt),
                    FirstVerifiedAt = FromUnixEpoch(walletResponse.FirstVerifiedAt),
                    LatestVerifiedAt = FromUnixEpoch(walletResponse.LatestVerifiedAt),
                    Imported = walletResponse.Imported,
                    WalletIndex = walletResponse.WalletIndex,
                    ChainId = walletResponse.ChainId,
                    ChainType = walletResponse.ChainType,
                    WalletClient = walletResponse.WalletClient,
                    WalletClientType = walletResponse.WalletClientType,
                    ConnectorType = walletResponse.ConnectorType,
                    PublicKey = walletResponse.PublicKey,
                    RecoveryMethod = walletResponse.RecoveryMethod
                };
            }
            else if (response is EmailAccountResponse emailResponse)
            {
                return new PrivyEmailAccount
                {
                    Type = ParseType(emailResponse.Type),
                    Address = emailResponse.Address,
                    VerifiedAt = FromUnixEpoch(emailResponse.VerifiedAt),
                    FirstVerifiedAt = FromUnixEpoch(emailResponse.FirstVerifiedAt),
                    LatestVerifiedAt = FromUnixEpoch(emailResponse.LatestVerifiedAt)
                };
            }
            else if (response is PhoneAccountResponse phoneResponse)
            {
                return new PrivyPhoneAccount
                {
                    Type = ParseType(phoneResponse.Type),
                    PhoneNumber = phoneResponse.PhoneNumber,
                    VerifiedAt = FromUnixEpoch(phoneResponse.VerifiedAt),
                    FirstVerifiedAt = FromUnixEpoch(phoneResponse.FirstVerifiedAt),
                    LatestVerifiedAt = FromUnixEpoch(phoneResponse.LatestVerifiedAt)
                };
            }
            else if (response is GoogleOAuthAccountResponse googleOAuthAccountResponse)
            {
                return new GoogleAccount
                {
                    Subject = googleOAuthAccountResponse.Subject,
                    Email = googleOAuthAccountResponse.Email,
                    Name = googleOAuthAccountResponse.Name,
                    Type = ParseType(googleOAuthAccountResponse.Type),
                    VerifiedAt = FromUnixEpoch(googleOAuthAccountResponse.VerifiedAt),
                    FirstVerifiedAt = FromUnixEpoch(googleOAuthAccountResponse.FirstVerifiedAt),
                    LatestVerifiedAt = FromUnixEpoch(googleOAuthAccountResponse.LatestVerifiedAt)
                };
            }
            else if (response is DiscordOAuthAccountResponse discordOAuthAccountResponse)
            {
                return new DiscordAccount
                {
                    Subject = discordOAuthAccountResponse.Subject,
                    Email = discordOAuthAccountResponse.Email,
                    UserName = discordOAuthAccountResponse.UserName,
                    Type = ParseType(discordOAuthAccountResponse.Type),
                    VerifiedAt = FromUnixEpoch(discordOAuthAccountResponse.VerifiedAt),
                    FirstVerifiedAt = FromUnixEpoch(discordOAuthAccountResponse.FirstVerifiedAt),
                    LatestVerifiedAt = FromUnixEpoch(discordOAuthAccountResponse.LatestVerifiedAt)
                };
            }
            else if (response is TwitterOAuthAccountResponse twitterOAuthAccountResponse)
            {
                return new TwitterAccount
                {
                    Subject = twitterOAuthAccountResponse.Subject,
                    UserName = twitterOAuthAccountResponse.UserName,
                    Name = twitterOAuthAccountResponse.Name,
                    ProfilePictureUrl = twitterOAuthAccountResponse.ProfilePictureUrl,
                    Type = ParseType(twitterOAuthAccountResponse.Type),
                    VerifiedAt = FromUnixEpoch(twitterOAuthAccountResponse.VerifiedAt),
                    FirstVerifiedAt = FromUnixEpoch(twitterOAuthAccountResponse.FirstVerifiedAt),
                    LatestVerifiedAt = FromUnixEpoch(twitterOAuthAccountResponse.LatestVerifiedAt)
                };
            }
            else if (response is AppleOAuthAccountResponse appleOAuthAccountResponse)
            {
                return new AppleAccount
                {
                    Subject = appleOAuthAccountResponse.Subject,
                    Email = appleOAuthAccountResponse.Email,
                    Type = ParseType(appleOAuthAccountResponse.Type),
                    VerifiedAt = FromUnixEpoch(appleOAuthAccountResponse.VerifiedAt),
                    FirstVerifiedAt = FromUnixEpoch(appleOAuthAccountResponse.FirstVerifiedAt),
                    LatestVerifiedAt = FromUnixEpoch(appleOAuthAccountResponse.LatestVerifiedAt)
                };
            }
            else
            {
                PrivyLogger.Debug("Mapping to base type PrivyLinkedAccount");
                return null;
            }
        }
    }
}
