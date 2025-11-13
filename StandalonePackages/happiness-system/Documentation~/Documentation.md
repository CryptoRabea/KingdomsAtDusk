# Happiness System - Technical Documentation

## Overview
Population happiness/morale system with tax management and building bonuses.

## Architecture
Dictionary-based bonus tracking with automatic happiness recalculation on changes.

## API Reference

### Main Classes
#### IServices
Location: `Assets/Scripts/Core/IServices.cs`

#### HappinessManager
Location: `Assets/Scripts/Managers/HappinessManager.cs`


## Usage Examples
IHappinessService happiness = ServiceLocator.Get<IHappinessService>();
float current = happiness.CurrentHappiness;
happiness.TaxLevel = 0.2f; // 20% tax

## Configuration
Set base happiness and tax settings in HappinessManager inspector.

## Best Practices
- Keep happiness above 50% for optimal gameplay
- Balance taxes with happiness bonuses from buildings
