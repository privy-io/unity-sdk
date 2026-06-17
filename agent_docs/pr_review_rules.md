# PR Review Rules

These are the architectural patterns and rules for the Privy Unity SDK. Any new code must adhere to these.

## Architecture (Blocking)

- **Interface + Implementation separation**: Every public service has an interface prefixed with `I` (e.g., `ILoginWithEmail`, `IPrivy`) and a separate `internal` implementation class (e.g., `LoginWithEmail`, `PrivyImpl`). Interfaces are `public`, implementations are `internal`.
- **Folder structure**: Features live in `SDK/Runtime/<Feature>/`. Interfaces and implementations live in the same feature folder. Models get a `Models/` subfolder within the feature.
- **Namespace per folder**: Each folder has its own namespace matching the path — `Privy.Auth.Email`, `Privy.Core`, `Privy.Wallets`, `Privy.Utils`, `Privy.Internal.Networking`, etc.
- **Internal namespaces**: Implementation-only types use `Privy.Internal.*` namespaces (e.g., `Privy.Internal.Networking`, `Privy.Internal.Storage`).

## Naming Conventions (Blocking)

- **Interfaces**: `I` prefix + PascalCase — `IPrivy`, `ILoginWithEmail`, `IHttpRequestHandler`, `IAuthDelegator`.
- **Implementation classes**: No prefix, PascalCase matching the concept — `PrivyImpl`, `LoginWithEmail`, `HttpRequestHandler`, `AuthDelegator`.
- **Private fields**: `_camelCase` — `_authDelegator`, `_httpRequestHandler`.
- **Public properties/methods**: PascalCase — `Email`, `GetUser()`, `AuthStateChanged`.
- **Constants**: PascalCase — `MaxRetries`, `ApiVersion`.
- **File names**: Match the type name — `ILoginWithEmail.cs`, `LoginWithEmail.cs`, `PrivyException.cs`.

## Access Control (Blocking)

- **Public interfaces only**: Only interfaces (`IPrivy`, `ILoginWithEmail`, etc.) and types needed by SDK consumers are `public`.
- **Internal implementations**: All implementation classes are `internal`. They must not be directly accessible to SDK consumers.
- **Public models/enums for consumers**: Data types returned to consumers (`AuthState`, `PrivyException`, `AuthenticationError` enum) are `public`.
- **Internal networking/storage**: `IHttpRequestHandler`, `PlayerPrefsDataManager`, repositories are `internal`.

## Dependency Injection (Blocking)

- **Constructor injection**: All dependencies are passed via constructor. No service locators or static singletons (except the `PrivyManager` entry point).
- **Wiring in PrivyImpl**: All service instantiation happens in `PrivyImpl`'s constructor. New services must be instantiated there with their dependencies.
- **Null-check constructor params**: Constructor parameters for required dependencies should include `?? throw new ArgumentNullException(nameof(param))`.
- **PrivyManager as entry point**: `PrivyManager.Initialize(config)` is the only way to create an SDK instance. No other public constructors.

## Async Patterns (Blocking)

- **Task-based async**: All async operations return `Task<T>`. Use `async/await` throughout.
- **TaskCompletionSource for initialization**: SDK initialization uses `TaskCompletionSource` to allow `GetAuthState()`/`GetUser()` to await readiness.
- **SafeFireAndForget for background work**: Fire-and-forget tasks use the `.SafeFireAndForget()` extension with error logging.
- **No blocking calls**: Never use `.Result` or `.Wait()` on tasks. Always `await`.

## Error Handling (Blocking)

- **Typed exceptions**: Use `PrivyAuthenticationException` (with `AuthenticationError` enum) for auth failures and `PrivyWalletException` (with `EmbeddedWalletError` enum) for wallet failures. Base class is `PrivyException`.
- **Error enums**: Add new error cases to the appropriate enum (`AuthenticationError` or `EmbeddedWalletError`) rather than using generic error messages.
- **No swallowed exceptions**: Every catch block must either rethrow (wrapped), log, or handle meaningfully. Never empty catch blocks.
- **Guard clauses**: Validate inputs at the top of methods with descriptive exceptions.

