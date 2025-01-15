# Architecture

Related repository name: `architecture`.

Table of content:
1. Overall architecture diagram 
2. Concept description
    - Views, Services, and Controllers
    - ViewModels
    - Assertions
    - Config and Data
    - SceneReferenceHolders
    - Assembly-level runtime data and collections
    - Debug Commands (cheating)
<br></br>

## Overall architecture diagram 
![image info](./Images/Architecture.png)
</br>
**Assemblies present in every game**

Green arrows indicate that source assembly can only call public methods of the `ViewModel` of the target assembly.
Dashed lines mean that given assembly/dll is identical (shared) in all games.

## Concept description
### Views, Systems, and Controllers

View is a Mono Behaviour that job is to be a link between game object world and C# world. It is also the main Mono of the whole game object and by convention it is put on the top of the hierarchy to emphasize that. It has all Start, Awake and other life-cycle methods (no Updates tho as these are centralized in BootView).

In essense, view is the connection between game object world and C# world. If possible, it should only have reference variables (`[SerializedField]` or public) and/or basic game logic. In case of logic getting too large we should consider moving some of it to a dedicated controller/system.

![image info](./Images/SpawnPointView.png)
</br>
**Example of a View**

Above we see an example of a view. It has three `[SerializedField]` fields that are statically (in editor) assigned. By having only one component that does that we know where exactly to look for references and related logic.

Below we see the code of the SpawnPoint `View`:
``` csharp
class SpawnPointView : MonoBehaviour
{
    [SerializeField]
    PlayerId _playerId;

    [SerializeField]
    MeshFilter _meshFilter;

    [SerializeField]
    MeshRenderer _meshRenderer;

    void Awake()
    {
        Destroy(_meshFilter);
        Destroy(_meshRenderer);
    }
}
```

The last two remaining words in the acronym are: `Model` and `Controller`.
`Model` referes to classes or structs that store data. In our architecture we call them `DTOs` (for example: `SpellDto`) which stands for `Data Transfer Object`. However, as many things in computer science it is nothing more than just an arbitrary chosen word to help us distinguish one type of a class from another. So just remember that `Model` = `Dto` = "class/struct that holds data".

The second word - `Controller` - refers to classes that do something. It can be either an instantiatated controller or a static system.

**Where to use each:**

Does your class need to be a Mono?
</br>
Yes -> `View`
</br>
No -> Static is enough? 
- Yes -> `Service`
- No -> `Controller`

### ViewModels

ViewModel is a static class that holds all the public methods this assembly exposes (exception may apply due to language constaits, for example interface implementations are always public).
Only certain assemblies have `ViewModels` (`GameLogic`, `DataOriented`, `Presentation`, and `UI`).

The purpose of a `ViewModel` is to have a dedicated place where you can look for how to interact with the given assembly and to store logic that normally would have to be distributed between two or more controllers. However, if this logic gets too big it should be moved to given’s assembly `MainController`.
So essentially thank to `ViewModels` all other methods, properties and variables can be private or internal.
In case of `ViewModels` getting too large consider moving some of their logic to a controller and/or making the `ViewModel` partial.

``` csharp
[UsedImplicitly]
public class UIViewModel : IInitializable
{
    static readonly UIConfig _uiConfig; // some config injected via dependency injector

    [Inject]
    static readonly UIMainController _uiMainController; // controllers are also injected

    // classes instatiated by dependency injector need an empty parameterlesss constuctor with Preserve attribute to be compatible with high level of code stripping (IL2CPP)
    [Preserve]
    UIViewModel() { }

    // called once by dependency injector after instantiation phase
    public void Initialize() => InputSystem.Initialize();

    // Unity life-cycle synchronization methods (Custom Fixed, Normal and Late Update)
    public static void CustomFixedUpdate() => _uiMainController.CustomFixedUpdate();
    
    // [...]

    // State transition methods (OnExit and OnEntry)
    public static void BootingOnExit() { }

    public static void MainMenuOnEntry() => _uiConfig.InputActionAsset
        .FindActionMap(UIConstants.MainMenuActionMap).Enable();

    // [...]    
    
    // Normal methods to be normally called from some other assemblies
    public static void ToggleBossHpBar() { /* some code */ }

    // [...]
}
```

Above we can see a typical content of a `ViewModel`. An injected field, a method that is just passing the control flow, a more complicated method that is the actual reason why `ViewModel` is useful, and a scene control flow related method.

### Assertions

Some are easy:

``` csharp
public static void SetMusicVolume(int music)
{
    Assert.IsTrue(music is >= 0 and <= 10, "Volume must be represented by a value ranging from 0 to 10.");
    MusicSystem<Music>.Volume = music;
}
```

Some are more complecated:
``` csharp
Delegate reactMethods = SignalProcessorInternal.GetReactMethods(signalName);
Assembly targetAssembly = reactMethods.Method.DeclaringType!.Assembly;

Assembly callingAssembly = new System.Diagnostics.StackTrace().GetFrame(3).GetMethod().DeclaringType!.Assembly;
string callingName = callingAssembly.GetName().Name.Split('.')[1];
string receivingName = targetAssembly.GetName().Name.Split('.')[1];
int callingId = _lookup[callingName];
int receivingId = _lookup[receivingName];

Assert.IsTrue(_allowance[callingId, receivingId], Message());
```

But the over all idea is always the same - to create a net for bugs and save other programmers' time by immediatelly throwing an exception whenever something immpossible happen.

Keep in mind some assertions may be simply useless like the one below that essentially throws an error when the list is null despite the fact the language itself would do exactly the same. **Do not write such assertions as they only look smart**.

``` csharp
public static void DoSomething(List<int> list)
{
    Assert.IsFalse(list == null, "List cannot be null.");
    list.Add(1);
}
```

### Config and Data
Both `Config` and `Data` are `ScriptibleObjects`.
Technically they could all be `Configs` but as project grows it is a good idea to structure them somehow.

Suggested division is:
- per application - `Config`
- per instance (for example type of enemy) - `Data`

Ultimately, the decision is up to a programmer. If you think there is too many fields in a config you can move some of them to a Data file.

`Configs` are injected. `Data` files are simply referenced inside `Configs`.

![image info](./Images/ConfigAndData.png)
</br>
**Example of a config, where it is and what it holds**

**FAQ:**
</br>
Q: Can I store some variables in a config/data file and update them in runtime?
</br>
A: No. Config/Data are meant to be read only. Think of them as a set of parameters that the game start with. In terms their use case, the only difference between a config/data and a constant is that the first is nicely exposed for desingers to modify.

### Reference Holders
Sometimes we just want to have some references to a scene stored conviniently so that we don't have to find objects by tags or names.

![image info](./Images/ReferenceHolders.png)
</br>
**Example of a SceneReferenceHolder**

``` csharp
class PresentationSceneReferenceHolder : MonoBehaviour
{
    internal static Transform VfxContainer;
    // [...]

    [SerializeField]
    Transform _vfxContainer;
    // [...]

    void Awake()
    {
        VfxContainer = _vfxContainer;
        // [...]
    }
}
```

Rather straight forward.

### Assembly-level runtime data and collections

Runtime data are typically various DTOs created during gameplay.
Each DTO should have assigned an id that allows for an easy find. If it is suitable, dto sets should be encapsulated with collections.
Assembly level data is just a static class that contains all these collections.

``` csharp
static class PresentationData
{
    /// <summary>
    /// This will contain a reference only when in a single player mode.
    /// </summary>
    internal static PlayerView Player;
}
```
