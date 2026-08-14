#!/usr/bin/env bash
# install-linux.sh
# Installs ErganiManager system-wide on Linux.
# Run as root (or with sudo) after running publish-linux.sh:
#   sudo bash build/install-linux.sh
#
# To uninstall:
#   sudo bash build/install-linux.sh --uninstall

set -euo pipefail

INSTALL_DIR="/opt/ErganiManager"
BINARY_NAME="ErganiManager"
DESKTOP_FILE="/usr/share/applications/ErganiManager.desktop"
SYMLINK="/usr/local/bin/erganimanager"
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PUBLISH_DIR="$REPO_ROOT/build/publish/linux-x64"

if [[ "${1:-}" == "--uninstall" ]]; then
    echo "Uninstalling ErganiManager..."
    rm -rf "$INSTALL_DIR"
    rm -f  "$DESKTOP_FILE"
    rm -f  "$SYMLINK"
    echo "✅ Uninstalled."
    exit 0
fi

if [[ ! -f "$PUBLISH_DIR/$BINARY_NAME" ]]; then
    echo "❌ Published binary not found at $PUBLISH_DIR/$BINARY_NAME"
    echo "   Run 'bash build/publish-linux.sh' first."
    exit 1
fi

echo "Installing ErganiManager to $INSTALL_DIR..."

mkdir -p "$INSTALL_DIR"
cp -r "$PUBLISH_DIR/"* "$INSTALL_DIR/"
chmod +x "$INSTALL_DIR/$BINARY_NAME"

# Desktop entry
cp "$REPO_ROOT/build/ErganiManager.desktop" "$DESKTOP_FILE"
update-desktop-database /usr/share/applications/ 2>/dev/null || true

# Symlink for terminal use
ln -sf "$INSTALL_DIR/$BINARY_NAME" "$SYMLINK"

echo ""
echo "✅ Installed to $INSTALL_DIR"
echo "   Run from terminal: erganimanager"
echo "   Or find it in your application menu."
