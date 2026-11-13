#!/bin/bash
# Roda testes no espelho. Uso: mtest.sh EditMode|PlayMode
DST="C:/Users/david/AppData/Local/Temp/glmirror/My project"
S="C:/Users/david/AppData/Local/Temp/claude/16b0273e3f71106e/2b81e4c7-a91e-4a87-aeb4-71bdda4265e5/scratchpad"
P="${1:-EditMode}"
rm -f "$S/tests_$P.xml"
"C:/Program Files/Unity/Hub/Editor/6000.3.10f1/Editor/Unity.exe" -batchmode -projectPath "$DST" -runTests -testPlatform "$P" -testResults "$S/tests_$P.xml" -logFile "$S/mtest_$P.log"
echo "exit=$?"
grep -o '<test-run [^>]*' "$S/tests_$P.xml" | grep -o 'total="[0-9]*"\|passed="[0-9]*"\|failed="[0-9]*"' | tr '\n' ' '; echo
grep -o '<test-case [^>]*result="Failed"[^>]*' "$S/tests_$P.xml" | grep -o 'fullname="[^"]*"'
grep -A3 '<message>' "$S/tests_$P.xml" | head -60
