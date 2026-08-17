FROM mcr.microsoft.com/devcontainers/dotnet:2.1.4-11.0-preview

# This repo targets net10.0 (see Global Constraints in docs/superpowers/plans/ for why:
# the pinned 11.0 preview SDK's own toolchain doesn't ship a net10.0 SDK, but the base
# image's runtime set doesn't include net10.0 either) — install the stable net10.0
# runtime side by side so `dotnet build`/`dotnet test`/the packaged `enfusion-codegen`
# tool actually run, without affecting what the solution builds against (still the
# pinned preview SDK from global.json).
RUN curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
    && bash /tmp/dotnet-install.sh --channel 10.0 --runtime dotnet --install-dir /usr/share/dotnet \
    && rm /tmp/dotnet-install.sh
