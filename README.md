# ServiceInstaller
A Library to handle installation of .net applications as services

## Usage

### Installation

Install the package from nuget
    Install-Package ServiceInstaller

### Usage

```C#
using ServiceInstaller;

// Check if the args include a command
var hasCmd = args.Length > 0? args[0].Count(ch => ch == '-') : 0;
switch (hasCmd)
{
    case 1:
        // Handle service request
        Console.WriteLine(ServiceController.HandleRequest(args[0], "YourServiceName", "Display Name Of Your Service"));
        break;
    default:
        {
            // Run normally
            // Your code here
            break;
        }
}
```
