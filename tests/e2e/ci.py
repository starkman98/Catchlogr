#!/usr/bin/env python3
"""Own emulator/Appium processes started for one dedicated-host CI invocation."""
import os
from pathlib import Path
import signal
import subprocess
import sys
import time
import urllib.request

from host import ROOT, run, wait_http


def appium_ready():
    """Detect an already running Appium server without restarting it."""
    try:
        with urllib.request.urlopen("http://127.0.0.1:4723/status", timeout=2) as response:
            return response.status == 200
    except OSError:
        return False


def emulator_ready():
    """Inspect only the fixed dedicated emulator serial."""
    result = subprocess.run(["adb", "-s", "emulator-5554", "shell", "getprop", "sys.boot_completed"],
                            stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, text=True, timeout=10)
    return result.returncode == 0 and result.stdout.strip() == "1"


def start_hosts(processes, log):
    """Reuse live hosts or start owned processes with bounded readiness waits."""
    if not emulator_ready():
        processes.append(subprocess.Popen([
            "emulator", "-avd", os.environ.get("E2E_AVD", "catchlogr-api36"), "-port", "5554",
            "-no-window", "-no-audio", "-no-boot-anim", "-gpu", "swiftshader", "-accel", "on",
            "-memory", "2048", "-cores", "2", "-no-snapshot", "-no-metrics",
        ], stdout=log, stderr=log, start_new_session=True))
        deadline = time.monotonic() + 180
        while not emulator_ready():
            if time.monotonic() > deadline:
                raise RuntimeError("Dedicated emulator did not boot within 180 seconds")
            time.sleep(2)
    if not appium_ready():
        processes.append(subprocess.Popen([
            "appium", "--address", "127.0.0.1", "--port", "4723", "--log-level", "warn", "--log-no-colors",
        ], stdout=log, stderr=log, start_new_session=True))
        wait_http("http://127.0.0.1:4723/status")


def stop_hosts(processes):
    """Stop only child process groups created by this invocation."""
    for process in reversed(processes):
        if process.poll() is not None:
            continue
        os.killpg(process.pid, signal.SIGTERM)
        try:
            process.wait(timeout=15)
        except subprocess.TimeoutExpired:
            os.killpg(process.pid, signal.SIGKILL)
            process.wait()


def interrupt(_signal, _frame):
    """Unwind owned hosts when Actions terminates the wrapper."""
    raise KeyboardInterrupt


def main():
    """Run the same local E2E entry point with CI host lifecycle management."""
    signal.signal(signal.SIGTERM, interrupt)
    processes = []
    logs = ROOT / "artifacts/e2e-host"
    logs.mkdir(parents=True, exist_ok=True, mode=0o700)
    # Host logs stay local; CI uploads only result.json and redacted UI hierarchy.
    with (logs / "host.log").open("w") as log:
        try:
            start_hosts(processes, log)
            run(sys.executable, str(Path(__file__).with_name("run.py")), "--device", "emulator-5554",
                interrupt_timeout=120)
        finally:
            stop_hosts(processes)


if __name__ == "__main__":
    main()
