#!/usr/bin/env bash
# publish-linux.sh
# Builds a self-contained single executable for Linux x64.
# Run from the repository root: bash build/publish-linux.sh
#
# Output: build/publish/linux-x64/ErganiManager

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$REPO_ROOT/src/ErganiManager.UI/ErganiManager.UI.csproj"
OUTPUT_DIR="$REPO_ROOT/build/publish/linux-x64"
CONFIGURATION="${1:-Release}"
VERSION="${2:-1.0.0}"

echo "Building ErganiManager $VERSION for Linux x64..."

dotnet publish "$PROJECT" \
    --configuration "$CONFIGURATION" \
    --runtime linux-x64 \
    --self-contained true \
    --output "$OUTPUT_DIR" \
    -p:PublishSingleFile=true \
    -p:EnableCompressionInSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:PublishReadyToRun=true \
    -p:Version="$VERSION" \
    -p:AssemblyVersion="$VERSION"

# Make the output executable
chmod +x "$OUTPUT_DIR/ErganiManager.UI"

# Rename to a cleaner name
mv -f "$OUTPUT_DIR/ErganiManager.UI" "$OUTPUT_DIR/ErganiManager"

echo ""
echo "✅ Published to: $OUTPUT_DIR"
echo "   Executable:   ErganiManager"
echo ""
echo "To create a distributable tarball:"
echo "   tar -czf ErganiManager-${VERSION}-linux-x64.tar.gz -C '$OUTPUT_DIR' ."
