FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/ShopFlow.Api/ShopFlow.Api.csproj src/ShopFlow.Api/
RUN dotnet restore src/ShopFlow.Api/ShopFlow.Api.csproj

COPY src/ShopFlow.Api/ src/ShopFlow.Api/
RUN --mount=type=secret,id=sixlabors_license,required=true dotnet publish src/ShopFlow.Api/ShopFlow.Api.csproj -c Release -o /app/publish --no-restore -p:SixLaborsLicenseFile=/run/secrets/sixlabors_license

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
RUN mkdir -p /app/App_Data/product-images && chown "$APP_UID" /app/App_Data/product-images

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "ShopFlow.Api.dll"]
