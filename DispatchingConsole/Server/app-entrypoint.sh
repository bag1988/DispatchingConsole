#!/bin/sh
set -e

echo "starting app entrypoint..."
echo "CMD: $@"

echo "executing command $@..."
exec "$@"

