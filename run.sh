#!/bin/bash

# 1. Configuration
PROJECT_FILE="Nano-Backup-Website/Nano-Backup-Website.csproj"

echo "--- STEP 1: Deep Cleaning Project ---"
rm -rf Nano-Backup-Website/bin
rm -rf Nano-Backup-Website/obj

echo "--- STEP 2: Publishing for Linux-x64 ---"
dotnet publish "$PROJECT_FILE" -c Release -r linux-x64 --self-contained true

echo "--- STEP 3: Restarting Docker (Full Rebuild) ---"
docker compose down

echo "--- STEP 4: Rebuilding Docker Containers ---"
docker compose build
docker compose up -d

echo "--- STEP 5: Monitoring Logs ---"
# Follow the logs to see if the kernel32.dll error is gone
docker compose logs -f app