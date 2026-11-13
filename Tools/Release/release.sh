#!/bin/bash
# Publica uma versão do jogo no GitHub Releases (é de lá que o jogo instalado se atualiza).
#
#   bash Tools/Release/release.sh 0.2.0 "O que mudou nesta versão"
#
# Faz, em ordem: build final para Windows com essa versão, zip, commit da versão, push e a
# release v<versão> com o zip anexado. Precisa do editor do Unity FECHADO e do `gh` logado.
set -e
VERSION="$1"
NOTES="${2:-Atualização $1}"
REPO="DavidHGJ/the-game-like"
UNITY="C:/Program Files/Unity/Hub/Editor/6000.3.10f1/Editor/Unity.exe"
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="$ROOT/Build/Release/$VERSION"
ZIP="$ROOT/Build/Release/TheGameLike-$VERSION-win64.zip"

if [ -z "$VERSION" ]; then echo "Uso: release.sh <versao> [notas]"; exit 2; fi
if tasklist | grep -q "^Unity.exe"; then echo "Feche o editor do Unity antes de publicar."; exit 3; fi
if gh release view "v$VERSION" --repo "$REPO" > /dev/null 2>&1; then echo "A release v$VERSION já existe."; exit 4; fi

echo "== Build $VERSION =="
rm -rf "$OUT"; mkdir -p "$OUT"
"$UNITY" -batchmode -projectPath "$ROOT" -executeMethod GameLike.Editor.CI.BatchBuild.BuildRelease \
  -glout "$OUT" -glversion "$VERSION" -logFile "$ROOT/Build/Release/build-$VERSION.log"
grep -q "GAMELIKE_RELEASE_BUILD result=Succeeded" "$ROOT/Build/Release/build-$VERSION.log" || { echo "Build falhou — veja Build/Release/build-$VERSION.log"; exit 5; }
rm -rf "$OUT"/*_BurstDebugInformation_DoNotShip "$OUT"/*_BackUpThisFolder_ButDontShipItWithYourGame

echo "== Zip =="
rm -f "$ZIP"
powershell -NoProfile -Command "Compress-Archive -Path '$(cygpath -w "$OUT")\*' -DestinationPath '$(cygpath -w "$ZIP")' -Force"

echo "== Git =="
cd "$ROOT"
git add ProjectSettings/ProjectSettings.asset
git diff --cached --quiet || git commit -m "chore: versão $VERSION"
git push origin HEAD

echo "== Release =="
gh release create "v$VERSION" "$ZIP" --repo "$REPO" --title "The Game Like $VERSION" --notes "$NOTES"
echo "Publicado: https://github.com/$REPO/releases/tag/v$VERSION"
