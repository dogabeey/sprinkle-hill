# Event System Implementation Summary

This document summarizes all the EventManager.TriggerEvent calls added throughout the project.

## New GameEvent Enum Values Added

The following events have been added to the `GameEvent` enum in `EventManager.cs`:

### Element and Grid Events
- `ELEMENTS_SWAPPED` - Triggered when two elements are swapped
- `SWAP_FAILED` - Triggered when a swap doesn't result in a match
- `MATCH_DETECTED` - Triggered when a match is detected
- `COMBO_TRIGGERED` - Triggered when a combo/chain match occurs
- `GRAVITY_STARTED` - Triggered when gravity starts applying
- `GRAVITY_COMPLETED` - Triggered when gravity finishes
- `ELEMENTS_REFILLED` - Triggered when new elements are spawned
- `GRID_INITIALIZED` - Triggered when the grid is fully initialized
- `GRID_STABLE` - Triggered when the grid has no more matches and is stable

### Currency Events
- `CURRENCY_EARNED` - Triggered when currency is earned
- `CURRENCY_SPENT` - Triggered when currency is spent

### Action Bar Events
- `ACTION_USED` - Reserved for future use when actions are executed
- `ACTION_CLICKED` - Triggered when an action bar button is clicked

### UI Events
- `SCREEN_OPENED` - Triggered when a screen is opened
- `SCREEN_CLOSED` - Triggered when a screen is closed

### Special Elements
- `BOX_DESTROYED` - Triggered when a custom box is destroyed
- `SPECIAL_ELEMENT_ACTIVATED` - Reserved for future special element activations

### Ad Events
- `AD_SHOWN` - Triggered when an interstitial ad is shown
- `AD_CLOSED` - Triggered when an interstitial ad is closed
- `AD_FAILED` - Triggered when an ad fails to show
- `REWARDED_AD_SHOWN` - Triggered when a rewarded ad is shown
- `REWARDED_AD_COMPLETED` - Triggered when a rewarded ad is completed
- `REWARDED_AD_FAILED` - Triggered when a rewarded ad fails

### Sound Events
- `SOUND_PLAYED` - Triggered when a sound effect is played
- `MUSIC_STARTED` - Triggered when music starts playing
- `MUSIC_STOPPED` - Triggered when music is stopped
- `SOUND_SETTINGS_CHANGED` - Triggered when sound/music/vibration settings change

## Event Trigger Locations

### Match3Grid.cs
**Location: SwapAndMatch()**
- `ELEMENTS_SWAPPED` - Triggered after elements are swapped
  - Parameters: vectorList (contains positions of swapped elements)

**Location: MatchProcess()**
- `MATCH_DETECTED` - Triggered when matches are found
  - Parameters: paramInt (number of match groups)
- `COMBO_TRIGGERED` - Triggered for chain matches (combo > 1)
  - Parameters: paramInt (combo count)
- `ELEMENT_MATCHED` - Triggered for each match group
  - Parameters: paramInt (number of elements in group)
- `SWAP_FAILED` - Triggered when swap results in no matches
  - Parameters: vectorList (positions of elements that were swapped back)
- `GRID_STABLE` - Triggered when all matches are processed and grid is stable

**Location: ApplyGravity()**
- `GRAVITY_STARTED` - Triggered at the start of gravity application
- `GRAVITY_COMPLETED` - Triggered after gravity finishes
- `ELEMENTS_REFILLED` - Triggered after new elements spawn
  - Parameters: paramInt (number of refilled elements)

### Match3GridInputController.cs
**Location: SetSelectedElement()**
- `ELEMENT_SELECTED` - Triggered when a player selects an element
  - Parameters: paramObj (selected element GameObject), paramScriptable (ElementData)

### Grid3D.cs
**Location: Init()**
- `GRID_INITIALIZED` - Triggered after grid initialization completes
  - Parameters: paramInt (total grid cell count)

### GridElement_Match3Game.cs
**Location: DestroyElement()**
- `ELEMENT_DESTROYED` - Triggered when an element is destroyed (already existed)
  - Parameters: paramScriptable (ElementData)

### CurrencyManager.cs
**Location: AddCoin()**
- `CURRENCY_EARNED` - Triggered when coins are added
  - Parameters: paramStr ("Coin"), paramFloat (amount), paramObj (source GameObject)

**Location: AddCurrency()**
- `CURRENCY_EARNED` - Triggered when premium currency is added
  - Parameters: paramStr (currency ID), paramFloat (amount), paramObj (source GameObject)

**Location: SpendCoin()** (New Method)
- `CURRENCY_SPENT` - Triggered when coins are spent
  - Parameters: paramStr ("Coin"), paramFloat (amount)

**Location: SpendCurrency()** (New Method)
- `CURRENCY_SPENT` - Triggered when premium currency is spent
  - Parameters: paramStr (currency ID), paramFloat (amount)

### ActionBarItem.cs
**Location: Start()**
- `ACTION_CLICKED` - Triggered when an action bar button is clicked
  - Parameters: paramStr (action name)

