#!/usr/bin/env python3
import subprocess
import sys
import os

def main():
    repo_root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    cmd = ["dotnet", "build", "ActDim.Practix.sln", "-v", "q"]
    res = subprocess.run(cmd, cwd=repo_root)
    sys.exit(res.returncode)

if __name__ == "__main__":
    main()

