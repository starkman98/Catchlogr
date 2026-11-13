"""Host process, local TLS, and service readiness helpers for disposable E2E runs."""
from pathlib import Path
import socket
import subprocess
import time
import urllib.error
import urllib.request

ROOT = Path(__file__).resolve().parents[2]


def run(*args, capture_output=False, interrupt_timeout=10, **kwargs):
    """Execute checked argv and stop the child before interrupt cleanup starts."""
    if capture_output:
        kwargs.update(stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    with subprocess.Popen(args, cwd=ROOT, **kwargs) as process:
        try:
            stdout, stderr = process.communicate()
        except KeyboardInterrupt:
            stop_child(process, interrupt_timeout)
            raise
        if process.returncode:
            raise subprocess.CalledProcessError(process.returncode, args, stdout, stderr)
        return subprocess.CompletedProcess(args, process.returncode, stdout, stderr)


def stop_child(process, timeout=10):
    """Prevent an interrupted Compose client from continuing stack startup."""
    process.terminate()
    try:
        process.wait(timeout=timeout)
    except subprocess.TimeoutExpired:
        process.kill()
        process.wait()


def certificates(directory):
    """Generate a short-lived CA and SAN certificate scoped to the test stack."""
    common = {"cwd": directory, "stdout": subprocess.DEVNULL, "stderr": subprocess.DEVNULL}
    subprocess.run([
        "openssl", "req", "-x509", "-newkey", "rsa:2048", "-nodes", "-days", "2",
        "-keyout", "ca.key", "-out", "ca.crt", "-subj", "/CN=Catchlogr disposable E2E CA",
        "-addext", "basicConstraints=critical,CA:TRUE",
        "-addext", "keyUsage=critical,keyCertSign,cRLSign",
    ], check=True, **common)
    subprocess.run([
        "openssl", "req", "-newkey", "rsa:2048", "-nodes", "-keyout", "server.key",
        "-out", "server.csr", "-subj", "/CN=localhost",
    ], check=True, **common)
    (directory / "extensions.cnf").write_text(
        "subjectAltName=DNS:localhost,DNS:api,DNS:web,IP:127.0.0.1\n"
        "basicConstraints=critical,CA:FALSE\n"
        "keyUsage=critical,digitalSignature,keyEncipherment\n"
        "extendedKeyUsage=serverAuth\n")
    subprocess.run([
        "openssl", "x509", "-req", "-in", "server.csr", "-CA", "ca.crt",
        "-CAkey", "ca.key", "-CAcreateserial", "-days", "2", "-out", "server.crt",
        "-extfile", "extensions.cnf",
    ], check=True, **common)
    for name in ("ca.key", "server.key"):
        (directory / name).chmod(0o600)


def require_free_ports():
    """Refuse collisions instead of stopping existing development services."""
    for port in (15001, 17160, 17056, 18025):
        with socket.socket() as probe:
            probe.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            try:
                probe.bind(("127.0.0.1", port))
            except OSError as error:
                raise RuntimeError(f"E2E port {port} is occupied; existing services were left running") from error


def wait_http(url, context=None):
    deadline = time.monotonic() + 120
    while time.monotonic() < deadline:
        try:
            with urllib.request.urlopen(url, context=context, timeout=5) as response:
                if response.status == 200:
                    return
        except (OSError, urllib.error.URLError):
            pass
        time.sleep(1)
    raise RuntimeError("Test service did not become ready: " + url)
