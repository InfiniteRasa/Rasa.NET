#!/bin/sh
set -e

case "$1" in
    auth)
        shift
        cd /app/auth
        exec dotnet Rasa.Auth.dll "$@"
        ;;
    game)
        shift
        cd /app/game
        exec dotnet Rasa.Game.dll "$@"
        ;;
    *)
        exec "$@"
        ;;
esac
