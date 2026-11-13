#!/bin/bash
# Uso: patch.sh <arquivo> <script.pl> — aplica o patch só se o perl terminar bem (nunca trunca o arquivo).
perl "$2" < "$1" > "$1.tmp" && [ -s "$1.tmp" ] && mv "$1.tmp" "$1" && echo "patched $1" || { rm -f "$1.tmp"; echo "PATCH FAILED $1"; exit 1; }
