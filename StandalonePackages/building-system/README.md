# Building System

## Description
Complete RTS building system with placement, construction, and data-driven configuration.

## Category
Gameplay Systems

## Features
- Visual placement preview (green/red)
- Grid snapping
- Collision detection
- Terrain validation
- Resource cost checking
- Construction time simulation
- Data-driven building configuration (ScriptableObjects)
- Happiness bonuses from buildings
- Resource generation buildings

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
- Resource Management System
- Happiness System
- Event System
- Service Locator
- Selection System

## Unity Package Dependencies
- com.unity.inputsystem: 1.4.4

## Quick Start
Add BuildingManager to scene. Assign BuildingDataSO assets. Connect to UI buttons.

## Documentation
See the Documentation~ folder for detailed usage instructions.

## Support
For issues and questions, please contact support.

## License
See LICENSE file for details.
