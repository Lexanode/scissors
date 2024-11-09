FROM mcr.microsoft.com/dotnet/sdk:8.0 as build
WORKDIR /app

COPY . .
RUN dotnet publish "./DiscordIS/DiscordIS.csproj" -c Release -o ./publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 as runtime

WORKDIR /app
ENV mpegFile=ffmpeg-6.0.1-amd64-static
ENV mpegExtension=.tar.xz

#parallel
RUN apt update
RUN apt install -y tar gzip build-essential
RUN apt install -y wget
RUN apt install -y libsodium-dev
RUN apt install -y libopus-dev
RUN wget "https://www.johnvansickle.com/ffmpeg/old-releases/$mpegFile$mpegExtension"
RUN tar -xvf $mpegFile$mpegExtension $mpegFile/ffmpeg
RUN cp $mpegFile/ffmpeg /usr/bin/ffmpeg

ENV XDG_CACHE_HOME=/app

COPY --from=build /app/publish .

EXPOSE 80
ENTRYPOINT ["dotnet", "DiscordIS.dll"]