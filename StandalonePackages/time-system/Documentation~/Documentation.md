# Time System - Technical Documentation

## Overview
Day/night cycle and time management system.

## Architecture
Time.deltaTime based accumulation with configurable day length.

## API Reference

### Main Classes
#### TimeManager
Location: `Assets/Scripts/Managers/TimeManager.cs`


## Usage Examples
ITimeService time = ServiceLocator.Get<ITimeService>();
float progress = time.DayProgress; // 0-1
int day = time.CurrentDay;
time.SetTimeScale(2.0f); // 2x speed

## Configuration
Configure day length in TimeManager inspector.

## Best Practices
- Use DayProgress for lighting transitions
- Subscribe to day change events for scheduled activities
