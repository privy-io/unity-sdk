# SampleApp/

A Unity project that demonstrates SDK usage. It is not a published artifact — its purpose is to exercise the public API and serve as a reference for consumers.

## Structure

```
Assets/Scripts/
  AuthScreenController.cs       # Email/OAuth login UI
  WalletController.cs           # Wallet creation and RPC signing UI
  UIManager.cs / UIStateMachine.cs  # Screen navigation
  EnvConfig.cs / EnvFileReader.cs   # App ID + client ID from environment
Assets/Resources/EnvConfig.asset    # Environment configuration asset
Assets/Scenes/SampleScene.unity     # Main scene
```

## When to Update

Update SampleApp whenever the SDK's public API changes:

- New public interface or method added → add a usage example
- Method signature changed → update all call sites
- New auth method or wallet operation → add a demonstration flow

Search the `Assets/Scripts/` directory for usages of the old API when renaming or removing public members.
