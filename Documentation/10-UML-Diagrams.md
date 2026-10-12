# UML-style diagrams

The Mermaid diagrams below use code-supported relationships. A serialized field reference is shown as an association/dependency, not composition, unless object lifetime ownership is explicit in code. Diagrams are intentionally limited to system slices rather than every imported class.

## 1. Boat system class diagram

Shows the coordinator, input/controller delegates, events, and consumers. It does not show all private tuning fields or every visual helper.

```mermaid
classDiagram
    class BoatLevelFlow {
        +LevelState State
        +int Stars
        +int Coins
        +CompleteIntro()
        +StartPlaying()
        +OpenHelp()
        +Opensettings()
        +CloseSettings()
        +CompleteArrival()
        +ReplayLevel()
        +LoadNextSection()
    }
    class BoatInput {
        +bool IsInputEnabled
        +event OnLaneStep
    }
    class BoatController {
        +bool IsMoving
        +bool IsBlocked
        +int Lane
        +StepLane(direction)
        +SetProgress(progress01)
        +StartAutoPilot(stopY)
        +HasReachedAutoPilotStop()
    }
    class Coin {
        +bool IsCollected
        +Collect()
    }
    class Obstacle
    class BoatLevelUI
    class BoatLevelVfx
    class AudioService
    class EventBus
    class IEvent
    class EventPayloads {
        <<structs>>
        LevelStateChangedEvent
        CoinCollectedEvent
        BoatHitObstacleEvent
        LevelProgressEvent
        BoatBlockedEvent
        PlaySfxEvent
    }
    BoatLevelFlow --> BoatController : serialized reference / callbacks
    BoatLevelFlow --> BoatInput : serialized reference / lane callback
    BoatController --> Coin : trigger query
    BoatController --> Obstacle : trigger query
    BoatLevelUI ..> BoatLevelFlow : Service resolver
    BoatLevelFlow ..> EventBus : publishes
    BoatController ..> EventBus : publishes
    BoatLevelUI ..> EventBus : subscribes / publishes
    BoatLevelVfx ..> EventBus : subscribes
    AudioService ..> EventBus : subscribes
    EventPayloads ..|> IEvent
```

Purpose: document ownership and communication for the namespaced boat feature. `BoatController` invokes flow callbacks; UI/audio/VFX use events. Source: `Assets/scripts/GameplaySystems/BoatLevel/` and `CoreData/BoatLevelEvents.cs`.

## 2. Nilometer class diagram

```mermaid
classDiagram
    class NilometerUI {
        +OnButtonClick_Hint()
        +SubmitAnswer(levelIndex, answerIndex)
        -BeginWaterLevel(levelIndex)
        -CompletePendingDialogue(pending)
    }
    class WaterOperator {
        +LevelCount
        +CurrentLevelIndex
        +MoveToLevel(levelIndex)
        +MoveToNextLevel()
        +IsCorrectAnswer(levelIndex, answerIndex)
        +WaterLevelReached
    }
    class WaterAnimator {
        +AnimateYSize(targetY, onComplete, duration)
        +AnimateYSizeAndReturn(targetY)
    }
    class Tail {
        +SetGlow(tick, glow)
    }
    class SharedDialoguePanel {
        +PlayLines(lines, isAutoAdvance, isLastLineKept)
        +Hide()
        +OnFinished
        +OnLineStarted
        +OnVoiceStarted
    }
    class CharacterExpressions {
        +SetMood(mood)
        +PlayHop()
    }
    class StarsDisplay {
        +ShowStars(count, isAnimated)
    }
    class EventBus
    NilometerUI --> WaterOperator : calls / subscribes
    WaterOperator --> WaterAnimator : serialized reference
    NilometerUI --> Tail : answer panels
    NilometerUI --> SharedDialoguePanel : calls / callbacks
    NilometerUI --> CharacterExpressions : mood and reaction
    NilometerUI --> StarsDisplay : star feedback
    NilometerUI ..> EventBus : publishes SFX
```

Purpose: show that the UI coordinates quiz state while `WaterOperator` owns stage rules and water animation. A `Tail` panel/button binding is serialized; exact per-button wiring should be verified in the scene.

## 3. Persistence class diagram

