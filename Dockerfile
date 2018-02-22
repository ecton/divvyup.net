FROM microsoft/dotnet:2.0-runtime

EXPOSE 5000

ADD DivvyUp.Web/bin/Release/netcoreapp2.0/publish/ /app

ADD DivvyUp.Web/appsettings.json /app/
WORKDIR /app

ENV ASPNETCORE_URLS http://*:5000
ENV ASPNETCORE_ENVIRONMENT Production

CMD dotnet DivvyUp.Web.dll
