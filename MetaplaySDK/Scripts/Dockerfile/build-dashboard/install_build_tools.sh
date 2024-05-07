if [ "$RUN_TESTS" != "0" ]; then
    apt-get update
    apt-get install --no-install-recommends -y libgtk2.0-0 libgtk-3-0 libgbm-dev libnotify-dev libgconf-2-4 libnss3 libxss1 libasound2 libxtst6 xauth xvfb
fi

# Install pnpm
npm i -g pnpm
