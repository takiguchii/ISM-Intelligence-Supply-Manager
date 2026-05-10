FROM mcr.microsoft.com/dotnet/sdk:8.0

ENV DOTNET_CLI_HOME=/tmp/dotnet-home
ENV DOTNET_USE_POLLING_FILE_WATCHER=1
ENV PATH="${PATH}:/tmp/dotnet-home/.dotnet/tools"

WORKDIR /workspace/backend

RUN dotnet tool install --global dotnet-ef --version 8.0.13

COPY docker/backend/entrypoint.sh /usr/local/bin/ism-backend-entrypoint
RUN chmod +x /usr/local/bin/ism-backend-entrypoint

CMD ["ism-backend-entrypoint"]
