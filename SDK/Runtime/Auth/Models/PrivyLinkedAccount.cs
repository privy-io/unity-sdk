using System;
using JetBrains.Annotations;

namespace Privy.Auth.Models
{
    /// <summary>
    /// Represents the various kinds of linked accounts a user may have.
    /// </summary>
    public enum LinkedAccountType
    {
        Email,
        Phone,
        Google,
        Apple,
        Discord,
        Twitter,
        EmbeddedEthereum,
        EmbeddedSolana,
        ExternalWallet,
        Unknown
    }

    public class PrivyLinkedAccount
    {
        /// <summary>
        /// The kind of linked account.
        /// </summary>
        public LinkedAccountType Type { get; set; } = LinkedAccountType.Unknown;

        /// <summary>
        /// When the account was verified (UTC).
        /// </summary>
        public DateTimeOffset VerifiedAt { get; set; }

        /// <summary>
        /// First time the account was verified (UTC).
        /// </summary>
        public DateTimeOffset FirstVerifiedAt { get; set; }

        /// <summary>
        /// Most recent verification time (UTC).
        /// </summary>
        public DateTimeOffset LatestVerifiedAt { get; set; }
    }

    //Privy Embedded Wallet Account
    public class PrivyEmbeddedWalletAccount : PrivyLinkedAccount
    {
        [CanBeNull]
        public string Id { get; set; }

        public string Address { get; set; }

        public bool Imported { get; set; }

        public int WalletIndex { get; set; }

        public string ChainId { get; set; }

        public string ChainType { get; set; }

        public string WalletClient { get; set; }

        public string WalletClientType { get; set; }

        public string ConnectorType { get; set; }

        public string PublicKey { get; set; }

        public string RecoveryMethod { get; set; }

        internal bool IsOnDevice => Id == null || RecoveryMethod != "privy-v2";
    }

    /// <summary>
    /// An embedded Solana wallet account linked to the user.
    /// </summary>
    public class PrivyEmbeddedSolanaWalletAccount : PrivyLinkedAccount
    {
        public string ChainType => "solana";

        [CanBeNull]
        public string Id { get; set; }

        /// <summary>
        /// The embedded wallet's address, as the base58 encoded public key.
        /// </summary>
        public string Address { get; set; }

        public int WalletIndex { get; set; }

        public bool Imported { get; set; }

        public string RecoveryMethod { get; set; }

        internal bool IsOnDevice => Id == null || RecoveryMethod != "privy-v2";
    }

    /// <summary>
    /// An external wallet account linked to the user.
    /// </summary>
    public class ExternalWalletAccount : PrivyLinkedAccount
    {
        public string Address { get; set; }

        public string ChainType { get; set; }

        public string WalletClientType { get; set; }

        public string ConnectorType { get; set; }
    }

    public class PrivyEmailAccount : PrivyLinkedAccount
    {
        public string Address { get; set; }
    }

    /// <summary>
    /// A phone (SMS) account linked to the user.
    /// </summary>
    public class PrivyPhoneAccount : PrivyLinkedAccount
    {
        /// <summary>
        /// The phone number in E.164 format (e.g. "+15551234567").
        /// </summary>
        public string PhoneNumber { get; set; }
    }

    public class GoogleAccount : PrivyLinkedAccount
    {
        public string Subject { get; set; }

        public string Email { get; set; }

        public string Name { get; set; }
    }

    public class DiscordAccount : PrivyLinkedAccount
    {
        public string Subject { get; set; }

        public string Email { get; set; }

        public string UserName { get; set; }
    }

    public class TwitterAccount : PrivyLinkedAccount
    {
        public string Subject { get; set; }

        public string UserName { get; set; }

        public string Name { get; set; }

        public string ProfilePictureUrl { get; set; }
    }

    public class AppleAccount : PrivyLinkedAccount
    {
        public string Subject { get; set; }

        public string Email { get; set; }
    }
}
