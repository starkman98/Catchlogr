"""Verify partial startup failures cannot skip disposal of the test stack."""
import argparse
from pathlib import Path
import unittest
from unittest.mock import patch

import run as runner


class CleanupTests(unittest.TestCase):
    """Cleanup must stay scoped and run even when Android cleanup also fails."""

    def test_partial_stack_startup_is_disposed(self):
        commands = []

        def command(*args, **_kwargs):
            commands.append(args)
            if "up" in args:
                raise RuntimeError("Simulated partial startup failure")

        options = argparse.Namespace(backend_only=True, device="emulator-5554")
        with patch.object(runner, "require_free_ports"), patch.object(runner, "certificates"), \
                patch.object(runner, "run", side_effect=command):
            with self.assertRaises(RuntimeError):
                runner.execute(options, Path("/tmp/unused-e2e-artifacts"))

        startup, disposal = commands
        self.assertEqual(startup[:6], disposal[:6])
        self.assertTrue(startup[3].startswith("catchlogr-e2e-"))
        self.assertEqual(disposal[6:], ("down", "--volumes", "--remove-orphans"))

    def test_android_cleanup_failure_still_disposes_stack(self):
        commands = []

        def command(*args, **_kwargs):
            commands.append(args)
            if "up" in args or args[0] == "/fake/adb":
                raise RuntimeError("Simulated startup or device failure")

        options = argparse.Namespace(backend_only=False, device="emulator-5554")
        with patch.object(runner, "require_free_ports"), patch.object(runner, "certificates"), \
                patch.object(runner, "prepare_android", return_value="/fake/adb"), \
                patch.object(runner, "run", side_effect=command):
            with self.assertRaises(RuntimeError):
                runner.execute(options, Path("/tmp/unused-e2e-artifacts"))

        device_command = commands[1]
        self.assertEqual(device_command[-1], "com.catchlogr.e2e")
        self.assertEqual(commands[-1][6:], ("down", "--volumes", "--remove-orphans"))
        self.assertEqual(commands[0][:6], commands[-1][:6])


if __name__ == "__main__":
    unittest.main()
