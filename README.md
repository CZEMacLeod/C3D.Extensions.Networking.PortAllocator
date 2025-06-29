# C3D.Extensions.Networking.PortAllocator

[![Build Status](https://dev.azure.com/flexviews/OSS.Build/_apis/build/status%2FCZEMacLeod.C3D.Extensions.Playwright.AspNetCore?branchName=main)](https://dev.azure.com/flexviews/OSS.Build/_build/latest?definitionId=86&branchName=main)
[![.NET](https://github.com/CZEMacLeod/C3D.Extensions.Networking.PortAllocator/actions/workflows/dotnet.yml/badge.svg)](https://github.com/CZEMacLeod/C3D.Extensions.Networking.PortAllocator/actions/workflows/dotnet.yml)

[![NuGet package](https://img.shields.io/nuget/v/C3D.Extensions.Networking.PortAllocator.svg)](https://nuget.org/packages/C3D.Extensions.Networking.PortAllocator)
[![NuGet downloads](https://img.shields.io/nuget/dt/C3D.Extensions.Networking.PortAllocator.svg)](https://nuget.org/packages/C3D.Extensions.Networking.PortAllocator)

Network port allocator for use with unit tests, playwright, aspire, etc.

Based on code originally from [CZEMacLeod/C3D.Extensions.Playwright.AspNetCore](https://github.com/CZEMacLeod/C3D.Extensions.Playwright.AspNetCore) and [CZEMacLeod/C3D.Extensions.Aspire](https://github.com/CZEMacLeod/C3D.Extensions.Aspire)

Specifically, this library provides a way to allocate ports for testing purposes, ensuring that the ports are available and not in use by other applications.
It avoids ports which are blacklisted by common browsers such as firefox and chromium, avoiding requiring the use of `network.security.ports.banned.override` or `--explicitly-allowed-ports` options.

[Read More](src/C3D/Extensions/Networking/PortAllocator/README.md)