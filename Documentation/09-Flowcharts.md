# Editable flowcharts

The diagrams below use Mermaid source and have standalone editable `.mmd` files in `Documentation/diagrams/`. They describe source/configuration paths, not play-tested runtime traces.

## A. Project overview

Shows which systems are directly coordinated and which exchange typed events. Based on code references and scene composition.

```mermaid
flowchart TB
    Scene[Scenes and prefabs] --> Menu[Menu / player / map]
    Scene --> BoatScene[BoatLevel]
    Scene --> NileScene[Nilometer]
    Menu --> Navigation[SceneManager / UnityEvents]
    Menu --> Player[Avatar selection / SaveManager]
    BoatScene --> Flow[BoatLevelFlow]
    Flow --> Actors[BoatInput / BoatController / Coin / Obstacle]
    Flow --> Bus[EventBus]
    UI[BoatLevelUI] --> Flow
    UI --> Bus
    Bus --> Audio[AudioService]
    Bus --> VFX[BoatLevelVfx]
    NileScene --> Quiz[NilometerUI]
    Quiz --> Water[WaterOperator / WaterAnimator]
    Quiz --> Dialogue[Shared DialoguePanel]
    Dialogue --> Bus
    Player --> Save[SaveSystem]
```

Source: [project-overview.mmd](diagrams/project-overview.mmd).

## B. Startup and initialization

Shows first enabled scene and known main-menu components. Ordering among unrelated Unity callbacks remains inferred because no runtime trace was collected.

```mermaid
flowchart TD
    Launch[Player launches build] --> Enabled[Unity uses first enabled build scene]
    Enabled --> Menu[main menu scene]
    Menu --> Awake[Components receive Awake/OnEnable]
    Awake --> Start[Components receive Start]
    Start --> Services[Save / scene / sound / audio components initialize]
    Start --> Organizer[SceneOrgniser invokes serialized start event]
    Services --> Ready[Menu interaction available]
    Organizer --> Ready
    Ready --> Selection[User starts and enters player selection]
```

The callback order between objects and the effect of the serialized organizer event depend on Unity scheduling and scene wiring. Source: [startup-flow.mmd](diagrams/startup-flow.mmd).

## C. Main gameplay loop — boat level

```mermaid
flowchart TD
    Intro[Intro dialogue] --> Tutorial[Tutorial]
    Tutorial -->|Start| Playing[Playing: enable boat and input]
    Playing --> Read[Read keyboard or pointer input]
    Read --> Lane[Emit lane step]
    Lane --> Move[BoatController moves within 3 lanes]
    Move --> Collision{Trigger contact?}
    Collision -->|None| Progress[Flow updates progress / speed]
    Collision -->|Coin| Coin[Collect once; increment count]
    Coin --> CoinEvents[Coin event and SFX]
    Collision -->|Obstacle| Hit[Pushback / block or bounce]
    Hit --> HitEvents[Update stars when eligible; publish hit event]
    CoinEvents --> Feedback[HUD / VFX / audio listeners]
    HitEvents --> Feedback
    Progress --> Dock{Near dock?}
    Feedback --> Dock
    Dock -->|No| Read
    Dock -->|Yes| Arrival[Disable input and start autopilot]
    Arrival --> Arrived[Dock stop, delay, arrival dialogue]
    Arrived --> Complete[Completion card]
    Complete --> Replay[Replay reloads current build scene]
    Complete --> Next[Current UI Next handler only plays tap SFX]
```

Source: [gameplay-flow.mmd](diagrams/gameplay-flow.mmd). No fail state is shown because none was identified in this state machine.

## D. Scene transitions

```mermaid
flowchart LR
    Menu[main menu] --> Player[player]
    Player --> Map[Map]
    Map --> Boat[boat/BoatLevel]
    Boat --> Nile[NiloMeter/NiloMeter]
    Boat --> Replay[Reload active scene]
    Boat --> NextHandler[Completion Next button]
    NextHandler -->|Current code| NoLoad[Does not invoke LoadNextSection]
    FlowNext[BoatLevelFlow.LoadNextSection] --> Name[Load configured _nextSceneName]
    Name --> Check{Enabled scene available?}
    Check -->|No| LoadProblem[Destination not enabled / unavailable]
    Check -->|Yes| Destination[Load destination]
```

The exact menu/map button edges rely on serialized UnityEvents and should be verified in the scene. Source: [scene-transitions.mmd](diagrams/scene-transitions.mmd).

## E. Typed gameplay event flow

```mermaid
flowchart LR
    Flow[BoatLevelFlow] --> State[LevelStateChangedEvent]
    Flow --> Progress[LevelProgressEvent]
    Flow --> Pickup[CoinCollectedEvent]
    Flow --> Hit[BoatHitObstacleEvent]
    Boat[BoatController] --> Blocked[BoatBlockedEvent]
    Flow --> Sfx[PlaySfxEvent]
    Nilometer[NilometerUI] --> Sfx
    UI[BoatLevelUI / OptionsPanel] --> Sfx
    State --> Bus[EventBus]
    Progress --> Bus
    Pickup --> Bus
    Hit --> Bus
    Blocked --> Bus
    Sfx --> Bus
    Bus --> UIConsumer[BoatLevelUI]
    Bus --> Audio[AudioService]
    Bus --> Vfx[BoatLevelVfx]
    Audio --> Voice[VoiceActivityEvent]
    Voice --> Character[CharacterExpressions]
```

Bus publishing is synchronous; only active subscriptions receive a publish. Source: [system-architecture.mmd](diagrams/system-architecture.mmd).

## F. Victory, failure, and restart

```mermaid
flowchart TD
    Boat[Boat reaches dock approach] --> Auto[Autopilot and disable input]
    Auto --> Stop[Boat reaches stop point]
    Stop --> Delay[Arrival delay]
    Delay --> Dialogue[Arrival dialogue]
    Dialogue -->|Finished callback| Win[CompleteArrival and win SFX]
    Win --> Card[Completion card with stars and coins]
    Card --> Replay[ReplayLevel reloads active scene]
    Card --> Next[UI Next button]
    Next --> NoLoad[Current handler does not load another scene]
    Stars[Stars can decrease to zero] --> NoFail[No explicit boat failure/game-over state found]
```

The Nilometer separately completes after lesson/cubit dialogue and displays remaining stars. No explicit failure screen/state was identified in either flow. Source: [gameplay-flow.mmd](diagrams/gameplay-flow.mmd).

## Additional learning and persistence flows

The detailed Nilometer quiz/water flow and save/load envelope sequence are included in the standalone [sequence-diagrams.mmd](diagrams/sequence-diagrams.mmd); the save/load static flow is also in [class-diagram.mmd](diagrams/class-diagram.mmd) where data ownership is summarized. These additional sources are linked from [UML diagrams](10-UML-Diagrams.md).
