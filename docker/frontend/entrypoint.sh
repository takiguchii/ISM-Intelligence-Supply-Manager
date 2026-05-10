#!/bin/sh
set -eu

echo "Installing frontend dependencies..."
npm install

echo "Starting Nuxt development server..."
exec npm run dev
