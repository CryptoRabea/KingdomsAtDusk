# Object Pooling System

## Description
Efficient object pooling for frequently instantiated/destroyed objects.

## Category
Performance Systems

## Features
- Automatic pool creation
- Warmup support
- Generic type support
- Pool clearing

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

## Dependencies
This system requires the following other systems:
- Service Locator

## Quick Start
Use IPoolService.Get<T>(prefab) instead of Instantiate. Return with IPoolService.Return<T>(instance).

## Documentation
See the Documentation~ folder for detailed usage instructions.

## Support
For issues and questions, please contact support.

## License
See LICENSE file for details.
