FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
ARG DEBIAN_FRONTEND=noninteractive
ENV TZ=Asia/Ho_Chi_Minh
RUN apt-get update -y
RUN apt-get install -y tzdata
WORKDIR /app
EXPOSE 80
EXPOSE 443
ENTRYPOINT ["dotnet", "RecordingProxy.dll"]