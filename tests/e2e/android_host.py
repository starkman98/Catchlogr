"""Build and install Catchlogr's disposable Android app on a dedicated emulator."""
from pathlib import Path
from host import ROOT, run, wait_http

HERE = Path(__file__).resolve().parent
PACKAGE = "com.catchlogr.e2e"


def prepare_android(device, env):
    """Build and install only the E2E application on an Android emulator."""
    if not device.startswith("emulator-"):
        raise ValueError("E2E runner accepts emulator serials only")
    android_sdk = Path(env.get("ANDROID_HOME", ""))
    java = env.get("JAVA_HOME")
    if not java or not (android_sdk / "platform-tools" / "adb").is_file():
        raise RuntimeError("Set JAVA_HOME and ANDROID_HOME using the Android host setup")
    adb = str(android_sdk / "platform-tools" / "adb")
    booted = run(adb, "-s", device, "shell", "getprop", "sys.boot_completed", capture_output=True, text=True)
    if booted.stdout.strip() != "1":
        raise RuntimeError("Start the dedicated Android emulator and wait for boot")
    wait_http("http://127.0.0.1:4723/status")
    build_android(android_sdk, java, env)
    install_android(adb, device, env)
    return adb


def build_android(android_sdk, java, env):
    """Compile the fixed Local E2E identity using the installed SDKs."""
    run("dotnet", "build", "src/Catchlogr.Mobile/Catchlogr.Mobile.csproj",
        "-f", "net10.0-android", "-c", "Local", "-p:RuntimeIdentifier=android-x64",
        f"-p:DirectoryBuildTargetsPath={HERE / 'android-only.targets'}",
        "-p:AndroidPackageFormats=apk", "-p:EmbedAssembliesIntoApk=true",
        f"-p:AndroidSdkDirectory={android_sdk}", f"-p:JavaSdkDirectory={java}", env=env)


def install_android(adb, device, env):
    """Install and reset only the E2E package and resolve its launch activity."""
    apk = ROOT / "src/Catchlogr.Mobile/bin/Local/net10.0-android/android-x64/com.catchlogr.e2e-Signed.apk"
    if not apk.is_file():
        raise RuntimeError("Build did not produce the E2E package")
    run(adb, "-s", device, "install", "--no-incremental", "-r", str(apk))
    run(adb, "-s", device, "shell", "am", "force-stop", PACKAGE)
    run(adb, "-s", device, "shell", "pm", "clear", PACKAGE)
    activity = run(adb, "-s", device, "shell", "cmd", "package", "resolve-activity", "--brief", PACKAGE,
                   capture_output=True, text=True).stdout.strip().splitlines()[-1]
    if not activity.startswith(PACKAGE + "/"):
        raise RuntimeError("Could not resolve the E2E application activity")
    env["E2E_ACTIVITY"] = activity.split("/", 1)[1]
