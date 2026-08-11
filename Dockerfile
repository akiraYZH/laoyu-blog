FROM mcr.microsoft.com/dotnet/sdk:10.0 AS development

WORKDIR /src

COPY ["laoyu-blog-backend.csproj", "./"]

RUN dotnet restore "laoyu-blog-backend.csproj"

COPY . .

ENV ASPNETCORE_HTTP_PORTS=8080

ENV DOTNET_USE_POLLING_FILE_WATCHER=1

EXPOSE 8080

CMD ["dotnet", "watch", "--no-hot-reload", "run", "--no-launch-profile"]


FROM development AS build

RUN dotnet publish "laoyu-blog-backend.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

USER $APP_UID

ENTRYPOINT ["dotnet", "laoyu-blog-backend.dll"]