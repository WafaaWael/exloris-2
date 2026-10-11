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
