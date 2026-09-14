#!/usr/bin/env python3
"""Import Markdown notes into the blog API as draft posts.

The bearer token is read from standard input so it is not stored in a file or
included in the process arguments. Existing slugs are skipped, making reruns
safe after a partial failure.
"""

from __future__ import annotations

import argparse
import json
import sys
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--base-url", required=True)
    parser.add_argument("--notes-dir", type=Path, default=Path("note"))
    parser.add_argument("--category", default=".NET Full Stack")
    parser.add_argument("--dry-run", action="store_true")
    return parser.parse_args()


def request_json(
    url: str,
    token: str,
    method: str = "GET",
    payload: dict[str, object] | None = None,
) -> dict[str, object]:
    data = None if payload is None else json.dumps(payload).encode("utf-8")
    request = urllib.request.Request(
        url,
        data=data,
        method=method,
        headers={
            "Accept": "application/json",
            "Authorization": f"Bearer {token}",
            "Content-Type": "application/json",
        },
    )

    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            return json.load(response)
    except urllib.error.HTTPError as error:
        detail = error.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"{method} {url} failed ({error.code}): {detail}") from error


def read_title(markdown: str, path: Path) -> str:
    for line in markdown.splitlines():
        if line.startswith("# "):
            return line[2:].strip()
    raise ValueError(f"No level-one heading found in {path}")


def tags_for(slug: str) -> list[str]:
    rules = (
        ("aspnet", "ASP.NET Core"),
        ("dotnet", ".NET"),
        ("ef-core", "EF Core"),
        ("postgresql", "PostgreSQL"),
        ("docker", "Docker"),
        ("compose", "Docker Compose"),
        ("jwt", "JWT"),
        ("github", "GitHub Actions"),
        ("cloudwatch", "CloudWatch"),
        ("s3", "Amazon S3"),
        ("vps", "VPS"),
        ("test", "Testing"),
    )
    return [tag for keyword, tag in rules if keyword in slug]


def main() -> int:
    args = parse_args()
    token = sys.stdin.readline().strip()
    if not token:
        print("No access token received on standard input.", file=sys.stderr)
        return 2

    base_url = args.base_url.rstrip("/")
    existing = request_json(f"{base_url}/api/blogs?page=1&pageSize=100", token)
    existing_slugs = {
        str(item["slug"])
        for item in existing.get("items", [])
        if isinstance(item, dict) and "slug" in item
    }

    paths = sorted(path for path in args.notes_dir.glob("*.md") if path.name != "README.md")
    created = 0
    skipped = 0

    for path in paths:
        slug = path.stem
        if slug in existing_slugs:
            print(f"SKIP   {slug}")
            skipped += 1
            continue

        content = path.read_text(encoding="utf-8")
        payload: dict[str, object] = {
            "title": read_title(content, path),
            "slug": slug,
            "categoryNames": [args.category],
            "tags": tags_for(slug),
            "content": content,
        }

        if args.dry_run:
            print(f"WOULD  {slug}")
            continue

        result = request_json(f"{base_url}/api/blogs", token, "POST", payload)
        print(f"CREATE {result.get('id')} {slug}")
        existing_slugs.add(slug)
        created += 1

    print(f"SUMMARY created={created} skipped={skipped} total={len(paths)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
