#!/usr/bin/env bash
set -e

if [ -z "$1" ]; then
  echo "Usage: $0 <new-version>"
  exit 1
fi

newver=$1

echo "Bumping version to $newver"

# update SdkVersion.cs
sed -i "s/VersionNumber = \"[0-9]*\.[0-9]*\.[0-9]*\"/VersionNumber = \"$newver\"/" SDK/Runtime/Utils/SdkVersion.cs

# update package.json
jq ".version = \"$newver\"" SDK/package.json > SDK/package.tmp.json && mv SDK/package.tmp.json SDK/package.json

echo "Version updated in SdkVersion.cs and package.json"