## Layered Architecture (Warning)

- **IPrivy → LoginWith* → AuthDelegator → AuthRepository → HttpRequestHandler**: Public API delegates to feature modules, which use the auth delegator, which calls repositories, which use the HTTP handler. Don't skip layers.
- **AuthDelegator for state management**: Authentication state changes flow through `AuthDelegator`. Modules should not directly mutate auth state.
- **Repositories for network calls**: Repositories (`AuthRepository`, `AppConfigRepository`) handle HTTP requests and deserialization. Business logic belongs in delegators/managers.

## Network Layer (Warning)

- **Use IHttpRequestHandler**: All API calls go through `IHttpRequestHandler.SendRequestAsync()`. Never use `UnityWebRequest` directly in feature code.
- **JSON serialization**: Use `JsonUtility` or the project's JSON approach consistently. Request/response models should be serializable.
- **Custom headers via parameter**: Pass additional headers (e.g., MFA tokens) through the `customHeaders` dictionary parameter, not by modifying the handler.

## Events (Warning)

- **C# events for state changes**: Use `event Action<T>` for state change notifications (e.g., `AuthStateChanged`). Forward events from internal components to the public interface.
- **No Unity-specific patterns in SDK core**: Don't use `UnityEvent`, `MonoBehaviour`, or coroutines in the SDK Runtime. Use standard C# async/await and events.

## Correctness & Logic (Warning)

- **No unhandled error paths**: Every `async Task` call site must handle exceptions. Don't let raw exceptions propagate past the delegator layer unhandled.
- **No `.Result` or `.Wait()`**: Never block on async tasks. This causes deadlocks in Unity's single-threaded synchronization context. Always `await`.
- **No sensitive data in logs**: Never log tokens, private keys, seed phrases, wallet addresses, or user credentials at any log level.
- **Dead code**: Flag unused methods, parameters, and imports. Remove them rather than leaving commented-out code.
- **Null reference safety**: Check for null before dereferencing objects from external sources (API responses, deserialized JSON, user input). Use null-conditional operators (`?.`) or explicit guards.
- **Race conditions in async code**: Ensure that state checks followed by state mutations don't have `await` points between them that could allow interleaving.
- **Guard against invalid inputs**: Validate inputs at method boundaries before performing work. Throw `ArgumentException`/`ArgumentNullException` early rather than proceeding with invalid state.
- **No redundant work**: Don't re-fetch data that's already available in scope. Don't re-compute values inside loops when they can be hoisted.
- **Dispose resources**: `IDisposable` resources (HTTP clients, streams, WebViews) must be disposed when no longer needed.
- **TaskCompletionSource safety**: Always set a result or exception on `TaskCompletionSource` — unresolved TCS instances will hang awaiting callers forever.

## Documentation (Nit)

- **XML docs on public types**: All `public` interfaces, methods, properties, and classes need `/// <summary>` documentation.
- **Document parameters**: Use `/// <param name="">` for method parameters.
- **Document exceptions**: Use `/// <exception cref="">` for thrown exceptions.
- **No over-documentation**: Internal implementation classes don't need XML docs unless behavior is non-obvious.

## Style (Nit)

- **Allman brace style**: Opening braces on a new line (enforced by `.editorconfig`).
- **4-space indentation**: No tabs.
- **Format with `dotnet format`**: Run `dotnet format Format.csproj` before committing.
- **Expression-bodied members**: Prefer for simple single-expression properties/methods.
- **Object/collection initializers**: Required (enforced as error in `.editorconfig`).

## Sample App (Nit)

- **Update SampleApp on public API changes**: When adding new public interfaces or changing method signatures, update `SampleApp/` to demonstrate usage.
