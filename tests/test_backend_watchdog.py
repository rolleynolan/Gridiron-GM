# ACCEPTANCE CRITERIA
# - Server exits automatically when parent process exits.
# - Tests should be resilient (polling loops with timeouts), and pass on Windows + macOS/Linux.

import socket
import subprocess
import sys
import tempfile
import time
import urllib.request
import shutil
from pathlib import Path

import pytest

from gridiron_gm_pkg.api.server import is_pid_alive


def _wait_until(predicate, timeout_seconds, interval_seconds=0.1):
    deadline = time.time() + timeout_seconds
    while time.time() < deadline:
        if predicate():
            return True
        time.sleep(interval_seconds)
    return False


def _get_free_port():
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
        sock.bind(("127.0.0.1", 0))
        return sock.getsockname()[1]


def _wait_for_server_start(process, host, port, timeout_seconds=3.0):
    url = f"http://{host}:{port}/health"
    deadline = time.time() + timeout_seconds
    while time.time() < deadline:
        if process.poll() is not None:
            return False
        try:
            with urllib.request.urlopen(url, timeout=0.5) as resp:
                if resp.status == 200:
                    return True
        except Exception:
            time.sleep(0.1)
    return process.poll() is None


def test_is_pid_alive_reflects_process_lifecycle():
    proc = subprocess.Popen(
        [sys.executable, "-c", "import time; time.sleep(1.5)"],
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
    )
    try:
        assert is_pid_alive(proc.pid) is True
        if not _wait_until(lambda: proc.poll() is not None, timeout_seconds=3.0):
            pytest.fail("Subprocess did not exit within the expected time.")
        assert _wait_until(lambda: not is_pid_alive(proc.pid), timeout_seconds=2.0)
    finally:
        if proc.poll() is None:
            proc.kill()
            proc.wait(timeout=5)


def test_watchdog_exits_when_parent_exits():
    parent_proc = subprocess.Popen(
        [sys.executable, "-c", "import time; time.sleep(10)"],
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
    )
    server_proc = None
    temp_dir = None
    try:
        port = _get_free_port()
        repo_root = Path(__file__).resolve().parents[1]
        temp_dir = tempfile.mkdtemp(dir=repo_root)
        save_path = Path(temp_dir) / "savegame.json"
        server_proc = subprocess.Popen(
            [
                sys.executable,
                "-m",
                "gridiron_gm_pkg.api.server",
                "--host",
                "127.0.0.1",
                "--port",
                str(port),
                "--save-path",
                str(save_path),
                "--parent-pid",
                str(parent_proc.pid),
            ],
            cwd=repo_root,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
        )

        if not _wait_for_server_start(server_proc, "127.0.0.1", port, timeout_seconds=3.0):
            raise AssertionError("Server process exited before it became ready.")

        parent_proc.terminate()
        try:
            parent_proc.wait(timeout=5)
        except subprocess.TimeoutExpired:
            parent_proc.kill()
            parent_proc.wait(timeout=5)

        if not _wait_until(lambda: server_proc.poll() is not None, timeout_seconds=3.0):
            server_proc.kill()
            server_proc.wait(timeout=5)
            pytest.fail("Server did not exit within 3 seconds after parent termination.")
    finally:
        if server_proc is not None and server_proc.poll() is None:
            server_proc.kill()
            server_proc.wait(timeout=5)
        if parent_proc.poll() is None:
            parent_proc.kill()
            parent_proc.wait(timeout=5)
        if temp_dir is not None:
            shutil.rmtree(temp_dir, ignore_errors=True)
