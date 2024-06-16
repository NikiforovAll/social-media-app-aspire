#!/bin/bash

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
cd "$SCRIPT_DIR/.."

COMMAND="$1"
MIGRATION_NAME="Initial"

if [ "$COMMAND" == "add" ]; then
    dotnet ef migrations add "$MIGRATION_NAME" --output-dir Migrations \
        --project ./src/Data/Postgres/ \
        --startup-project ./src/MigrationService/
elif [ "$COMMAND" == "remove" ]; then
    dotnet ef migrations remove --project ./src/Data/Postgres/ \
        --startup-project ./src/MigrationService/
else
    echo "Invalid command. Please specify 'add' or 'remove'."
fi
