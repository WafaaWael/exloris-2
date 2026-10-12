# Gameplay flows

The diagrams are embedded Mermaid source. Standalone scripts live in [docs/diagrams](diagrams/) for Mermaid Live or compatible documentation tools.

## Build scene sequence

```mermaid
flowchart LR
    Menu[Main menu] --> Avatar[Player selection]
    Avatar --> Map[Map]
    Map --> Boat[Boat level]
    Boat --> Nilometer[Nilometer]
```

This is the enabled scene sequence, not a verified navigation graph. Exact buttons and transitions are scene-configured, so test all serialized destinations after modifying scenes.

## Boat-level state flow

```mermaid
flowchart TD
    Intro[Intro dialogue] --> Tutorial[Tutorial overlay]
    Tutorial -->|Tap start| Playing[Playing]
    Playing -->|Help| Paused[Paused/tutorial overlay]
    Paused -->|Tap continue| Playing
    Playing -->|Settings| Settings[Settings]
    Settings -->|Close| Playing
    Playing -->|Near dock| Arriving[Autopilot arriving]
    Arriving -->|Dock reached| Arrived[Arrival dialogue]
    Arrived -->|Dialogue complete| Complete[Completion card]
    Complete -->|Replay| Replay[Reload current scene]
    Complete -->|Next| Next[Load configured next scene]
    Replay --> Intro
```

## Boat gameplay loop

```mermaid
flowchart TD
    Input[Keyboard, swipe, or tap] --> Lane[BoatInput emits lane step]
    Lane --> Move[BoatController changes lane]
    Move --> Trigger{Trigger overlap?}
    Trigger -->|Coin| Coin[Coin collect animation]
    Coin --> CoinEvent[CoinCollectedEvent + coin SFX]
    Trigger -->|Obstacle| Hit[Boat pushes back / blocks lane]
    Hit --> HitEvent[BoatHitObstacleEvent + bump SFX]
    CoinEvent --> Feedback[HUD, VFX, audio update]
    HitEvent --> Feedback
    Feedback --> Input
```

An obstacle only costs one star for its first eligible hit. A forward collision blocks the current lane until the player changes lane; a sideways collision returns the boat to its previous lane.

## Nilometer learning flow

```mermaid
flowchart TD
    Start[Intro dialogue] --> RisePrompt[Water-rise prompt]
    RisePrompt --> Move[WaterOperator moves water]
    Move --> Question[Show question Tail]
    Question --> Answer{Answer correct?}
    Answer -->|Yes| Correct[Correct feedback and dialogue]
    Answer -->|No, first miss| Almost[Remove star, disable answer, hint / retry]
    Almost --> Question
    Answer -->|No, second miss| Explain[Remove star and explanation]
    Correct --> More{More levels?}
    Explain --> More
    More -->|Yes| RisePrompt
    More -->|No| Lesson[Lesson and cubit card]
    Lesson --> Complete[Completion display]
```

Each level permits two missed answers before it advances. Correct answers use a temporary highlighted state before the next explanation or level transition.

## Event and feedback pipeline

```mermaid
flowchart LR
    Input[Boat input / collisions / UI] --> Flow[BoatLevelFlow]
    Flow --> State[LevelStateChangedEvent]
    Flow --> Progress[LevelProgressEvent]
    Flow --> Score[CoinCollectedEvent / BoatHitObstacleEvent]
    Flow --> Sound[PlaySfxEvent]
    Nilometer[NilometerUI] --> Sound
    UI[BoatLevelUI / OptionsPanel] --> Sound
    State --> Bus[Typed EventBus]
    Progress --> Bus
    Score --> Bus
    Sound --> Bus
    Bus --> BoatUi[BoatLevelUI]
    Bus --> Audio[AudioService]
    Bus --> Vfx[BoatLevelVfx]
    Audio --> Voice[VoiceActivityEvent]
    Voice --> Expressions[CharacterExpressions]
```

The `EventBus` is synchronous and transient: it notifies active listeners but does not store or replay state. The standalone source is [event-pipeline.mmd](diagrams/event-pipeline.mmd).

## Save and load flow

```mermaid
flowchart TD
    Player[player component] --> Manager[SaveManager]
    Manager --> Save[SaveSystem.SavePlayer]
    Save --> Json[JsonUtility serializes PlayerData]
    Json --> Encrypt[AES-CBC encryption with random IV]
    Encrypt --> Sign[HMAC-SHA256 over IV + ciphertext]
    Sign --> Encode[Build EX2S envelope and Base64 encode]
    Encode --> File[player.save in persistentDataPath]
    File --> Read[LoadPlayer reads Base64 text]
    Read --> Header{EX2S header valid?}
    Header -->|No| Fail[Log failure and return null]
    Header -->|Yes| Verify{HMAC valid?}
    Verify -->|No| Fail
    Verify -->|Yes| Decrypt[AES-CBC decrypt]
    Decrypt --> Deserialize[JsonUtility deserializes PlayerData]
    Deserialize --> Manager
```

The standalone source is [save-load-flow.mmd](diagrams/save-load-flow.mmd). Save data is integrity-checked before decryption; still treat the shipped client-side key as a platform-security concern, not complete anti-tamper protection.

## Scene navigation and completion behavior

```mermaid
flowchart TD
    Start[Open main menu scene] --> Menu[main menu]
    Menu --> Player[player]
    Player --> Map[Map]
    Map --> Boat[boat/BoatLevel]
    Boat --> Nilometer[NiloMeter/NiloMeter]
    Boat --> Replay[ReplayLevel reloads active scene]
    Boat --> Next{Completion Next enabled?}
    Next -->|Current UI handler| Disabled[No scene load: LoadNextSection is commented out]
    Next -->|If enabled| Configured[Load _nextSceneName]
    Configured --> Check{Scene is in build settings?}
    Check -->|Yes| Destination[Load destination]
    Check -->|No| Error[Unity scene-load error]
```

The standalone source is [scene-navigation.mmd](diagrams/scene-navigation.mmd). The player route reflects enabled build scenes; the final links between menu/map/boat/Nilometer are scene-configured and should be play-tested after any scene rename or build-list change.
