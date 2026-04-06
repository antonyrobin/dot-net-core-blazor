# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR src

# Copy csproj and restore as distinct layers
COPY [BlazorAppBlazorApp.csproj, BlazorApp]
RUN dotnet restore BlazorAppBlazorApp.csproj

# Copy the rest of the source code
COPY . .

# Build the app
WORKDIR srcBlazorApp
RUN dotnet publish BlazorApp.csproj -c Release -o apppublish

# Use the official ASP.NET runtime image for the final container
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR app
COPY --from=build apppublish .

# Expose port 8080 for Render.com
ENV ASPNETCORE_URLS=http+8080
EXPOSE 8080

ENTRYPOINT [dotnet, BlazorApp.dll]