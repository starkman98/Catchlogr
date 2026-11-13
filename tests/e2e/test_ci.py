"""Check CI leaves existing hosts alone and cleans partial host startup."""
import tempfile
from pathlib import Path
import unittest
from unittest.mock import Mock, patch

import ci


class HostOwnershipTests(unittest.TestCase):
    """The wrapper owns only emulator/Appium processes it actually started."""

    def test_live_hosts_are_reused(self):
        children = []
        with patch.object(ci, "emulator_ready", return_value=True), \
                patch.object(ci, "appium_ready", return_value=True), \
                patch.object(ci.subprocess, "Popen") as spawn:
            ci.start_hosts(children, Mock())
        spawn.assert_not_called()
        self.assertEqual(children, [])

    def test_partial_host_startup_failure_stops_owned_process(self):
        child = Mock()

        def fail_after_start(children, _log):
            children.append(child)
            raise RuntimeError("Appium startup failed")

        with tempfile.TemporaryDirectory() as temporary, \
                patch.object(ci, "ROOT", Path(temporary)), \
                patch.object(ci.signal, "signal"), \
                patch.object(ci, "start_hosts", side_effect=fail_after_start), \
                patch.object(ci, "stop_hosts") as stop:
            with self.assertRaises(RuntimeError):
                ci.main()
        stop.assert_called_once_with([child])


if __name__ == "__main__":
    unittest.main()
