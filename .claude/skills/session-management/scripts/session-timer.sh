#!/usr/bin/env bash
# session-timer.sh — Countdown sleep timer for AI coding agents
# Usage: bash session-timer.sh [minutes]
# Compatible with macOS and Linux

set -euo pipefail

MINUTES="${1:-5}"
TOTAL_SECONDS=$((MINUTES * 60))
ELAPSED=0

echo ""
echo "⏸️  SESSION REST — Sleeping for ${MINUTES} minute(s)"
echo "   Started at: $(date '+%Y-%m-%d %H:%M:%S')"
echo "   Will resume at: $(date -v+${MINUTES}M '+%Y-%m-%d %H:%M:%S' 2>/dev/null || date -d "+${MINUTES} minutes" '+%Y-%m-%d %H:%M:%S' 2>/dev/null || echo 'unknown')"
echo "   ────────────────────────────────────"

while [ $ELAPSED -lt $TOTAL_SECONDS ]; do
    REMAINING=$((TOTAL_SECONDS - ELAPSED))
    REMAINING_MIN=$((REMAINING / 60))
    REMAINING_SEC=$((REMAINING % 60))
    printf "\r   ⏳ %02d:%02d remaining..." "$REMAINING_MIN" "$REMAINING_SEC"

    # Sleep in 30-second increments (or remaining time if less)
    if [ $REMAINING -ge 30 ]; then
        sleep 30
        ELAPSED=$((ELAPSED + 30))
    else
        sleep $REMAINING
        ELAPSED=$((ELAPSED + REMAINING))
    fi
done

printf "\r   ✅ Rest complete!                    \n"
echo "   ────────────────────────────────────"
echo "▶️  RESUMING at $(date '+%Y-%m-%d %H:%M:%S')"
echo ""