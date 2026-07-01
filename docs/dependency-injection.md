# Dependency Injection

The SDK uses manual constructor injection. There is no DI framework.

## Wiring

All service instantiation happens in `PrivyImpl`'s constructor. When adding a new service, instantiate it there and pass its dependencies explicitly. No service locators or static helpers beyond `PrivyManager`.

## Constructor params

Required dependencies must be null-checked:

```csharp
_authDelegator = authDelegator ?? throw new ArgumentNullException(nameof(authDelegator));
```

## Entry point

`PrivyManager.Initialize(config)` is the only way to create an SDK instance — no other public constructors exist. It creates `PrivyImpl`, which owns the full dependency graph.
