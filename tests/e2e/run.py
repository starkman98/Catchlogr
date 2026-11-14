#!/usr/bin/env python3
"""Run isolated Catchlogr authentication and sync E2E on a Linux Android host."""
import argparse
import fcntl
import json
import os
from pathlib import Path
import secrets
import shutil
import signal
import ssl
import tempfile
import uuid

from host import run, certificates, require_free_ports, wait_http
from android_host import prepare_android
from recording import record_android

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
PACKAGE = "com.catchlogr.e2e"


def execute(args, artifacts):
    """Own one disposable Compose project and clean it on success or failure."""
    print("Checking dedicated E2E ports and prerequisites...", flush=True)
    require_free_ports()
    env = os.environ.copy()
    env["E2E_DEVICE"] = args.device
    env["E2E_ARTIFACTS"] = str(artifacts)
    adb = None
    if not args.backend_only:
        adb = prepare_android(args.device, env)
    with tempfile.TemporaryDirectory(prefix="catchlogr-e2e-certs-") as temporary:
        certs = Path(temporary)
        certificates(certs)
        env["E2E_CERT_DIRECTORY"] = str(certs)
        env["E2E_DB_PASSWORD"] = secrets.token_urlsafe(32)
        project = "catchlogr-e2e-" + uuid.uuid4().hex[:12]
        compose = ["docker", "compose", "--project-name", project,
                   "--file", str(ROOT / "deploy/e2e/compose.yml")]
        env["E2E_COMPOSE_PROJECT"] = project
        try:
            run(*compose, "up", "--build", "--detach", env=env)
            print("Waiting for API, Web, and inbox readiness...", flush=True)
            wait_http("http://127.0.0.1:15001/health")
            wait_http("https://localhost:17056/confirm-email", ssl.create_default_context(cafile=str(certs / "ca.crt")))
            wait_http("http://127.0.0.1:18025/api/v1/info")
            command = ["node", str(HERE / "auth-smoke.mjs")]
            if args.backend_only:
                command.append("--backend-only")
            with record_android(adb, args.device, artifacts, env.get("E2E_RECORD_VIDEO") == "1"):
                run(*command, env=env)
        finally:
            # Restrict cleanup to the generated project and our separate app package.
            try:
                if adb:
                    run(adb, "-s", args.device, "shell", "am", "force-stop", PACKAGE)
                    run(adb, "-s", args.device, "shell", "pm", "clear", PACKAGE)
            finally:
                run(*compose, "down", "--volumes", "--remove-orphans", env=env)


def interrupt(_signal, _frame):
    """Turn CI termination into normal stack disposal through finally blocks."""
    raise KeyboardInterrupt


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--backend-only", action="store_true", help="Exercise API/email/Web without Android")
    parser.add_argument("--device", default="emulator-5554", help="Dedicated emulator to receive the E2E package")
    args = parser.parse_args()
    signal.signal(signal.SIGTERM, interrupt)
    for tool in ("docker", "openssl", "node", "dotnet"):
        if not shutil.which(tool):
            parser.error(f"Missing prerequisite: {tool}")
    if not (HERE / "node_modules/playwright").is_dir():
        parser.error("Run npm ci --prefix tests/e2e and install Playwright Chromium first")
    base = ROOT / "artifacts/e2e"
    base.mkdir(parents=True, exist_ok=True)
    with (base / "run.lock").open("w") as lock:
        fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        artifacts = base / uuid.uuid4().hex
        artifacts.mkdir(mode=0o700)
        try:
            execute(args, artifacts)
        except (Exception, KeyboardInterrupt) as error:
            # Do not expose subprocess environments, email links, or response bodies.
            print(f"E2E failed ({type(error).__name__}). See the failing step above.", flush=True)
            if isinstance(error, RuntimeError):
                print(str(error), flush=True)
            (artifacts / "result.json").write_text(json.dumps({"passed": False, "backendOnly": args.backend_only}))
            return 130 if isinstance(error, KeyboardInterrupt) else 1
        (artifacts / "result.json").write_text(json.dumps({"passed": True, "backendOnly": args.backend_only}))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
