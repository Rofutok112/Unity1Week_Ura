# Player Setup Guide

1. Create a player GameObject.
2. Add `Rigidbody2D` and freeze Z rotation.
3. Add a `Collider2D` that matches the character body.
4. Add `PlayerInputReader2D` and assign `Assets/InputSystem_Actions.inputactions`.
5. Add `PlayerMotor2D`.
6. Set `Ground Layers` to the layers your stage uses.
7. Optional: create a child object at the feet and assign it to `Ground Check Point`.
8. Optional: add `PlayerAnimationDriver2D` to the visual root that holds `Animator` and `SpriteRenderer`.
9. Add `WorldPolarityService` somewhere in the scene and assign `PlayerInputReader2D`.
10. Add `PolarityBackground2D` to a scene object and point it at the main camera if you want the background to flip white/black.
11. Add `PolarityObject2D` to any sprite that should invert with the world.

Recommended Animator parameters:

- `Grounded` (`bool`)
- `MoveSpeed` (`float`)
- `VerticalSpeed` (`float`)
- `Crouching` (`bool`)
- `Running` (`bool`)
- `InputX` (`float`)

Optional polarity input:

- Add a `TogglePolarity` action to the `Player` map and bind it to `Tab`.
- If no action is added yet, `PlayerInputReader2D` falls back to `Tab` directly.
