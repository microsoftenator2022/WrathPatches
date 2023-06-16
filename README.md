# Wrath Patches

A collection of small patches for Pathfinder: Wrath of the Righteous. Mostly aimed at cleaning up the
game's log files, but fixed some minor bugs along the way.

Any patch can be disabled by removing the corresponding dll from the mod directory.

Thanks to @Kurufinve on the Owlcat discord for the assistance and inspiration.

## Current fixes

### "Await event system" fix

Removes the "Await event system" log entries (which mostly only happens when using Unity Explorer).

### Element.AssetGuid/Element.AssetGuidShort fix

Adds a missing null check in `Element.AssetGuid` and `Element.AssetGuidShort`

### Better EntityFactComponent exception messages

Improves the log messages for exceptions in `EntityFactComponent.Activate` and
`EntityFactComponentDelegate.OnActivate`

### GameStatistic.Tick null Player fix

Adds a missing null check in `GameStatistic.Tick`

### Better short log exception entries

Removes a mostly-redundant verbose stacktrace from Exception entries in the "short" game log (`GameLog.txt`)

### "Bind: no binding named x" fix

Removes logging for unbound key bindings in `KeyboardAccess.Bind` and `KeyboardAccess.DoUnbind`
("Bind: no binding named x")

### Save menu "AddDisposable" fix

Fixes redundant view bind in the Save/Load menu. Fixes "Trying to assign a new owner to 
BaseDisposable that already have one" log entries for every save slot

### SharedString fix

Correctly instantiates `SharedStringAsset`s with `UnityEngine.ScriptableObject.CreateInstance` 
instead of `new SharedStringAsset`. This bug presented as large numbers of  
"Kingmaker.Localization.SharedStringAsset must be instantiated using the ScriptableObject.CreateInstance method instead of new SharedStringAsset."
log messages being generated. Most noticeably with the Homebrew Archetypes mod

### UnitMarkManager fix

Fixes a "modify collection while enumerating" bug in `UnitMarkManager.LateUpdate`
