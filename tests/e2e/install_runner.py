#!/usr/bin/env python3
"""Install/register the dedicated Actions runner after the host owner approves."""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import tarfile
import urllib.request

REPOSITORY = "starkman98/Catchlogr"


def github(endpoint, method="GET"):
    """Read runner metadata without printing authentication material."""
    return json.loads(subprocess.check_output([
        "gh", "api", "--method", method, f"repos/{REPOSITORY}/{endpoint}",
    ]))


def install(root):
    """Verify the archive checksum and register with no generic runner labels."""
    root.mkdir(mode=0o700, parents=True, exist_ok=True)
    if (root / ".runner").exists():
        raise RuntimeError("Runner already configured; refusing to replace its registration")
    downloads = github("actions/runners/downloads")
    item = next(entry for entry in downloads if entry["os"] == "linux" and entry["architecture"] == "x64")
    archive = root / "runner.tar.gz"
    urllib.request.urlretrieve(item["download_url"], archive)
    if hashlib.sha256(archive.read_bytes()).hexdigest() != item["sha256_checksum"]:
        raise RuntimeError("Runner archive checksum mismatch")
    with tarfile.open(archive) as package:
        package.extractall(root, filter="data")
    archive.unlink()
    token = github("actions/runners/registration-token", "POST")["token"]
    # Keep the short-lived registration token out of command arguments and logs.
    env = {**os.environ, "ACTIONS_RUNNER_INPUT_TOKEN": token}
    subprocess.run([
        str(root / "config.sh"), "--unattended", "--url", f"https://github.com/{REPOSITORY}",
        "--name", "catchlogr-agent-server-e2e", "--no-default-labels", "--labels", "catchlogr-e2e",
        "--work", "_work",
    ], cwd=root, env=env, check=True)


def service(root):
    """Persist the dedicated runner using the current user's systemd manager."""
    units = Path.home() / ".config/systemd/user"
    units.mkdir(parents=True, exist_ok=True)
    unit = units / "catchlogr-e2e-runner.service"
    unit.write_text(f"""[Unit]
Description=Catchlogr dedicated Android E2E Actions runner
After=network-online.target

[Service]
WorkingDirectory={root}
ExecStart={root}/run.sh
Restart=on-failure
RestartSec=10
TimeoutStopSec=120

[Install]
WantedBy=default.target
""")
    subprocess.run(["systemctl", "--user", "daemon-reload"], check=True)
    subprocess.run(["systemctl", "--user", "enable", "--now", unit.name], check=True)


if __name__ == "__main__":
    destination = Path.home() / ".local/share/catchlogr-actions-runner"
    install(destination)
    service(destination)
    print("Installed catchlogr-agent-server-e2e with only the catchlogr-e2e label.")
