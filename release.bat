@echo off

dotnet build src/Limbo.Umbraco.UnusedMedia --configuration Release /t:rebuild /t:pack -p:PackageOutputPath=../../releases/nuget