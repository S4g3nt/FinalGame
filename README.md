前面忘了，后面忘了，总之串串🐂B！！

逆天超神王Q

## scc

### yoru
**发现bug（已解决）**

1.假人会卡在悬崖壁上 可以去除侧面摩擦力解决

2.pre fake anchor互相碰撞

3.掉落悬崖时候 更自由（none）

**修改**

1.所有的陷阱的tag改为deathzone

2.PlayerControl.cs是每个hero的基础能力 --skills是特殊能力

*注意*  若有新加入的特殊机制（弹簧一类的） 若要假人也能实现 需要单独加

### 弹簧SpringPad
bounceVelocityY（弹跳竖直速度）
触发弹簧时，给目标 Rigidbody2D 设置的 linearVelocity.y。
数值越大弹得越高；可以和玩家 jumpForce（例如 12）对比着调。

maxUpwardSpeedToAccept（允许触发的最大向上速度）
只有当前 velocity.y 不大于这个值 才会弹。
用来避免：已经在快速上升时又碰到弹簧，被反复叠速度或一帧内多次判定。
例如设为 0.5，表示“明显在往上飞”时先不弹。

cooldownPerTarget（每个目标的冷却时间，秒）
同一个刚体（同一实例）触发一次弹簧后，至少隔这么多秒才能再被这根弹簧弹一次。
减轻落地抖动、连续碰撞造成的连弹。