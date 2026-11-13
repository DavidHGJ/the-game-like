#!/bin/bash
# REAL=1 usa o projeto real (editor fechado) em vez do espelho.
# Espelha Assets do projeto real e compila headless no espelho. Uso: mcompile.sh [metodo]
SRC="C:/Users/david/OneDrive/Documentos/Unity/TheGameLike/My project"
DST="C:/Users/david/AppData/Local/Temp/glmirror/My project"
S="C:/Users/david/AppData/Local/Temp/claude/16b0273e3f71106e/2b81e4c7-a91e-4a87-aeb4-71bdda4265e5/scratchpad"
METHOD="${1:-GameLike.Editor.CI.BatchCompile.Check}"
robocopy "$(cygpath -w "$SRC/Assets")" "$(cygpath -w "$DST/Assets")" //MIR //NFL //NDL //NJH //NJS //NP //R:1 //W:1 //XF "*.meta" > /dev/null
# metas: copia as do real sem apagar as geradas no espelho
robocopy "$(cygpath -w "$SRC/Assets")" "$(cygpath -w "$DST/Assets")" "*.meta" //S //NFL //NDL //NJH //NJS //NP //R:1 //W:1 > /dev/null
"C:/Program Files/Unity/Hub/Editor/6000.3.10f1/Editor/Unity.exe" -batchmode -quit -projectPath "$DST" -executeMethod "$METHOD" -logFile "$S/mcompile.log"
echo "exit=$?"
grep -E "error CS|GAMELIKE_[A-Z_]+OK" "$S/mcompile.log" | sort -u | head -40
