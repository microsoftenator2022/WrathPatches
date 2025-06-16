# Wrath Patches

A collection of small patches for Pathfinder: Wrath of the Righteous. Mostly aimed at cleaning up the
game's log files, but fixed some minor bugs along the way.

Any patch can be disabled by removing the corresponding dll from the mod directory.

Thanks to Kurufinve on the Owlcat discord for the assistance and inspiration. Also Kurufinve and Hambeard for their contributions.

## Current fixes

This list is infrequently updated and likely out of date

- Add AbilityData spell descriptor to AbilityExecutionContext
- Fix Shared Strings
- AddFacts.UpdateFacts NRE fix
- Silence missing animator state errors
- Add per-OwlMod log sinks
- Use OwlMod logger in place of PFLog.Mods
- Catch and log Blueprint patch exceptions
- No BloodyFaceController for non-UnitEntityView
- Fix NRE in SelectionCharacterController.CurrentSelectedCharacter
- Element AssetGuid null fix
- Better EntityFactComponent error messages
- Make BlueprintBeingRead ThreadStatic
- GameStatistic.Tick null Player fix
- Event subscription leak fixes
- Silence 'no binding' warnings
- Silence 'Await event system' messages
- Only load .dll files from owlmod Assemblies directories
- Less verbose short log exceptions
- OpenGameLogFull opens Player.log too
- Don't try to play missing settings UI sound types
- OwlcatModificationManager: UMM mods as dependencies for OMM mods
- OwlcatModificationManager: Compare OMM mod versions like UMM
- Reduce button click delay from 300ms to 1 frame
- Add components from mod assemblies to binder cache
- Load OwlMod BlueprintDirectReferences dependencies
- Non-matching BlueprintComponent.OwnerBlueprint warning
- Make ProgressionRoot.HumanRace not break for new races
- Hambeard: Fix save slot VM double bind
- Remove unnecessary code in SetMagusFeatureActive.OnTurnOff
- Kurufinve: ShaderWrapper fix for owlmod material shaders
- Fix Spell Slot comparison
- Kurufinve: Owlmod Enhancer
- Fix UnitMarkManager.LateUpdate iterator mutation
