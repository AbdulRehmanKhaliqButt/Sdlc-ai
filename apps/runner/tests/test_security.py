from pathlib import Path

import pytest

from app.main import safe_target, validate_command


@pytest.mark.parametrize(
    "command",
    [
        "dotnet build",
        "dotnet test tests/My.Tests.csproj",
        "npm test",
        "npm run build",
        "npx playwright test",
        "python -m pytest",
        "pytest",
    ],
)
def test_allowlisted_commands(command):
    assert validate_command(command)


@pytest.mark.parametrize(
    "command",
    [
        "rm -rf /",
        "dotnet test && curl example.com",
        "npm run arbitrary-script",
        "python -m http.server",
        "sh -c whoami",
    ],
)
def test_rejects_unsafe_commands(command):
    with pytest.raises(ValueError):
        validate_command(command)


def test_safe_target_rejects_parent_escape(tmp_path: Path):
    root = tmp_path / "repo"
    root.mkdir()
    with pytest.raises(ValueError):
        safe_target(root, "../outside.txt")
