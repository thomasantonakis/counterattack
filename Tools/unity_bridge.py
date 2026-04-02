#!/usr/bin/env python3

from __future__ import annotations

import argparse
import json
import sys
import time
import uuid
from datetime import datetime, timezone
from pathlib import Path


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Send a command to the open Unity editor bridge.")
    parser.add_argument("command", help="Whitelisted Unity bridge command to execute.")
    parser.add_argument(
        "--arg",
        action="append",
        default=[],
        metavar="KEY=VALUE",
        help="Command argument. Repeat for multiple values.",
    )
    parser.add_argument(
        "--timeout",
        type=float,
        default=30.0,
        help="Seconds to wait for a response before exiting with an error.",
    )
    parser.add_argument(
        "--no-wait",
        action="store_true",
        help="Queue the command and exit without waiting for Unity to respond.",
    )
    return parser


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


def parse_args(raw_args: list[str]) -> dict[str, str]:
    parsed: dict[str, str] = {}
    for item in raw_args:
        if "=" not in item:
            raise ValueError(f"Invalid --arg value '{item}'. Expected KEY=VALUE.")
        key, value = item.split("=", 1)
        if not key:
            raise ValueError("Argument key cannot be empty.")
        parsed[key] = value
    return parsed


def get_bridge_paths(project_root: Path) -> tuple[Path, Path]:
    bridge_root = project_root / "Temp" / "CodexUnityBridge"
    requests_dir = bridge_root / "requests"
    responses_dir = bridge_root / "responses"
    requests_dir.mkdir(parents=True, exist_ok=True)
    responses_dir.mkdir(parents=True, exist_ok=True)
    return requests_dir, responses_dir


def main() -> int:
    parser = build_parser()
    args = parser.parse_args()

    project_root = Path(__file__).resolve().parent.parent
    requests_dir, responses_dir = get_bridge_paths(project_root)

    try:
        arg_map = parse_args(args.arg)
    except ValueError as exc:
        print(str(exc), file=sys.stderr)
        return 2

    request_id = f"{datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S')}_{uuid.uuid4().hex[:8]}"
    request_path = requests_dir / f"{request_id}.json"
    response_path = responses_dir / f"{request_id}.json"

    payload = {
        "id": request_id,
        "command": args.command,
        "createdAtUtc": utc_now(),
        "args": [{"key": key, "value": value} for key, value in sorted(arg_map.items())],
    }

    temp_request_path = request_path.with_suffix(".tmp")
    temp_request_path.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    temp_request_path.replace(request_path)

    if args.no_wait:
        print(json.dumps({"status": "queued", "id": request_id, "request": str(request_path)}, indent=2))
        return 0

    deadline = time.monotonic() + args.timeout
    while time.monotonic() < deadline:
        if response_path.exists():
            response = json.loads(response_path.read_text(encoding="utf-8"))
            response_path.unlink(missing_ok=True)
            print(json.dumps(response, indent=2))
            return 0 if response.get("status") == "ok" else 1
        time.sleep(0.2)

    print(
        json.dumps(
            {
                "status": "timeout",
                "id": request_id,
                "message": f"No Unity bridge response received within {args.timeout:.1f}s.",
                "request": str(request_path),
                "response": str(response_path),
            },
            indent=2,
        ),
        file=sys.stderr,
    )
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