### ScreenManager.cs
**Location: Show(GameScreen)**
- `SCREEN_CLOSED` - Triggered for each screen being closed
  - Parameters: paramObj (screen GameObject), paramInt (screen ID as int)

**Location: Show(Screens)**
- `SCREEN_CLOSED` - Triggered for each screen being closed
  - Parameters: paramObj (screen GameObject), paramInt (screen ID as int)

**Location: ShowScreen()**
- `SCREEN_OPENED` - Triggered when a screen is opened
  - Parameters: paramObj (screen GameObject), paramInt (screen ID as int)

### CustomBox.cs
**Location: DestroyElement()**
- `BOX_DESTROYED` - Triggered when a custom box is destroyed
  - Parameters: paramObj (box GameObject), paramScriptable (ElementData)

### ObjectiveManager.cs
**Location: OnEnable()**
- `OBJECTIVE_COMPLETED` - Triggered when an objective is completed
  - Parameters: paramScriptable (ObjectiveType)

### UnityAdsManager.cs
**Location: ShowAd()**
- `AD_SHOWN` - Triggered when an interstitial ad is shown

**Location: ShowRewardedAd()**
- `REWARDED_AD_SHOWN` - Triggered when a rewarded ad is shown

**Location: OnAdClosed()**
- `AD_CLOSED` - Triggered when an interstitial ad closes

**Location: OnAdFailedShow()**
- `AD_FAILED` - Triggered when an ad fails to show
  - Parameters: paramStr (error message)

**Location: OnRewardedClosed()**
- `REWARDED_AD_COMPLETED` - Triggered when a rewarded ad completes

**Location: OnRewardedFailedShow()**
- `REWARDED_AD_FAILED` - Triggered when a rewarded ad fails
  - Parameters: paramStr (error message)

### SoundManager.cs
**Location: Play()**
- `MUSIC_STARTED` - Triggered when music starts playing
  - Parameters: paramStr (sound ID)
- `SOUND_PLAYED` - Triggered when a sound effect is played
  - Parameters: paramStr (sound ID)

**Location: Stop()**
- `MUSIC_STOPPED` - Triggered when music is stopped
  - Parameters: paramStr (sound ID)

**Location: SetSoundTypeOnOff()**
- `SOUND_SETTINGS_CHANGED` - Triggered when sound/music settings change
  - Parameters: paramStr (sound type), paramBool (is on/off)

**Location: SetVibrationOnOff()**
- `SOUND_SETTINGS_CHANGED` - Triggered when vibration setting changes
  - Parameters: paramStr ("Vibration"), paramBool (is on/off)

## Already Existing Events (Not Modified)

The following events were already in the system and have been preserved:
- `COLLECTIBLE_EARNED`
- `OBJECTIVES_INITIALIZED` - Triggered in LevelScene_Match3Game.cs
- `OBJECTIVE_COMPLETED` - Now properly triggered in ObjectiveManager.cs
- `OBJECTIVE_FAILED`
- `LEVEL_COMPLETED` - Triggered in LevelScene.cs
- `LEVEL_FAILED` - Triggered in LevelScene.cs
- `LEVEL_STARTED` - Triggered in LevelScene.cs
- `CURRENT_WORLD_CHANGED`
- `ELEMENT_SELECTED` - Now triggered in Match3GridInputController.cs
- `ELEMENT_MATCHED` - Now triggered in Match3Grid.cs
- `ELEMENT_DESTROYED` - Already triggered in GridElement_Match3Game.cs

## Usage Examples

### Listening to Events
```csharp
// Listen to element matched event
EventManager.StartListening(GameEvent.ELEMENT_MATCHED, OnElementMatched);

private void OnElementMatched(EventParam param)
{
    int matchCount = param.paramInt;
    Debug.Log($"Elements matched: {matchCount}");
}

// Don't forget to stop listening
EventManager.StopListening(GameEvent.ELEMENT_MATCHED, OnElementMatched);
```

### Triggering Events
```csharp
// Trigger a simple event
EventManager.TriggerEvent(GameEvent.GRID_STABLE);

// Trigger an event with parameters
EventManager.TriggerEvent(GameEvent.CURRENCY_EARNED, new EventParam(
    paramStr: "Coin",
    paramFloat: 100f,
    paramObj: sourceObject
));
```

## Benefits of This Implementation

1. **Analytics Integration**: All these events can be easily tracked for analytics
2. **Sound System**: Sound effects can be triggered based on game events
3. **Tutorial System**: Tutorial steps can listen to specific events
4. **Achievement System**: Achievements can track progress through events
5. **UI Feedback**: UI can respond to game events (animations, counters, etc.)
6. **Debug Tools**: Events make it easy to log and debug game flow
7. **Modular Design**: New features can listen to events without modifying existing code

## Recommendations for Future Development

1. Create an analytics wrapper that automatically logs all events
2. Consider adding more specific events for special elements when implemented
3. Add events for power-ups and boosters as they're developed
4. Consider adding events for tutorial milestones
5. Add player progression events (level unlocked, star earned, etc.)
6. Consider adding time-based events (move time, level duration, etc.)
