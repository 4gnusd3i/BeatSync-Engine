from __future__ import annotations

from importlib import metadata
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
REQUIREMENTS_DIR = REPO_ROOT / "requirements"


def parse_requirements(path: Path) -> set[str]:
    package_names: set[str] = set()
    for line in path.read_text(encoding="utf-8").splitlines():
        stripped = line.strip()
        if not stripped or stripped.startswith("#") or stripped.startswith("-r "):
            continue
        package_names.add(stripped.split("==", 1)[0].lower())
    return package_names


def main() -> int:
    runtime_packages = parse_requirements(REQUIREMENTS_DIR / "runtime.txt")
    dev_packages = parse_requirements(REQUIREMENTS_DIR / "dev.txt")
    declared_packages = runtime_packages | dev_packages
    installed_packages = {dist.metadata["Name"].lower() for dist in metadata.distributions()}

    print("Declared packages:")
    for package in sorted(declared_packages):
        print(f"  - {package}")

    print("\nInstalled packages not declared in requirements:")
    extras = sorted(installed_packages - declared_packages)
    for package in extras:
        print(f"  - {package}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())

