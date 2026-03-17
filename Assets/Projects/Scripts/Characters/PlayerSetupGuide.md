# Player Setup Guide

1. Create a player GameObject.
2. Add `Rigidbody2D` and freeze Z rotation.
3. Add a `Collider2D` that matches the character body.
4. Add `PlayerInputReader2D` and assign `Assets/InputSystem_Actions.inputactions`.
5. Add `PlayerMotor2D`.
6. Set `Ground Layers` to the layers your stage uses.
7. Optional: create a child object at the feet and assign it to `Ground Check Point`.
8. Optional: add `PlayerAnimationDriver2D` to the visual root that holds `Animator` and `SpriteRenderer`.

Recommended Animator parameters:

- `Grounded` (`bool`)
- `MoveSpeed` (`float`)
- `VerticalSpeed` (`float`)
- `Crouching` (`bool`)
- `Running` (`bool`)
- `InputX` (`float`)
