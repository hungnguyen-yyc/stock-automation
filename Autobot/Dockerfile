FROM mcr.microsoft.com/dotnet/runtime:6.0-windowsservercore-ltsc2022


WORKDIR /app

# Copy everything
COPY bin/Release/net6.0-windows/ /app/

RUN chmod +x ./Autobot.exe

ENTRYPOINT [ "./Autobot.exe" ]