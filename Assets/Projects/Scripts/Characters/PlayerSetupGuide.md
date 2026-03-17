# プレイヤーセットアップガイド

1. プレイヤーの GameObject を作成します。
2. `Rigidbody2D` を追加し、Z 軸の回転をフリーズします。
3. キャラクター本体に合わせた `Collider2D` を追加します。
4. `PlayerInputReader2D` を追加し、`Assets/InputSystem_Actions.inputactions` を割り当てます。
5. `PlayerMotor2D` を追加します。
6. `Ground Layers` をステージが使用しているレイヤーに設定します。
7. オプション：足の位置に子オブジェクトを作成し、`Ground Check Point` に割り当てます。
8. オプション：`Animator` と `SpriteRenderer` を持つビジュアルルートに `PlayerAnimationDriver2D` を追加します。
9. シーンのどこかに `WorldPolarityService` を追加し、`PlayerInputReader2D` を割り当てます。
10. シーンオブジェクトに `PolarityBackground2D` を追加し、背景が白/黒に反転する場合はメインカメラを指します。
11. ワールドと一緒に反転すべきスプライトに `PolarityObject2D` を追加します。
12. シーンに `ProjectileSpawner2D` を追加し、`Projectile2D` プリファブを割り当てます。
13. プレイヤーに `WeaponController2D` と `Shooter2D` を追加します。
14. `WeaponDefinition2D` と `ProjectileDefinition2D` アセットを作成し、`WeaponController2D` に割り当てます。
15. `Shooter2D` に子トランスフォームを `Muzzle` として設定します。

推奨される Animator パラメータ：

- `Grounded` (`bool`)
- `MoveSpeed` (`float`)
- `VerticalSpeed` (`float`)
- `Crouching` (`bool`)
- `Running` (`bool`)
- `InputX` (`float`)

オプション：ポーラリティ入力

- `Player` マップに `TogglePolarity` アクションを追加し、`Tab` にバインドします。
- まだアクションが追加されていない場合、`PlayerInputReader2D` は `Tab` に直接フォールバックします。

シューティングセットアップ：

- `WeaponDefinition2D` は発射モード、ペレット数、拡散、発射レートを制御します。
- `ProjectileDefinition2D` は速度、ライフタイム、距離、ダメージ、ヒットマスクを制御します。
- `Projectile2D` はヒット検出にフレーム間レイキャストを使用するため、高速弾が簡単にトンネリングしません。
