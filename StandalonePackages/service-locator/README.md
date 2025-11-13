# Service Locator

## Description
Dependency injection pattern implementation for accessing game services.

## Category
Core Systems

## Features
- Interface-based service registration
- Type-safe service retrieval
- Global service access without singletons
- Easy to test and mock

## Installation

### Via Unity Package Manager
1. Open Unity Package Manager (Window > Package Manager)
2. Click the '+' button and select 'Add package from disk...'
3. Navigate to the package.json file in this directory

### Via Direct Import
1. Copy the entire package folder to your project's Assets directory
2. Unity will automatically import the scripts

## Requirements
- Unity 2021.3+

## Quick Start
Register services with ServiceLocator.Register<IService>(implementation). Access via ServiceLocator.Get<IService>().

## Documentation
See the Documentation~ folder for detailed usage instructions.

## Support
For issues and questions, please contact support.

## License
See LICENSE file for details.
