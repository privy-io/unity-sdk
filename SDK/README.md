# Privy Unity SDK

Welcome to the Privy Unity SDK! This SDK provides a set of tools and
functionalities for integrating Privy services into your Unity projects. The
package can be consumed via the Unity Package Manager, .unitypackage import,
OpenUPM or by copying the contents of the `SDK/` folder into your project's
`Assets/` directory. The SDK bundles third-party libraries (Newtonsoft.Json, jsoncanonicalizer,
and unity-webview) inside `SDK/ExternalDependencies`. No external package
references are required; consumers simply import the SDK and the dependencies
are included automatically.

## Contributing

If you are contributing to the Privy SDK codebase, **read
[CONTRIBUTING.md](./CONTRIBUTING.md) instead**.

## Prerequisites

- Unity Editor **2022.3.42f1**

## Importing the SDK

1. Download the `.unitypackage` file.
2. Open Your Unity Project.
3. Import the `.unitypackage` package from the `Assets` menu
   (`Import Package` > `CustomPackage ...`).
4. Confirm the Import to bring the SDK into your project.
5. Verify the SDK installation. You should see the Privy SDK components
   appear in your project’s `Assets` folder.

## Usage

### Initialization

```csharp
var config = new PrivyConfig{
    AppId = "YOUR_APP_ID",
    ClientId = "CLIENT_ID"
};

PrivyManager.Initialize(config);
```

### Check user's authentication state

When the Privy SDK is initialized, it will automatically begin to load its necessary
dependencies and restoring user session data.
By awaiting on `GetAuthState` you can ensure the SDK will be ready to use in full
after returning.

```csharp
var authState = await PrivyManager.Instance.GetAuthState();

switch (authState) {
    case AuthState.Authenticated:
        // User is authenticated. Grab the user's linked accounts
        var privyUser = await PrivyManager.Instance.GetUser();
        var linkedAccounts = privyUser.LinkedAccounts;
        break;
    case AuthState.Unauthenticated:
        // User is not authenticated.
        break;
}
```

### Use the Email Auth Module

```csharp
bool success = await PrivyManager.Instance.Email.SendCode(email);

if (success) {
    // OTP send successfully, update UI to retrieve code from user
} else {
    // There was an error sending OTP.
}
```

```csharp
try {
    // User will be authenticated if this call is successful
    await PrivyManager.Instance.Email.LoginWithCode(email, code);
} catch {
    // If "LoginWithCode" throws an exception, user login was unsuccessful.
    Debug.Log("Error logging user in.");
}
```

### PrivyUser

```csharp
PrivyUser user = PrivyManager.Instance.User;
```

### Creating an Embedded Wallet

```csharp
try {
    PrivyUser privyUser = PrivyManager.Instance.User;

    if (privyUser != null) {
        IEmbeddedWallet wallet = await PrivyManager.Instance.User.CreateWallet();
        Debug.Log("New wallet created with address: " + wallet.address);
    }
} catch {
    Debug.Log("Error creating embedded wallet.");
}
```

### Triggering an RPC Request

```csharp
try {
    IEmbeddedWallet embeddedWallet = PrivyManager.Instance.User.EmbeddedWallets[0];

    var rpcRequest = new RpcRequest
    {
        Method = "personal_sign",
        Params = new string[]
        {
            "A message to sign",
            embeddedWallet.Address
        }
    };

    RpcResponse personalSignResponse = await embeddedWallet.RpcProvider.Request(rpcRequest);

    Debug.Log(personalSignResponse.Data);
} catch (PrivyException.EmbeddedWalletException ex){
    Debug.LogError($"Could not sign message due to error: {ex.Error} {ex.Message}");
} catch (Exception ex) {
    Debug.LogError($"Could not sign message exception {ex.Message}");
}
```

## Build Settings

### WebGL Build Requirements

To ensure the Privy SDK functions correctly in WebGL builds, especially for our
iframe implementation, there are specific settings that need to be configured:

1. Set the WebGL Template:
   - Navigate to `Edit > Project Settings > Player`.
   - Under the **WebGL** tab, find the **Resolution and Presentation** section.
   - Set the **WebGL Template** to the custom template included in the `.unitypackage`.
     - unity-webview-2020, for 2020 and newer versions
     - unity-webview for older versions
   - **Why is this necessary?**
     - Unity’s default WebGL template creates an instance of `unityInstance` but
       doesn’t expose it globally.
     - In our custom template, we attach the `unityInstance` to the `window`
       object, allowing external JavaScript to interact with Unity.
       This setup is required for enabling programmatic message sending from
       JavaScript to Unity using `unityInstance.SendMessage()`.
2. **Update the Code Stripping Level:**
   - Navigate to `Edit > Project Settings > Player`.
   - Under the **Other Settings** section in the WebGL tab,
     locate **Code Stripping Level**.
   - Set the **Code Stripping Level** to **Minimal** from the dropdown menu.
   - **Why is this necessary?**
     - Unity’s code stripping feature can remove unused methods and constructors
       to reduce build size.
       This includes default constructors of classes that are only used indirectly,
       such as during JSON deserialization.
     - Setting the Code Stripping Level to **Minimal** ensures that these
       constructors and other necessary code aren’t stripped,
       preventing runtime errors in WebGL builds.

By following these steps, you ensure that your WebGL builds are correctly configured
to work with the Privy SDK, particularly in scenarios where you need to send messages
from JavaScript to Unity using our iframe implementation.
