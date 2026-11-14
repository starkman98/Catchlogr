"""Capture only the selected emulator during an explicitly requested test run."""
from contextlib import contextmanager
import select
import subprocess
import uuid

from host import run


class AndroidRecording:
    """Own one remote screenrecord process and retrieve its finalized MP4."""

    def __init__(self, adb, device, artifacts):
        self.adb = [adb, "-s", device]
        self.destination = str(artifacts / "android.mp4")
        self.remote = f"/sdcard/catchlogr-e2e-{uuid.uuid4().hex}.mp4"
        self.process = None
        self.pid = None

    def start(self):
        """Read the owned shell PID before exec replaces it with screenrecord."""
        command = ("echo $$; exec screenrecord --size 540x1200 --bit-rate 1500000 "
                   f"--time-limit 0 {self.remote}")
        self.process = subprocess.Popen(self.adb + ["shell", command],
                                        stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, text=True)
        if not select.select([self.process.stdout], [], [], 10)[0]:
            raise RuntimeError("Android recorder did not report its process ID")
        pid = self.process.stdout.readline().strip()
        if not pid.isdecimal():
            raise RuntimeError("Android recorder reported an invalid process ID")
        self.pid = pid

    def stop(self):
        """Signal only our recorder, pull its video, then delete its device file."""
        if self.process is None:
            return
        try:
            if self.pid and self.process.poll() is None:
                run(*self.adb, "shell", "kill", "-2", self.pid)
            self.process.wait(timeout=20)
            if self.pid:
                run(*self.adb, "pull", self.remote, self.destination)
        finally:
            if self.process.poll() is None:
                self.process.terminate()
                self.process.wait(timeout=10)
            self.process.stdout.close()
            run(*self.adb, "shell", "rm", "-f", self.remote)


@contextmanager
def record_android(adb, device, artifacts, enabled):
    """Finalize recording on success, failure or cancellation; no-op by default."""
    recording = AndroidRecording(adb, device, artifacts) if enabled and adb else None
    try:
        if recording:
            recording.start()
        yield
    finally:
        if recording:
            recording.stop()
