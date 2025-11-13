# Resource Management System

## Description
Complete resource management system with support for multiple resource types (Wood, Food, Gold, Stone). Data-driven design makes adding new resources trivial.

## Category
Core Systems

## Features
- Multiple resource types support
- Resource spending and affordability checks
- Event-driven resource updates
- Helper class for building resource dictionaries
- Easy to extend with new resource types

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
- Event System
- Service Locator

## Quick Start
Add ResourceManager component to a GameObject. Use ServiceLocator.Register() to register it. Access via IResourcesService interface.

## Documentation
See the Documentation~ folder for detailed usage instructions.

## Support
For issues and questions, please contact support.

## License
See LICENSE file for details.
