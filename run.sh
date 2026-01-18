#!/bin/bash

# 1. Configuration
PROJECT_FILE="Nano-Backup-Website/Nano-Backup-Website.csproj"

echo "--- STEP 1: Restarting Docker (Full Rebuild) ---"
docker compose down

echo "--- STEP 2: Relaunch Docker Containers ---"
docker compose up -d

echo "--- STEP 3: Monitoring Logs ---"
# Follow the logs to see if the kernel32.dll error is gone
docker compose logs -f app