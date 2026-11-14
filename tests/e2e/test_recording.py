"""Check opt-in recording ownership and failure-path video finalization."""
from pathlib import Path
import unittest
from unittest.mock import Mock, patch

import recording


class RecordingTests(unittest.TestCase):
    """Recording never touches another process and still finalizes failed runs."""

    def test_disabled_and_backend_only_do_not_spawn(self):
        with patch.object(recording.subprocess, "Popen") as spawn:
            for adb, enabled in (("adb", False), (None, True)):
                with recording.record_android(adb, "emulator-5554", Path("/tmp"), enabled):
                    pass
        spawn.assert_not_called()

    def test_test_failure_stops_owned_pid_and_pulls_video(self):
        process = Mock()
        process.stdout.readline.return_value = "12345\n"
        process.poll.return_value = None
        with patch.object(recording.subprocess, "Popen", return_value=process), \
                patch.object(recording.select, "select", return_value=([process.stdout], [], [])), \
                patch.object(recording, "run") as command:
            with self.assertRaisesRegex(RuntimeError, "Test failed"):
                with recording.record_android("adb", "emulator-5554", Path("/tmp"), True):
                    raise RuntimeError("Test failed")
        calls = [call.args for call in command.call_args_list]
        self.assertEqual(calls[0], ("adb", "-s", "emulator-5554", "shell", "kill", "-2", "12345"))
        self.assertEqual(calls[1][3], "pull")
        self.assertEqual(calls[1][-1], "/tmp/android.mp4")
        self.assertEqual(calls[2][3:6], ("shell", "rm", "-f"))
        self.assertEqual(calls[1][4], calls[2][-1])

    def test_invalid_pid_never_signals_remote_process(self):
        process = Mock()
        process.stdout.readline.return_value = "not-a-pid\n"
        process.poll.return_value = 1
        with patch.object(recording.subprocess, "Popen", return_value=process), \
                patch.object(recording.select, "select", return_value=([process.stdout], [], [])), \
                patch.object(recording, "run") as command:
            with self.assertRaisesRegex(RuntimeError, "invalid process ID"):
                with recording.record_android("adb", "emulator-5554", Path("/tmp"), True):
                    self.fail("An invalid recorder must not run the test")
        self.assertEqual(command.call_count, 1)
        self.assertEqual(command.call_args.args[3:6], ("shell", "rm", "-f"))


if __name__ == "__main__":
    unittest.main()
