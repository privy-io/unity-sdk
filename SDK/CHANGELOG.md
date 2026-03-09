# Changelog

## Unreleased

## [0.9.0-rc.2](https://github.com/privy-io/unity-sdk/compare/v0.9.0-rc.1...v0.9.0-rc.2) (2026-03-09)


### Fixed

* alice ([5bcdc6f](https://github.com/privy-io/unity-sdk/commit/5bcdc6f65b177dad510c22d52746503dd6c4bd0e))

## 0.7.1 - 2026-01-27

### Fixed

- Fix support for null "verified at" fields.

## 0.7.0 - 2026-01-15

### Addded

- Support for reading the IdentityToken via `user.GetIdentityToken()`.

### Fixed

- Improves support for non-iOS compatible setups

## 0.6.0 - 2025-08-01

### Added

- (internal) Adds support for the `privy:user-signer:sign` iframe RPC
- Adds an `Id` field to `PrivyEmbeddedWalletAccount`
- Adds an `Id` field to `PrivyEmbeddedSolanaWalletAccount`
- (internal) Loads the Application config object
- (internal) Supports wallet creation in TEE execution apps
- Adds a new dependency on the `jsoncanonicalizer` library
- (internal) Supports ETH personal_sign RPC in TEE wallets
- (internal) Supports ETH eth_sign RPC in TEE wallets
- (internal) Supports ETH secp256k1_sign RPC in TEE wallets
- Adds support for the `secp256k1_sign` ethereum RPC
- (internal) Explicitly skip support for the ETH eth_populateTransactionRequest RPC in TEE wallets
- (internal) Supports ETH eth_signTypedData_v4 RPC in TEE wallets
- (internal) Supports ETH eth_sendTransaction RPC in TEE wallets
- (internal) Supports ETH eth_signTransaction RPC in TEE wallets
- (internal) Supports SOL signMessage RPC in TEE wallets
- Adds `Privy.GetAuthState()`
- Adds `Privy.GetUser()`

### Changed

- Deprecates `PrivyManager.AwaitReady()` in favor of `Privy.GetAuthState()`
- Deprecates `Privy.AuthState` in favor of `Privy.GetAuthState()`
- Deprecates `Privy.User` in favor of `Privy.GetUser()`

## 0.5.1 - 2025-07-11

### Added

- Adds support for OAuth when using the Unity Editor

## 0.5.0 - 2025-05-07

### Added

- Added the `PrivyUser.GetAccessToken` method.

## 0.4.0 - 2025-04-14

### Added

- Added the `PrivyEmbeddedSolanaWalletAccount` linked account class.
- Added the `LinkedAccount[].EmbeddedSolanaWalletAccounts()` extension method.
- Added the `PrivyUser.EmbeddedSolanaWallets` array property.
- Added the `IEmbeddedSolanaWalletProvider.SignMessage` method.
- Added the `PrivyUser.CreateSolanaWallet` method.
- Added Twitter as an OAuth provider.

### Changed

- (internal) migrated iframe rpcs to use the new chain-agnostic variants.
- BREAKING: changed the associated string values on the `PrivyEventType` enum.
- BREAKING: updated the `CreateAdditionalWalletRequestData` class to match the chain-agnostic interface.
- BREAKING: updated the `ConnectWalletRequestData` class to match the chain-agnostic interface.
- BREAKING: updated the `RecoverWalletRequestData` class to match the chain-agnostic interface.
- BREAKING: updated the `RpcRequestData` class to match the chain-agnostic interface.
- BREAKING: updated the `ConnectWalletResponseData` class to match the chain-agnostic interface.
- BREAKING: updated the `RecoverWalletResponseData` class to match the chain-agnostic interface.

### Deprecated

- Deprecates the `IEmbeddedWallet` interface, renamed to `IEmbeddedEthereumWallet`.

## 0.3.4 - 2025-03-18

### Fixed

- Runtime error when looking for the `version.txt` file

## 0.3.3 - 2025-03-07

### Added

- SDK init analytics
- Add client analytics ID header to network requests
- Add client header to networks requests

## 0.3.2 - 2025-03-07

### Added

- Defined a new linked account type, ExternalWalletAccount

## 0.3.1 - 2024-11-18

### Fixed

- Only require JS extern methods on WebGL platform

## 0.3.0 - 2024-11-07

### Added

- Create HD wallet with specified index

## 0.2.1 - 2024-10-25

### Added

- Login with OAuth (WebGL support)

### Fixed

- Only run iOS post process build on iOS platform

## 0.2.0 - 2024-10-15

### Added

- Login with Apple (iOS/Android support only, web-based)
- Native Login with Apple for iOS

## 0.1.0 - 2024-10-09

### Added

- Login with Google (iOS/Android support only)
- Login with Discord (iOS/Android support only)
- "Privy" namespace to all Privy classes
- Compiler flags to exclude Windows / Linux

### Changed

- Renamed "IEmail" interface to "ILoginWithEmail" to match our other SDKs.
- Restructured folders so Privy SDK is self-contained in Plugins/Privy

## 0.0.1

### Added

- Login with email
- Create wallet
- Create additional wallet
- Personal sign / sign transaction
