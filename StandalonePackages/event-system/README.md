# Event System

## Description
Type-safe event bus for decoupled communication between systems.

## Category
Core Systems

## Features
- Type-safe events
- Subscribe/Unsubscribe pattern
- No coupling between publishers and subscribers
- Easy to add new event types

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
Define events as classes. Use EventBus.Subscribe/Publish/Unsubscribe. Always unsubscribe in OnDestroy.

## Documentation
See the Documentation~ folder for detailed usage instructions.

## Support
For issues and questions, please contact support.

## License
See LICENSE file for details.
