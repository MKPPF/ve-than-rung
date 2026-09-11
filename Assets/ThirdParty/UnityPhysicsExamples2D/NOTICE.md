# Unity PhysicsExamples2D attribution

This Unity project contains an adapted portion of Unity Technologies'
`KinematicTopDownController` cast-and-slide movement algorithm.

- Upstream project: `Unity-Technologies/PhysicsExamples2D`
- Upstream file: `OldPhysics2D/Assets/Scripts/SceneSpecific/KinematicTopDownController.cs`
- Pinned revision: `418ede118ab0b512a7177f8cf8971e9e87cc76a0`
- Source: https://github.com/Unity-Technologies/PhysicsExamples2D/blob/418ede118ab0b512a7177f8cf8971e9e87cc76a0/OldPhysics2D/Assets/Scripts/SceneSpecific/KinematicTopDownController.cs
- Local adaptation: `Assets/Scripts/Player/PlayerMovement.cs`

Local changes retain this game's Input System actions, gamepad/WASD input,
click destinations, combat movement locks, animation velocity sampling and
footstep cadence. Player knockback and Ice Dash are also routed through the
same solver so external movement cannot bypass solid walls. The collision
solver uses Unity 6's positioned
`Rigidbody2D.Cast` overload so it does not temporarily reposition the live body
during query iterations. Trigger colliders are deliberately excluded from the
movement filter.

The upstream copyright and license notice is preserved in `LICENSE.md` in this
directory. The upstream license file is available at:
https://github.com/Unity-Technologies/PhysicsExamples2D/blob/418ede118ab0b512a7177f8cf8971e9e87cc76a0/OldPhysics2D/LICENSE.md
