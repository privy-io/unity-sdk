using System;

namespace Privy.Utils
{
    public class PrivyException : Exception
    {
        public PrivyException(string message) : base(message) { }
        public PrivyException(string message, Exception inner) : base(message, inner) { }
    }

    public class PrivyAuthenticationException : PrivyException
    {
        public AuthenticationError Error { get; }
        public PrivyAuthenticationException(string message, AuthenticationError error) : base(message)
        {
            Error = error;
        }
        public PrivyAuthenticationException(string message, AuthenticationError error, Exception inner) : base(message, inner)
        {
            Error = error;
        }
    }

    public class PrivyWalletException : PrivyException
    {
        public EmbeddedWalletError Error { get; }
        public PrivyWalletException(string message, EmbeddedWalletError error) : base(message)
        {
            Error = error;
        }
        public PrivyWalletException(string message, EmbeddedWalletError error, Exception inner) : base(message, inner)
        {
            Error = error;
        }
    }

    public enum AuthenticationError
    {
        SendCodeFailed,
        EmailEmpty,
        OtpEmpty,
        CodeChallengeEmpty,
        RedirectUriEmpty,
        StateCodeEmpty,
        AuthorizationCodeEmpty,
        CodeVerifierEmpty,
        InvalidOAuthResult,
        RefreshFailed,
        WrongOtpCode,
        OAuthInitFailed,
        OAuthVerificationFailed,
        OAuthAuthenticateFailed,
        NotAuthenticated,
        PhoneNumberEmpty,
        InvalidPhoneNumber,
        LinkFailed,
        UnlinkFailed,
        IncorrectOtpCode,
        InternalError
    }

    public enum EmbeddedWalletError
    {
        WalletDoesNotExist,
        ConnectionFailed,
        CreateFailed,
        CreateAdditionalFailed,
        RecoverFailed,
        RpcRequestFailed,
        UserSignerRequestFailed,
        MaxWalletsCreated,
    }
}
