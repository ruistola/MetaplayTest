# \note Always running the tests on the latest build image as the build image doesn't support running with older frameworks
if [ "$RUN_TESTS" != "0" ]; then
    dotnet test /build/$SDK_ROOT/Backend/Cloud.Tests --framework net8.0
    dotnet test /build/$SDK_ROOT/Backend/Cloud.Serialization.Compilation.Tests --framework net8.0
    dotnet test /build/$SDK_ROOT/Backend/Server.Tests --framework net8.0
else
    echo "Skipping unit tests"
fi
