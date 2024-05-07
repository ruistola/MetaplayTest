# Install dependencies:
# If pnpm-lock.yaml exists, use it. Otherwise, generate it using the latest
# compatible versions of the dependencies. NOTE: If the pnpm-lock.yaml doesn't
# exists and latest dependency versions are used, it is possible that a dependency
# may no longer be compatible and break something. Thus, it is strongly recommended
# to generate a pnpm-lock.yaml and commit it to the project's source repository!

# Cypress installation:
# - If not running in integration tests, skip Cypress installation
# - If running tests, take a copy of the actual Cypress cache into /cypress-cache and
#   use it in all subsequent build steps (so that all steps don't need to have the --mount arg).
# - We must take a copy of Cypress "cache" (really more like install dir) in order to have it
#   available for the integration test runs which just run Cypress within this image. The mounts
#   are not available at the time of running the tests anymore.

if [ "$RUN_TESTS" = "0" ]; then
    export CYPRESS_INSTALL_BINARY=0
fi

echo "Installing dashboard dependencies..."
if [ -f "/build/$PNPM_ROOT/pnpm-lock.yaml" ]; then
    pnpm install --frozen-lockfile
else
    echo "Warning: No pnpm-lock.yaml found! You should consider creating one with 'pnpm install' and committing it into the repository!"
    pnpm install
fi

# If running tests, take a copy of Cypress into /cypress-cache and use it when running tests
if [ "$RUN_TESTS" = "1" ]; then
    echo "Copying Cypress cache into /cypress-cache so it's available on all subsequent steps..."
    cp -R /root/.cache/Cypress /cypress-cache
fi
