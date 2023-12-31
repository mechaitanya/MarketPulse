#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
RUN apt update && apt -y upgrade
RUN apt install -y wget
RUN wget https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6-1/wkhtmltox_0.12.6-1.buster_amd64.deb
RUN apt install -y \
                fontconfig \
                libfreetype6 \
                libjpeg62-turbo \
                libpng16-16 \
                libx11-6 \
                libxcb1 \
                libxext6 \
                libxrender1 \
                xfonts-75dpi \
                xfonts-base
 
RUN dpkg -i wkhtmltox_0.12.6-1.buster_amd64.deb

RUN apt-get update
RUN apt-get install -y libgdiplus
RUN apt-get install -y fonts-liberation

WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["MarketPulse.csproj", "."]
RUN dotnet restore "./MarketPulse.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "MarketPulse.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MarketPulse.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Set the time zone to UTC (Etc/UTC)
RUN apt-get update && apt-get install -y tzdata
ENV TZ=Etc/UTC
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MarketPulse.dll"]