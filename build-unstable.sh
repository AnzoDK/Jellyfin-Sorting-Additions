#!/bin/bash
set -e
BUILD_SCRIPT_DIR=$(dirname "$(realpath "$0")")
echo "Running from: "$BUILD_SCRIPT_DIR
cd "$BUILD_SCRIPT_DIR"

#Setup
ARG_COUNT=$#
for arg in "$@"; do
    case $arg in
        --no-auto-increment)
        ;;
        *)
        echo "Unknown argument: $arg"
        echo "Usage: $0 [--no-auto-increment]"
        exit 1
        ;;
    esac
done


PREV_VERSION_QUOTED=$(cat build.yaml | awk 'BEGIN{ RS = "" ; FS = "\n"}{print $4}' | awk -F ': ' '{print $2}')

CURR_VERSION=${PREV_VERSION_QUOTED:1:-1}
CURR_VERSION_LAST_NUM="${CURR_VERSION##*.}"
CURR_VERSION_LAST_NUM=$((CURR_VERSION_LAST_NUM+1))

CURR_VERSION_NO_LAST=${CURR_VERSION%.*}
NEXT_VERSION=$CURR_VERSION_NO_LAST.$CURR_VERSION_LAST_NUM
echo "Next Version will be written as:" "$NEXT_VERSION"

if [[ "$1" == "--no-auto-increment" ]]; then
    echo "Auto-increment disabled, using current version: $CURR_VERSION"
    NEXT_VERSION=$CURR_VERSION
fi

#4 is the line number in the build yaml
sed -i "4s/.*/version: \"${NEXT_VERSION}\"/" build.yaml

echo "build.yaml updated"

sed -i "3s/.*/\t\t<Version>${NEXT_VERSION}<\/Version>/" Directory.Build.props
sed -i "4s/.*/\t\t<AssemblyVersion>${NEXT_VERSION}<\/AssemblyVersion>/" Directory.Build.props
sed -i "5s/.*/\t\t<FileVersion>${NEXT_VERSION}<\/FileVersion>/" Directory.Build.props

echo "Directory.Build.props updated"



#Building

dotnet build -c Release

TIME_NOW=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
cd ./Jellyfin.Plugin.SortAdditions/bin/Release/net9.0
ZIP_FILE_NAME="Jellyfin.Plugin.SortAdditions_${NEXT_VERSION}.zip"
rm ./*.zip
zip $ZIP_FILE_NAME ./Jellyfin.Plugin.SortAdditions.dll
CHECKSUM=$(md5sum $ZIP_FILE_NAME | awk '{ print $1 }')
echo "Checksum for new release: $CHECKSUM"
ENTRY_TEMPLATE="        {
            \"version\": \"$NEXT_VERSION\",
            \"changelog\": \"<INSERT CHANGELOG HERE>\",
            \"targetAbi\": \"10.11.0.0\",
            \"sourceUrl\": \"https://github.com/AnzoDK/Jellyfin-Sorting-Additions/releases/download/v$NEXT_VERSION/$ZIP_FILE_NAME\",
            \"checksum\": \"$CHECKSUM\",
            \"timestamp\": \"$TIME_NOW\"
        },
        {"
ENTRY_TEMPLATE_ESCAPED=${ENTRY_TEMPLATE//\"/\\\"}
ENTRY_TEMPLATE_ESCAPED=${ENTRY_TEMPLATE_ESCAPED//$'\n'/\\n}
echo "Adding template to manifest.json"
cd $BUILD_SCRIPT_DIR
echo $(pwd)
sed -i "11s;.*;${ENTRY_TEMPLATE_ESCAPED};g" manifest.json
exit 0
