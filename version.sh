#!/bin/sh

set -eu

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
ASSEMBLY_INFO="$SCRIPT_DIR/csharp/Properties/AssemblyInfo.cs"

if [ ! -f "$ASSEMBLY_INFO" ]; then
	printf '%s\n' "version.sh: missing assembly file: $ASSEMBLY_INFO" >&2
	exit 1
fi

assembly_version=$(sed -n 's/^[[:space:]]*\[assembly:[[:space:]]*AssemblyVersion("\([0-9][0-9.]*\)")\][[:space:]]*$/\1/p' "$ASSEMBLY_INFO")
if [ -z "$assembly_version" ]; then
	printf '%s\n' "version.sh: cannot read the current version" >&2
	exit 1
fi

# .NET assembly versions have a fourth revision component. Release versions do not.
current_version=${assembly_version%.0}

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

new_assembly_version="$new_version.0"
temporary_file=$(mktemp "${TMPDIR:-/tmp}/r2wars-version.XXXXXX")
trap 'rm -f "$temporary_file"' EXIT HUP INT TERM

sed -E \
	-e "s/^([[:space:]]*\[assembly:[[:space:]]*AssemblyVersion\(\")[0-9.]+(\"\)\][[:space:]]*)$/\1${new_assembly_version}\2/" \
	-e "s/^([[:space:]]*\[assembly:[[:space:]]*AssemblyFileVersion\(\")[0-9.]+(\"\)\][[:space:]]*)$/\1${new_assembly_version}\2/" \
	"$ASSEMBLY_INFO" > "$temporary_file"
cp "$temporary_file" "$ASSEMBLY_INFO"

printf '%s -> %s\n' "$current_version" "$new_version"
