"""Bump online compatibility versions or check a GitHub pull request."""

import argparse
import json
from pathlib import Path
import re
import subprocess


ROOT = Path(__file__).resolve().parents[1]
VERSIONS = {
    "rules": ("OnlineFrontendSessionPolicy.cs", rb'"frontend-rules-v([0-9]+)"'),
    "protocol": ("OnlineSessionWire.cs", rb'"session-wire-v([0-9]+)"'),
}
DECISIONS = {
    "compatible": set(),
    "rules bump": {"rules"},
    "protocol bump": {"protocol"},
    "both": set(VERSIONS),
}


def source_path(kind):
    return Path("Assets/Scripts/Online") / VERSIONS[kind][0]


def version_match(kind, source):
    matches = list(re.finditer(VERSIONS[kind][1], source))
    if len(matches) != 1:
        raise ValueError(f"Expected one {kind} version in {source_path(kind)}.")
    return matches[0]


def bump(selection):
    changes = []
    for kind in VERSIONS if selection == "both" else (selection,):
        path = ROOT / source_path(kind)
        source = path.read_bytes()
        match = version_match(kind, source)
        current = int(match[1])
        start, end = match.span(1)
        updated = source[:start] + str(current + 1).encode("ascii") + source[end:]
        changes.append((kind, path, updated, current))

    for kind, path, updated, current in changes:
        path.write_bytes(updated)
        print(f"{kind}: {current} -> {current + 1}")


def declared_bumps(body):
    sections = re.split(r"^#{1,2}[ \t]+", body, flags=re.MULTILINE)
    sections = [
        section for section in sections[1:]
        if section.partition("\n")[0].strip().lower() == "online compatibility"
    ]
    if len(sections) != 1:
        raise ValueError("Add the Online compatibility section from the PR template.")

    section = sections[0]
    checked = re.findall(r"^[ \t]*- \[[xX]\][ \t]+([^\r\n]+)", section, re.MULTILINE)
    choices = [choice.strip().lower() for choice in checked]
    if len(choices) != 1 or choices[0] not in DECISIONS:
        raise ValueError("Select exactly one Online compatibility option in the PR description.")
    if not re.search(r"^Reason:[ \t]*\S[^\r\n]*", section, re.MULTILINE):
        raise ValueError("Enter a reason on the Reason line in the PR description.")
    return DECISIONS[choices[0]]


def version_at(ref, kind):
    source = subprocess.check_output(
        ["git", "show", f"{ref}:{source_path(kind).as_posix()}"], cwd=ROOT
    )
    return int(version_match(kind, source)[1])


def check(event_path):
    event = json.loads(event_path.read_text(encoding="utf-8"))
    pull_request = event["pull_request"]
    expected = declared_bumps(pull_request.get("body") or "")
    base = pull_request["base"]["sha"]
    if not re.fullmatch(r"[0-9a-f]{40}", base):
        raise ValueError("Expected the target branch commit ID from GitHub.")

    problems = []
    for kind in VERSIONS:
        before = version_at(base, kind)
        after = version_at("HEAD", kind)
        if kind in expected and after <= before:
            problems.append(f"{kind} must increase from {before}, found {after}.")
        elif kind not in expected and after != before:
            problems.append(f"{kind} must stay at {before}, found {after}.")
        print(f"{kind}: target {before}, PR result {after}")
    if problems:
        raise ValueError(" ".join(problems))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    commands.add_parser("bump").add_argument("selection", choices=[*VERSIONS, "both"])
    commands.add_parser("check").add_argument("event", type=Path)
    args = parser.parse_args()

    try:
        if args.command == "bump":
            bump(args.selection)
        else:
            check(args.event)
    except (KeyError, OSError, ValueError, subprocess.CalledProcessError) as error:
        parser.exit(1, f"Online compatibility: {error}\n")


if __name__ == "__main__":
    main()
