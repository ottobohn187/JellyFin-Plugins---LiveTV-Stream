import argparse
import json
from pathlib import Path

import yaml


def load_yaml(path: Path) -> dict:
    with path.open("r", encoding="utf-8") as handle:
        return yaml.safe_load(handle)


def load_existing_versions(path: Path, guid: str) -> list[dict]:
    if not path.exists():
        return []

    with path.open("r", encoding="utf-8") as handle:
        data = json.load(handle)

    if not isinstance(data, list):
        return []

    for plugin in data:
        if plugin.get("guid") == guid:
            versions = plugin.get("versions", [])
            if isinstance(versions, list):
                return versions

    return []


def load_manifest(path: Path) -> list[dict]:
    if not path.exists():
        return []

    with path.open("r", encoding="utf-8") as handle:
        data = json.load(handle)

    if isinstance(data, list):
        return data

    return []


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--build-yaml", required=True)
    parser.add_argument("--manifest", required=True)
    parser.add_argument("--source-url", required=True)
    parser.add_argument("--checksum", required=True)
    parser.add_argument("--timestamp", required=True)
    args = parser.parse_args()

    build_yaml = Path(args.build_yaml)
    manifest_path = Path(args.manifest)

    build = load_yaml(build_yaml)
    existing_versions = load_existing_versions(manifest_path, build["guid"])

    current_version = {
        "version": str(build["version"]),
        "changelog": str(build["changelog"]).strip(),
        "targetAbi": str(build["targetAbi"]),
        "sourceUrl": args.source_url,
        "timestamp": args.timestamp,
        "checksum": args.checksum,
    }

    versions = [current_version]
    for version in existing_versions:
        if version.get("version") != current_version["version"]:
            versions.append(version)

    manifest = load_manifest(manifest_path)

    plugin_entry = {
        "guid": build["guid"],
        "name": build["name"],
        "overview": build["overview"],
        "description": str(build["description"]).strip(),
        "owner": build["owner"],
        "category": build["category"],
        "versions": versions,
    }

    replaced = False
    for index, plugin in enumerate(manifest):
        if plugin.get("guid") == build["guid"]:
            manifest[index] = plugin_entry
            replaced = True
            break

    if not replaced:
        manifest.append(plugin_entry)

    manifest.sort(key=lambda plugin: str(plugin.get("name", "")))

    with manifest_path.open("w", encoding="utf-8", newline="\n") as handle:
        json.dump(manifest, handle, indent=2)
        handle.write("\n")


if __name__ == "__main__":
    main()
