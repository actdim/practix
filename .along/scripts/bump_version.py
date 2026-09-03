#!/usr/bin/env python3
import sys
import os
import re

def main():
    bump_arg = sys.argv[1] if len(sys.argv) > 1 else "patch"
    repo_root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    props_path = os.path.join(repo_root, "Directory.Build.props")

    if not os.path.exists(props_path):
        print(f"Error: {props_path} not found.", file=sys.stderr)
        sys.exit(1)

    with open(props_path, "r", encoding="utf-8") as f:
        content = f.read()

    m = re.search(r"<Version>(\d+\.\d+\.\d+.*?)</Version>", content)
    cur_v = m.group(1) if m else "1.0.0"

    parts = [int(p) for p in cur_v.split("-")[0].split(".")]
    if bump_arg == "patch":
        next_v = f"{parts[0]}.{parts[1]}.{parts[2] + 1}"
    elif bump_arg == "minor":
        next_v = f"{parts[0]}.{parts[1] + 1}.0"
    elif bump_arg == "major":
        next_v = f"{parts[0] + 1}.0.0"
    else:
        next_v = bump_arg.lstrip("v")

    updated = re.sub(
        r"<Version>\d+\.\d+\.\d+.*?</Version>",
        f"<Version>{next_v}</Version>",
        content,
        count=1
    )

    with open(props_path, "w", encoding="utf-8") as f:
        f.write(updated)

    print(f"Bumped Directory.Build.props: v{cur_v} -> v{next_v}")

if __name__ == "__main__":
    main()

