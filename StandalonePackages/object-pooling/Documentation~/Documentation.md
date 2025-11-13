# Object Pooling System - Technical Documentation

## Overview
Efficient object pooling for frequently instantiated/destroyed objects.

## Architecture
Dictionary-based pools per prefab type. Automatic GameObject activation/deactivation.

## API Reference

### Main Classes
#### ObjectPool
Location: `Assets/Scripts/Core/ObjectPool.cs`


## Usage Examples
IPoolService pool = ServiceLocator.Get<IPoolService>();

// Warmup
pool.Warmup(bulletPrefab, 50);

// Get from pool
Bullet bullet = pool.Get(bulletPrefab);

// Return to pool
pool.Return(bullet);

## Configuration
Optional: Warmup pools in Start() for frequently used objects.

## Best Practices
- Warmup pools for objects spawned frequently
- Always return objects to pool when done
- Reset object state before returning to pool
