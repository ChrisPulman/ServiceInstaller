#!/usr/bin/env bash

set -euo pipefail
SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
BUILD_PROJECT_FILE="$SCRIPT_DIR/build/_build.csproj"
TEMP_DIRECTORY="$SCRIPT_DIR/.nuke/temp"
DOTNET_GLOBAL_FILE="$SCRIPT_DIR/global.json"
DOTNET_INSTALL_FILE="$TEMP_DIRECTORY/dotnet-install.sh"
DOTNET_DIRECTORY="$TEMP_DIRECTORY/dotnet-unix"

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_MULTILEVEL_LOOKUP=0
export NUKE_TELEMETRY_OPTOUT=true

if command -v dotnet >/dev/null 2>&1 && dotnet --version >/dev/null 2>&1; then
    DOTNET_EXE=$(command -v dotnet)
else
    mkdir -p "$TEMP_DIRECTORY"
    if [[ ! -f "$DOTNET_INSTALL_FILE" ]]; then
        curl -fsSL "https://dot.net/v1/dotnet-install.sh" -o "$DOTNET_INSTALL_FILE"
        chmod +x "$DOTNET_INSTALL_FILE"
    fi

    DOTNET_VERSION=$(perl -nle 'print $1 if /"version"\s*:\s*"([^"]+)"/' "$DOTNET_GLOBAL_FILE")
    if [[ ! -x "$DOTNET_DIRECTORY/dotnet" ]] || [[ "$($DOTNET_DIRECTORY/dotnet --version)" != "$DOTNET_VERSION" ]]; then
        "$DOTNET_INSTALL_FILE" --install-dir "$DOTNET_DIRECTORY" --version "$DOTNET_VERSION" --no-path
    fi

    DOTNET_EXE="$DOTNET_DIRECTORY/dotnet"
fi

export DOTNET_EXE
echo "Microsoft .NET SDK version $($DOTNET_EXE --version)"
"$DOTNET_EXE" build "$BUILD_PROJECT_FILE" /nodeReuse:false /p:UseSharedCompilation=false -nologo -clp:NoSummary --verbosity quiet
"$DOTNET_EXE" run --project "$BUILD_PROJECT_FILE" --no-build -- "$@"
