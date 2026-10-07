#!/bin/sh
set -e

case "$1" in
    auth)
        cd /app/auth
        exec dotnet Rasa.Auth.dll
        ;;
    game)
        cd /app/game
        exec dotnet Rasa.Game.dll
        ;;
    *)
        exec "$@"
        ;;
esac
