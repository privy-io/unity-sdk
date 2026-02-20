# How to contribute to Privy Unity SDK

For questions or support, email <support@privy.io>.

## Formatting

This project uses [`dotnet format`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-format)
with an `.editorconfig` to enforce a consistent C# code style.
CI will reject pull requests that contain unformatted code.

**Set up the pre-commit hook** (auto-formats staged files on every commit):

```bash
git config core.hooksPath .githooks
```

**Format manually:**

```bash
dotnet format Format.csproj
```

**Check without modifying files:**

```bash
dotnet format Format.csproj --verify-no-changes
```

## Native Plugins

If you are working with native platform plugins (e.g. iOS Objective-C), see
[docs/native-code.md](docs/native-code.md) for detailed guidance on bridging native
and managed code.

## Setting Up and Running the Sample App

1. **Clone Repo**

   ```bash
     git clone https://github.com/privy-io/unity-sdk.git
   ```

2. Open Unity Hub
3. Add the Sample App Project.
   - In Unity Hub, select the **Add** button to add a new project.
   - Navigate to the `unity-sdk/SampleApp` directory and select this project.
4. Select the Sample Scene.
   - In the Unity Editor, locate the **Project** window.
   - Navigate to the `Assets > Scenes` folder.
   - Double-click the `SampleScene` file to open it.
5. Run the Scene.
   - Once the sample scene is open, press the **Play** button at the top of the
     Unity Editor to run the scene.
   - This will launch the sample app within the Unity Editor, allowing you to test
     the SDK functionality.
