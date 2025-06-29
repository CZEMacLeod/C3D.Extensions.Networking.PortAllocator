# C3D.Extensions.Networking.PortAllocator
Network port allocator for use with unit tests, playwright, aspire, etc.

Based on code originally from [CZEMacLeod/C3D.Extensions.Playwright.AspNetCore](https://github.com/CZEMacLeod/C3D.Extensions.Playwright.AspNetCore) and [CZEMacLeod/C3D.Extensions.Aspire](https://github.com/CZEMacLeod/C3D.Extensions.Aspire)

Specifically, this library provides a way to allocate ports for testing purposes, ensuring that the ports are available and not in use by other applications.
It avoids ports which are blacklisted by common browsers such as firefox and chromium, avoiding requiring the use of `network.security.ports.banned.override` or `--explicitly-allowed-ports` options.

## Installation
You can install the package via NuGet:
```shell
dotnet add package C3D.Extensions.Networking.PortAllocator
```

## Usage

To use the port allocator, you can create an instance of `PortAllocator` and call the `GetRandomFreePort` method to get an available port.
```csharp
using C3D.Extensions.Networking;

var portAllocator = new PortAllocator();
int port = portAllocator.GetRandomFreePort();
Console.WriteLine($"Allocated port: {port}");
```

`PortAllocator` supports dependency injection, so you can register it in your `Startup.cs` or `Program.cs` file:
```csharp
using C3D.Extensions.Networking;

services.AddSingleton<PortAllocator>();
```
You can then inject `PortAllocator` into your classes and use it to allocate ports as needed.
When using dependency injection, actions will be logged using the `ILogger<PortAllocator>` interface, which you can configure in your application.

You can also just use the default instance provided by the `PortAllocator.Instance` property:
```csharp
using C3D.Extensions.Networking;
int port = PortAllocator.Instance.GetRandomFreePort();
```

There is also a constructor that allows you to specify the seed used for the random port generation.

You can also specify a range of ports to search for available ports:
```csharp
int port = portAllocator.GetRandomFreePort(8000,9000); // Allocates a port between 8000 and 9000 inclusive
```

There are also utility methods to check if a port is available or to free it again:
```csharp
if (portAllocator.TryMarkPortAsUsed(12345)) {
	// Port 12345 is now marked as used
	// Do some work with the port

	portAllocator.MarkPortAsFree(12345); // Free the port when done
}
```