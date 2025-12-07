# Unity 6.3 Performance Guidelines for Claude

## Critical Performance Rules

### 1. Component Access
- **ALWAYS** use `TryGetComponent()` instead of `GetComponent()`
- `GetComponent()` allocates memory even when no component is found
- `TryGetComponent()` is allocation-free and returns a boolean

**Example:**
```csharp
// BAD - causes allocations
var component = GetComponent<SomeComponent>();
if (component != null) { ... }

// GOOD - no allocations
if (TryGetComponent<SomeComponent>(out var component)) { ... }
```

### 2. Non-Alloc Methods
- **ALWAYS** use non-alloc variants of Unity methods
- Unity 6.3 provides many `NonAlloc` alternatives
- Common examples:
  - `Physics.RaycastNonAlloc()` instead of `Physics.Raycast()`
  - `Physics.OverlapSphereNonAlloc()` instead of `Physics.OverlapSphere()`
  - Use array buffers and reuse them

### 3. Performance First
- Performance is **TOP PRIORITY** in all code decisions
- Consider allocation impact of every line of code
- Profile and measure before optimizing, but write efficient code from the start

### 4. Minimize Garbage Collection
- Limit GC allocations as much as possible
- Avoid:
  - Creating new objects in Update/FixedUpdate loops
  - String concatenation (use StringBuilder for repeated operations)
  - LINQ queries in hot paths
  - Boxing value types
  - Unnecessary array/list creation

### 5. Debug Logging Cleanup
- When debug logs are added to diagnose an issue:
  - **Remove them immediately** when the issue is marked as "fixed"
  - Don't leave debug logs in production code
  - Use conditional compilation for debug-only code if needed

## Best Practices

### Object Pooling
- Reuse objects instead of instantiate/destroy
- Pre-allocate collections with expected capacity

### Caching
- Cache component references in Awake/Start
- Cache frequently accessed values
- Avoid repeated GetComponent calls

### Collections
- Use `List<T>` capacity constructor when size is known
- Clear and reuse collections instead of creating new ones
- Prefer arrays over lists when size is fixed

### String Handling
- Avoid string concatenation in loops
- Use `StringBuilder` for complex string building
- Cache frequently used strings

## Workflow
1. Write performance-conscious code from the start
2. Use appropriate non-alloc methods
3. Cache components and references
4. Add debug logs only when diagnosing issues
5. **Remove debug logs when issue is resolved**
6. Profile regularly to identify bottlenecks
