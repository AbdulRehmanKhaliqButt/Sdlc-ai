import os
import re
import shlex
import subprocess
import tempfile
import time
from pathlib import Path
from typing import Literal

from fastapi import FastAPI, Header, HTTPException
from pydantic import BaseModel, Field


app = FastAPI(title="SDLC AI Sandbox Runner", version="0.1.0")

REPOSITORY_RE = re.compile(r"^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$")
BLOCKED_TOKENS = (";", "&&", "||", "|", ">", "<", "`", "$(", "\n", "\r")
MAX_LOG_CHARS = 24_000
MAX_FILE_CHARS = 500_000

ALLOWED_COMMANDS = {
    ("dotnet", "restore"),
    ("dotnet", "build"),
    ("dotnet", "test"),
    ("npm", "ci"),
    ("npm", "install"),
    ("npm", "test"),
    ("npm", "run"),
    ("npx", "playwright"),
    ("python", "-m"),
    ("python3", "-m"),
    ("pytest",),
}


class FileChange(BaseModel):
    path: str = Field(min_length=1)
    action: Literal["create", "update"]
    content: str
    reason: str = Field(min_length=1)


class SandboxRequest(BaseModel):
    repository: str = Field(min_length=3)
    branch: str = Field(min_length=1)
    changes: list[FileChange] = Field(min_length=1)
    commands: list[str] = Field(min_length=1)


class CommandResult(BaseModel):
    command: str
    exitCode: int
    durationMs: int
    stdout: str
    stderr: str
    blocked: bool = False


class SandboxResult(BaseModel):
    passed: bool
    repository: str
    branch: str
    changedFiles: list[str]
    commands: list[CommandResult]


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok", "service": "sdlc-ai-sandbox-runner"}


def validate_command(command: str) -> list[str]:
    if any(token in command for token in BLOCKED_TOKENS):
        raise ValueError("Shell operators and command chaining are not allowed.")

    args = shlex.split(command)
    if not args:
        raise ValueError("Validation command is empty.")

    prefix = tuple(args[:2])
    if tuple(args[:1]) in ALLOWED_COMMANDS:
        return args
    if prefix not in ALLOWED_COMMANDS:
        raise ValueError(f"Command is not allowlisted: {args[0]} {' '.join(args[1:2])}".strip())

    if args[0] in {"python", "python3"} and len(args) >= 3 and args[1] == "-m" and args[2] != "pytest":
        raise ValueError("Only python -m pytest is allowed.")

    if args[0] == "npm" and len(args) >= 3 and args[1] == "run" and args[2] not in {"build", "test", "lint", "typecheck"}:
        raise ValueError("Only npm run build/test/lint/typecheck is allowed.")

    if args[0] == "npx" and (len(args) < 3 or args[1] != "playwright" or args[2] != "test"):
        raise ValueError("Only npx playwright test is allowed.")

    return args


def safe_target(root: Path, relative: str) -> Path:
    candidate = (root / relative).resolve()
    root_resolved = root.resolve()
    if candidate == root_resolved or root_resolved not in candidate.parents:
        raise ValueError(f"Change path escapes repository root: {relative}")
    return candidate


def clone_repository(root: Path, repository: str, branch: str, token: str | None) -> None:
    if not REPOSITORY_RE.fullmatch(repository):
        raise ValueError("Repository must use owner/name format.")

    env = os.environ.copy()
    env["GIT_TERMINAL_PROMPT"] = "0"
    askpass_path = None

    if token:
        askpass = root.parent / "git-askpass.sh"
        askpass.write_text(
            "#!/bin/sh\n"
            "case \"$1\" in\n"
            "  *Username*) echo x-access-token ;;\n"
            f"  *) echo '{token.replace("'", "")}' ;;\n"
            "esac\n",
            encoding="utf-8",
        )
        askpass.chmod(0o700)
        askpass_path = askpass
        env["GIT_ASKPASS"] = str(askpass)

    try:
        result = subprocess.run(
            ["git", "clone", "--depth", "1", "--branch", branch, f"https://github.com/{repository}.git", str(root)],
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            timeout=120,
            env=env,
        )
        if result.returncode != 0:
            raise RuntimeError(f"git clone failed: {result.stderr[-4000:]}")
    finally:
        if askpass_path and askpass_path.exists():
            askpass_path.unlink()


def apply_changes(root: Path, changes: list[FileChange]) -> list[str]:
    changed: list[str] = []
    for change in changes:
        if len(change.content) > MAX_FILE_CHARS:
            raise ValueError(f"Proposed file is too large for sandbox execution: {change.path}")
        target = safe_target(root, change.path)
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(change.content, encoding="utf-8")
        changed.append(change.path)
    return changed


def run_validation(root: Path, command: str) -> CommandResult:
    started = time.monotonic()
    try:
        args = validate_command(command)
    except ValueError as exc:
        return CommandResult(
            command=command,
            exitCode=126,
            durationMs=0,
            stdout="",
            stderr=str(exc),
            blocked=True,
        )

    env = os.environ.copy()
    env["CI"] = "true"
    env["HOME"] = "/tmp/home"
    Path(env["HOME"]).mkdir(parents=True, exist_ok=True)

    try:
        result = subprocess.run(
            args,
            cwd=root,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            timeout=int(os.getenv("SANDBOX_COMMAND_TIMEOUT_SECONDS", "180")),
            env=env,
        )
        exit_code = result.returncode
        stdout = result.stdout[-MAX_LOG_CHARS:]
        stderr = result.stderr[-MAX_LOG_CHARS:]
    except subprocess.TimeoutExpired as exc:
        exit_code = 124
        stdout = (exc.stdout or "")[-MAX_LOG_CHARS:] if isinstance(exc.stdout, str) else ""
        stderr = "Command timed out."
    duration_ms = int((time.monotonic() - started) * 1000)

    return CommandResult(
        command=command,
        exitCode=exit_code,
        durationMs=duration_ms,
        stdout=stdout,
        stderr=stderr,
    )


@app.post("/v1/execute", response_model=SandboxResult)
def execute(request: SandboxRequest, x_github_token: str | None = Header(default=None)) -> SandboxResult:
    with tempfile.TemporaryDirectory(prefix="sdlc-ai-") as temp:
        root = Path(temp) / "repo"
        try:
            clone_repository(root, request.repository, request.branch, x_github_token)
            changed = apply_changes(root, request.changes)
        except (ValueError, RuntimeError) as exc:
            raise HTTPException(status_code=400, detail=str(exc)) from exc

        results: list[CommandResult] = []
        for command in request.commands:
            result = run_validation(root, command)
            results.append(result)
            if result.exitCode != 0:
                break

        return SandboxResult(
            passed=bool(results) and all(result.exitCode == 0 for result in results),
            repository=request.repository,
            branch=request.branch,
            changedFiles=changed,
            commands=results,
        )
