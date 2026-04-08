# Simple Pool

<p align="center">
    <a href="https://github.com/brunomikoski/SimplePool/blob/master/LICENSE.md">
        <img alt="GitHub license" src="https://img.shields.io/github/license/brunomikoski/SimplePool" />
    </a>
</p>

<p align="center">
    <a href="https://openupm.com/packages/com.brunomikoski.simplepool/">
        <img src="https://img.shields.io/npm/v/com.brunomikoski.simplepool?label=openupm&registry_uri=https://package.openupm.com" />
    </a>
    <a href="https://github.com/brunomikoski/SimplePool/issues">
        <img alt="GitHub issues" src="https://img.shields.io/github/issues/brunomikoski/SimplePool" />
    </a>
    <a href="https://github.com/brunomikoski/SimplePool/pulls">
        <img alt="GitHub pull requests" src="https://img.shields.io/github/issues-pr/brunomikoski/SimplePool" />
    </a>
    <img alt="GitHub last commit" src="https://img.shields.io/github/last-commit/brunomikoski/SimplePool" />
</p>

<p align="center">
    <a href="https://github.com/brunomikoski">
        <img alt="GitHub followers" src="https://img.shields.io/github/followers/brunomikoski?style=social">
    </a>
    <a href="https://twitter.com/brunomikoski">
        <img alt="Twitter Follow" src="https://img.shields.io/twitter/follow/brunomikoski?style=social">
    </a>
</p>

A lightweight, zero-setup object pooling system for Unity. Replace `Instantiate` / `Destroy` calls with `SimplePool.Spawn` / `SimplePool.Despawn` and let the pool manage itself — no manual wiring required.

## Features
- Drop-in replacement for `Instantiate` and `Destroy` — minimal code changes required
- Pools are created automatically on the first `Spawn` call, no setup needed
- Generic `Spawn<T>` overloads return the right component type directly
- Spawn with position, rotation, and/or parent in a single call
- Preload pools at load time to avoid runtime allocations
- Per-prefab size control via the `PoolSettings` component (initial size + max cap)
- Global defaults via a `SimplePoolSettings` ScriptableObject (Resources-based singleton)
- Lifecycle callbacks through lightweight interfaces: `IOnSpawn`, `IOnDespawn`, `IOnPool`
- Scene-aware pools — automatically despawn objects belonging to an unloaded scene
- Max pool size with oldest-active recycling when the cap is reached
- Works in Edit Mode (uses `PrefabUtility.InstantiatePrefab` instead of pooling)
- Safety checks: warns on double-despawn and throws on unexpected `Destroy` of pooled objects (configurable)


## How to use

### Basic spawn and despawn

```csharp
// Spawn a GameObject
GameObject instance = SimplePool.Spawn(myPrefab);

// Spawn with position and rotation
GameObject instance = SimplePool.Spawn(myPrefab, position, rotation);

// Spawn parented
GameObject instance = SimplePool.Spawn(myPrefab, parentTransform);

// Spawn and get a component directly
MyComponent instance = SimplePool.Spawn(myComponentPrefab, parentTransform);

// Despawn (returns to pool instead of destroying)
SimplePool.Despawn(instance);
SimplePool.Despawn(instance.gameObject);

// Despawn all pooled children under a root object
SimplePool.DespawnAllMembers(rootGameObject);
```

### Preloading

Call `Preload` before a level starts to fill the pool ahead of time and avoid allocations during gameplay:

```csharp
// Preload 10 instances of a prefab
SimplePool.Preload(myPrefab, 10);

// Preload into a specific scene (objects will be despawned when that scene unloads)
SimplePool.Preload(myPrefab, 10, targetScene);
```

### Lifecycle callbacks

Implement any of these interfaces on a `MonoBehaviour` (including on child objects) to receive pool events:

```csharp
public class MyProjectile : MonoBehaviour, IOnSpawn, IOnDespawn, IOnPool
{
    // Called once when the instance is first added to the pool
    public void OnPool() { }

    // Called every time the object is spawned
    public void OnSpawn() { }

    // Called every time the object is despawned
    public void OnDespawn() { }
}
```

### Per-prefab pool settings

Add a `PoolSettings` component to your prefab to control its pool size without touching code:

| Field | Description |
|---|---|
| **Pool Size** | Number of instances created at startup |
| **Max Pool Size** | Hard cap on total instances (`-1` = unlimited). When the cap is reached, the oldest active object is recycled. |

### Scene unloading

If you enable **Despawn On Scene Unload** in `SimplePoolSettings`, call `OnBeforeSceneUnload` before unloading a scene to return its objects to the pool:

```csharp
SimplePool.OnBeforeSceneUnload(sceneBeingUnloaded);
SceneManager.UnloadSceneAsync(sceneBeingUnloaded);
```

### Pool management

```csharp
// Add extra objects to an existing pool at runtime
SimplePool.AddObjectsToPool(myPrefab, 5);

// Destroy an entire pool and all its instances
SimplePool.DestroyPool(myPrefab);

// Query helpers
bool inPool = SimplePool.BelongsToAPool(gameObject, out PoolMember member);
bool hasPool = SimplePool.HasPoolForItem(myPrefab);
```

### Global settings

Create a `SimplePoolSettings` asset via `Assets > Create > SimplePoolSettings` and place it in a `Resources` folder. Available options:

| Setting | Description |
|---|---|
| **Default Pool Size** | Initial size used when no `PoolSettings` component is present (default: 3) |
| **Despawn On Scene Unload** | Automatically despawn scene objects before unload (requires `OnBeforeSceneUnload` call) |
| **Allow Destruction Of Pooled Items** | Suppress the exception thrown when a pooled object is destroyed via `Object.Destroy` |


## System Requirements
Unity 2018.4.0 or later


## How to install

<details>
<summary>Add from OpenUPM <em>| via scoped registry, recommended</em></summary>

This package is available on OpenUPM: https://openupm.com/packages/com.brunomikoski.simplepool

To add the package to your project:

- open `Edit/Project Settings/Package Manager`
- add a new Scoped Registry:
  ```
  Name: OpenUPM
  URL:  https://package.openupm.com/
  Scope(s): com.brunomikoski
  ```
- click <kbd>Save</kbd>
- open Package Manager
- click <kbd>+</kbd>
- select <kbd>Add from Git URL</kbd>
- paste `com.brunomikoski.simplepool`
- click <kbd>Add</kbd>
</details>

<details>
<summary>Add from GitHub | <em>not recommended, no updates :(</em></summary>

You can also add it directly from GitHub on Unity 2019.4+. Note that you won't be able to receive updates through Package Manager this way, you'll have to update manually.

- open Package Manager
- click <kbd>+</kbd>
- select <kbd>Add from Git URL</kbd>
- paste `https://github.com/brunomikoski/SimplePool.git`
- click <kbd>Add</kbd>
</details>
