#!/bin/sh

set -eu

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
PROJECT_FILE="$SCRIPT_DIR/csharp/r2wars.csproj"

if [ ! -f "$PROJECT_FILE" ]; then
	printf '%s\n' "version.sh: missing project file: $PROJECT_FILE" >&2
	exit 1
fi

current_version=$(sed -n 's|^[[:space:]]*<Version>\([0-9][0-9.]*\)</Version>[[:space:]]*$|\1|p' "$PROJECT_FILE")
if [ -z "$current_version" ]; then
	printf '%s\n' "version.sh: cannot read the current version" >&2
	exit 1
fi

if [ "$#" -eq 0 ]; then
	printf '%s\n' "$current_version"
	exit 0
fi

if [ "$#" -ne 1 ]; then
	printf '%s\n' "usage: $0 [MAJOR.MINOR.PATCH]" >&2
	exit 1
fi

new_version=$1
if ! printf '%s\n' "$new_version" | grep -Eq '^[0-9]+(\.[0-9]+){2}$'; then
	printf '%s\n' "version.sh: expected version in the form MAJOR.MINOR.PATCH, got: $new_version" >&2
	exit 1
fi

temporary_file=$(mktemp "${TMPDIR:-/tmp}/r2wars-version.XXXXXX")
trap 'rm -f "$temporary_file"' EXIT HUP INT TERM

sed -E \
	-e "s|^([[:space:]]*<Version>)[0-9.]+(</Version>[[:space:]]*)$|\1${new_version}\2|" \
	"$PROJECT_FILE" > "$temporary_file"
cp "$temporary_file" "$PROJECT_FILE"

printf '%s -> %s\n' "$current_version" "$new_version"