```mermaid
classDiagram
    class player {
        +int Level
        +int Step
        +SetGender(selectedGender)
    }
    class PlayerData {
        +int level
        +int step
        +PlayerGender gender
    }
    class SaveManager {
        +PlayerData CurrentData
        +RegisterPlayer(player)
        +SaveCurrentPlayer()
        +LoadData()
        +LoadCurrentPlayer()
        +DeleteSave()
        +CreateNewPlayer(gender)
    }
    class SaveSystem {
        <<static>>
        +SavePlayer(player)
        +LoadPlayer() PlayerData
        +DeletePlayerSave()
    }
    class SaveSecrets {
        <<internal static>>
    }
    player --> PlayerData : mapped values
    SaveManager --> player : active player reference
    SaveManager --> SaveSystem : calls
    SaveSystem --> PlayerData : serializes / deserializes
    SaveSystem ..> SaveSecrets : key dependency
```

Purpose: clarify player/session ownership versus file serialization. The actual save payload includes the fields serialized by `PlayerData`; this diagram omits secret contents.

## 4. Boat state diagram

```mermaid
stateDiagram-v2
    [*] --> Intro
    Intro --> Tutorial: CompleteIntro
    Tutorial --> Playing: StartPlaying
    Playing --> Paused: OpenHelp
    Paused --> Playing: StartPlaying
    Playing --> Settings: Opensettings
    Settings --> Playing: CloseSettings
    Playing --> Arriving: dock threshold reached
    Arriving --> Arrived: autopilot stop + delay
    Arrived --> Complete: CompleteArrival after dialogue
    Complete --> Intro: ReplayLevel reloads scene
```

`Lesson` is present in the `LevelState` enum but is not entered by the current `BoatLevelFlow` path. No failure state is defined in this state machine.

## 5. Sequence diagram — boat obstacle hit

```mermaid
sequenceDiagram
    participant User
    participant Input as BoatInput
    participant Boat as BoatController
    participant Flow as BoatLevelFlow
    participant Bus as EventBus
    participant UI as BoatLevelUI
    participant VFX as BoatLevelVfx
    participant Audio as AudioService
    User->>Input: keyboard / swipe / tap
    Input->>Flow: OnLaneStep(direction)
    Flow->>Boat: StepLane(direction)
    Boat->>Boat: FixedUpdate movement / trigger
    Boat->>Flow: OnObstacleHit(obstacle, isFirstHit)
    Flow->>Bus: PlaySfxEvent(Bump)
    opt first eligible hit and stars remain
        Flow->>Flow: decrement Stars
        Flow->>Bus: PlaySfxEvent(StarLost)
    end
    Flow->>Bus: BoatHitObstacleEvent(stars, position)
    Bus-->>UI: update star display
    Bus-->>VFX: spawn impact / shake
    Bus-->>Audio: play registered SFX
```

The sequence follows direct C# callbacks and typed subscriptions in the source. Physics timing and scene listener activation were not runtime-tested.

## 6. Sequence diagram — Nilometer answer

```mermaid
sequenceDiagram
    participant User
    participant Tail as Question button
    participant UI as NilometerUI
    participant Water as WaterOperator
    participant Dialogue as Shared DialoguePanel
    participant Bus as EventBus
    User->>Tail: select answer
    Tail->>UI: SubmitAnswer(levelIndex, answerIndex)
    UI->>Water: IsCorrectAnswer(levelIndex, answerIndex)
    alt correct
        UI->>Bus: PlaySfxEvent(Correct)
        UI->>UI: feedback animation and dialogue progression
    else incorrect
        UI->>UI: decrement stars / increment attempts / disable answer
        UI->>Bus: PlaySfxEvent(Wrong)
        alt first miss
            UI->>Dialogue: play almost line
            Dialogue-->>UI: finished callback; resume question
        else second miss
            UI->>Dialogue: play sorry line
            Dialogue-->>UI: finished callback; next level or lesson
        end
    end
```

The selected answer index is serialized into button events and validated against current WaterLevel data.

## 7. Sequence diagram — save/load

```mermaid
sequenceDiagram
    participant Player as player
    participant Manager as SaveManager
    participant System as SaveSystem
    participant File as player.save
    Player->>Manager: RegisterPlayer / SaveCurrentPlayer
    Manager->>System: SavePlayer(player)
    System->>System: JsonUtility serialize PlayerData
    System->>System: AES-CBC encrypt with IV
    System->>System: HMAC-SHA256 and envelope
    System->>File: Base64 write
    Manager->>System: LoadPlayer()
    System->>File: read text
    System->>System: validate header and HMAC
    alt valid envelope
        System->>System: decrypt and deserialize
        System-->>Manager: PlayerData
    else invalid or unreadable
        System-->>Manager: null and log error
    end
```

## Diagram limits

Mermaid class diagrams cannot show Unity serialized object identity or scene activation states. Diagram lines represent code-level dependencies, callbacks, or serialized references as labeled. `Composition` is not used unless lifetime ownership is proven.
