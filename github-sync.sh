#!/bin/bash

cd "/volume1/Online Server/prestexa-los/api" || exit

git add .

if ! git diff --cached --quiet; then
    git commit -m "Auto sync $(date '+%Y-%m-%d %H:%M:%S')"
    git push origin main
fi
