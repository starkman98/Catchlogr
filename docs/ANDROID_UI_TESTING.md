# Android UI testing

Appium drives the actual Android MAUI app: agents can inspect its accessibility
tree, find existing XAML `AutomationId` values, enter text, tap controls, and save
screenshots. This supplements portable Core/DevHost tests. It does not establish
physical camera/GPS behavior, all device layouts, or Google Play release readiness.
These are optional local checks; the validation helper and CI do not run them.

## Windows prerequisites

Use native Windows PowerShell for these examples, from the repository root.
Install the .NET SDK selected by `global.json`, JDK 21, and an Appium-compatible
Node.js/npm version (the Linux baseline below used Node 26). Install the Android
SDK through Android Studio or Visual Studio's Android SDK Manager. Include:

- Android SDK Command-line Tools (latest), Platform-Tools, and Android Emulator;
- Android SDK Platform 36 and Build-Tools 36.0.0;
- Google APIs Android 36 x86_64 system image.

Enable CPU virtualization in BIOS/UEFI and **Windows Hypervisor Platform** in
Windows Features, then reboot if required. Windows uses WHPX, not `/dev/kvm`.
Follow the official [emulator acceleration instructions](https://developer.android.com/studio/run/emulator-acceleration)
and [MAUI installation instructions](https://learn.microsoft.com/dotnet/maui/get-started/installation).
The setup below assumes an Intel/AMD Windows PC; ARM hosts need a matching image
and app runtime instead of the x86_64 examples.

Set these variables in **each terminal** used for the tools, or save the commands
in a local profile. Adjust the SDK/JDK paths to the installed locations. Android
Studio commonly uses the SDK path below; Visual Studio commonly uses
`C:\Program Files (x86)\Android\android-sdk`.

```powershell
$env:ANDROID_HOME = "$env:LOCALAPPDATA\Android\Sdk"
# Replace this example with the actual JDK 21 directory containing bin\java.exe.
$env:JAVA_HOME = 'C:\Program Files\Microsoft\jdk-21'
$env:PATH = "$env:JAVA_HOME\bin;$env:ANDROID_HOME\platform-tools;$env:ANDROID_HOME\emulator;$env:ANDROID_HOME\cmdline-tools\latest\bin;$env:PATH"
java -version
dotnet --version
node --version
npm --version
emulator -accel-check
dotnet workload install maui-android
```

Some installations require an administrator to install workloads or enable
Windows features; daily emulator/Appium use does not require granting an agent
administrator rights. Review and accept SDK licenses in the SDK Manager or with
`sdkmanager --licenses`. If installing the SDK packages from the terminal:

```powershell
sdkmanager 'platform-tools' 'emulator' 'platforms;android-36' 'build-tools;36.0.0' 'system-images;android-36;google_apis;x86_64'
avdmanager create avd --name catchlogr-api36 --package 'system-images;android-36;google_apis;x86_64' --device pixel_6
```

Answer `no` if asked to create a custom hardware profile. Alternatively create
the same named Pixel 6/API 36 Google APIs x86_64 device in Device Manager.

## Start Android and Appium on Windows

In one terminal, start the emulator. Omit `-no-window` to watch the agent's
interactions in a desktop window:

```powershell
emulator -avd catchlogr-api36 -port 5554 -no-window -no-audio -no-boot-anim -gpu swiftshader -accel on -memory 2048 -cores 2 -no-snapshot -no-metrics
```

In another terminal, install the known Linux smoke-test Appium/driver pair and
start the server. Pinning avoids accidentally pairing Appium 3 with a driver
requiring Appium 4. See [Appium setup](https://appium.io/docs/en/3.0/quickstart/).

```powershell
npm install --global appium@3.9.0
appium driver install uiautomator2@8.7.3
appium --address 127.0.0.1 --port 4723
```

If Appium already has another UiAutomator2 version, inspect `appium driver list
--installed` before replacing it; do not overwrite another project's setup
blindly. Keep Appium on localhost. An agent needs tools that can call its
WebDriver HTTP API; starting the emulator alone does not grant UI access.

In your working terminal, wait until both readiness checks succeed:

```powershell
adb devices
adb -s emulator-5554 shell getprop sys.boot_completed
Invoke-RestMethod http://127.0.0.1:4723/status
```

Expect `emulator-5554 device`, boot completion `1`, and Appium `ready: true`.
Avoid duplicate emulator/Appium processes or port conflicts.

## Build and install a standalone Local APK

The temporary import below restricts **only Mobile** to Android so restore does
not require Apple/Windows workloads. Do not pass a global `TargetFrameworks`
override across the mixed project graph. This replaces normal
`Directory.Build.targets` discovery: revisit it if that file is added later.

```powershell
$androidTargets = Join-Path $env:TEMP 'Catchlogr.AndroidOnly.targets'
@'
<Project>
  <PropertyGroup Condition="'$(MSBuildProjectName)' == 'Catchlogr.Mobile'">
    <TargetFrameworks>net10.0-android</TargetFrameworks>
  </PropertyGroup>
</Project>
'@ | Set-Content -LiteralPath $androidTargets

dotnet build src/Catchlogr.Mobile/Catchlogr.Mobile.csproj -f net10.0-android -c Local -p:RuntimeIdentifier=android-x64 "-p:DirectoryBuildTargetsPath=$androidTargets" -p:AndroidPackageFormats=apk -p:EmbedAssembliesIntoApk=true "-p:AndroidSdkDirectory=$env:ANDROID_HOME" "-p:JavaSdkDirectory=$env:JAVA_HOME"
if ($LASTEXITCODE -ne 0) { throw 'Android build failed' }
adb -s emulator-5554 install --no-incremental -r src/Catchlogr.Mobile/bin/Local/net10.0-android/android-x64/com.catchlogr.app-Signed.apk
```

`EmbedAssembliesIntoApk=true` is necessary for direct APK installation here:
without it, Local fast deployment can leave an APK that aborts with **No
assemblies found** because it expects IDE-deployed assemblies. Rebuild/reinstall
after changes. The Local backend and emulator host address are documented in
[Mobile configuration](MOBILE_CONFIGURATION.md#local-api-addresses). This build
does not start the API or apply migrations.

## Exercise the UI from PowerShell or an agent

The following minimal check uses only PowerShell and Appium's HTTP API. Run on
the dedicated emulator while Catchlogr is signed out. It checks startup, blank
login validation, and navigation without sending credentials or reset requests.
It saves a screenshot and accessibility XML to a temporary directory.

```powershell
$base = 'http://127.0.0.1:4723'
$caps = @{ capabilities = @{ alwaysMatch = @{
    platformName = 'Android'
    'appium:automationName' = 'UiAutomator2'
    'appium:udid' = 'emulator-5554'
    'appium:appPackage' = 'com.catchlogr.app'
    'appium:appActivity' = 'crc6453834c20859ee6d5.MainActivity'
    'appium:noReset' = $true
    'appium:newCommandTimeout' = 300
} } } | ConvertTo-Json -Depth 8
$session = Invoke-RestMethod "$base/session" -Method Post -ContentType 'application/json' -Body $caps
$uri = "$base/session/$($session.value.sessionId)"
function Wait-UiText([string] $text) {
    $deadline = (Get-Date).AddSeconds(45)
    do {
        $xml = (Invoke-RestMethod "$uri/source").value
        if ($xml.Contains($text)) { return }
        Start-Sleep -Seconds 1
    } while ((Get-Date) -lt $deadline)
    throw "Expected UI text missing: $text"
}
function Click-UiId([string] $id) {
    $xpath = '//*[@resource-id="{0}" or @resource-id="com.catchlogr.app:id/{0}" or @content-desc="{0}"]' -f $id
    $body = @{ using = 'xpath'; value = $xpath } | ConvertTo-Json
    $found = Invoke-RestMethod "$uri/element" -Method Post -ContentType 'application/json' -Body $body
    $element = $found.value.'element-6066-11e4-a52e-4f735466cecf'
    Invoke-RestMethod "$uri/element/$element/click" -Method Post -ContentType 'application/json' -Body '{}' | Out-Null
}
try {
    Wait-UiText 'LoginEmailEntry'
    Click-UiId 'LoginButton'
    Wait-UiText 'Enter your email address and password.'
    Click-UiId 'OpenForgotPasswordButton'
    Wait-UiText 'ForgotPasswordEmailEntry'
    Click-UiId 'ForgotPasswordOpenLoginButton'
    Wait-UiText 'LoginEmailEntry'
    $artifacts = Join-Path $env:TEMP 'Catchlogr.AndroidSmoke'
    New-Item -ItemType Directory -Force $artifacts | Out-Null
    (Invoke-RestMethod "$uri/source").value | Set-Content (Join-Path $artifacts 'login.xml')
    # Capture through ADB: avoids the intermittent Appium screenshot failure below
    # and binary stdout redirection differences in Windows PowerShell 5.1.
    adb -s emulator-5554 shell screencap -p /sdcard/catchlogr-smoke.png
    if ($LASTEXITCODE -ne 0) { throw 'Screenshot capture failed' }
    adb -s emulator-5554 pull /sdcard/catchlogr-smoke.png (Join-Path $artifacts 'login.png')
    if ($LASTEXITCODE -ne 0) { throw 'Screenshot download failed' }
    Write-Output "PASS: validation and navigation; artifacts in $artifacts"
} finally {
    try { Invoke-RestMethod $uri -Method Delete | Out-Null }
    catch { Write-Warning "Session cleanup failed: $($_.Exception.Message)" }
}
```

If MainActivity's generated name changes, obtain it using
`adb -s emulator-5554 shell cmd package resolve-activity --brief com.catchlogr.app`
and update the capability. Future authenticated scenarios must use a dedicated
test account/API/database and verify persisted results after restart/sync.
Successful login, trip/catch creation and synchronization are **not** covered by
this signed-out smoke check. Real API tests can create persistent server data;
follow the [local setup](../README.md#local-setup) and use isolated test data.

Stop Appium with Ctrl+C and stop Android with
`adb -s emulator-5554 emu kill`. The GUI emulator can also be closed normally.

## Linux agent-server baseline and troubleshooting

Observed on 2026-11-08: JDK 21, .NET SDK 10.0.302 with `maui-android`, emulator
37.2.13, API 36 Google APIs x86_64, platform-tools **36.0.2**, Appium **3.9.0**,
and UiAutomator2 **8.7.3**. KVM boot, the standalone Local APK, empty-login
validation, password-recovery navigation/back, and screenshot capture passed.
The Android build had zero errors and five XC0025 binding warnings. Windows
instructions above require a local run; they are not a recorded Windows pass.

On the existing agent-server, reusable tools live outside the checkout:

```bash
source ~/.local/share/catchlogr-android/env.sh
emulator -accel-check
# Each start command runs in its own terminal; check for existing instances first.
~/.local/share/catchlogr-android/start-emulator.sh
~/.local/share/catchlogr-android/start-appium.sh
~/.local/share/catchlogr-android/build-local.sh
# After installing the APK as described in the local README:
python3 ~/.local/share/catchlogr-android/ui-smoke.py
```

That machine's `~/.local/share/catchlogr-android/README.md` documents its paths,
logs, and startup commands. Those files are machine-local, not supplied by this
checkout. The SDK is under `~/Android/Sdk`; Java and missing Linux emulator
libraries were extracted under the user tool directory without system installs.

- **KVM permissions:** the Proxmox host must allow nested virtualization and
  expose CPU virtualization to the VM. Inside the VM, `/dev/kvm` must exist and
  the emulator user must have read/write access, normally through the `kvm`
  group. An administrator adds membership; agents need no sudo rights. Reconnect
  SSH and restart existing IDE/agent services to refresh inherited groups.
- **Sandbox visibility:** an Orca terminal can access KVM while an agent's
  sandbox cannot see the device. Use the supported host execution approval
  mechanism; do not disable safeguards. Terminal access alone does not establish
  that an agent process has device access.
- **ADB drops/screenshots:** platform-tools 37.0.1 repeatedly disconnected during
  the Linux Appium run. An initial smoke run passed after switching to 36.0.2,
  but a later Appium screenshot request still lost the connection on 36.0.2.
  The version change is not an established fix or a Windows requirement. The
  PowerShell example uses ADB screenshot capture instead. Check `adb version`,
  avoid competing ADB installations, and inspect Appium/emulator logs if the
  device goes offline. Retest after SDK updates; one passing smoke run does not
  establish long-running reliability.
- **Driver mismatch:** UiAutomator2 9.0.1 required Appium 4 beta in this setup;
  stable Appium 3 was verified with driver 8.7.3 instead.
- **Slow setup:** the initial concurrent installations saturated VM disk I/O.
  Install sequentially if needed; this does not measure normal emulator speed.
