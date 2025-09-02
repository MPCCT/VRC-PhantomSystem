# VRC-PhantomSystem
一个简单的VRC小工具用以在VRC模型中添加一个跟随本体动作的分身模型。

## 准备工作
在使用该工具前，请确保以下内容正确安装：
1. Unity 2022.3.22f1
2. VRChat SDK (仅作为参考，在该文档编辑时使用的版本为3.8.2)
3. [Modular Avatar](https://github.com/bdunderscore/modular-avatar) (仅作为参考，在该文档编辑时使用的版本为1.13.4)

## 使用方法
1. 准备好一个基础模型和准备作为分身的分身模型（最好与基础模型身体相同或相似）
2. 在顶部菜单中选择 `MPCCT -> PhantomSystemSetup`
3. 选择 **基础模型** 和 **分身模型**
4. 点击 **开始配置！**，没有问题的话就没问题了

需要注意该工具为[Modular Avatar](https://github.com/bdunderscore/modular-avatar)做了一定适配，因此分身模型在配置前可以自由添加有[Modular Avatar](https://github.com/bdunderscore/modular-avatar)适配的组件。
> **卸载系统**：只需删除模型下的 `PhantomSystem` 对象即可

---

## 注意事项
- 若不想分身模型和原本模型参数同步，可以勾选 **重命名分身模型的参数** 选项。
- 注意配置好后的模型整体参数数量、动骨（`PhysBones`）数量限制等限制以防无法上传模型。
- 不建议在分身模型中添加任何骨骼动画相关的组件，以及任何在动画控制器中使用了 `VRC Tracking Control`的组件。

---

## 可能存在的问题

- **配置好之后分身模型/基础模型一直摆T-pose**：重新装一下试试（偶尔出现，不知道为什么，可能和骨骼绑定有关）。
- **幻影模型的样子扭曲得非常逆天**：分身模型的骨骼可能和基础模型不兼容，可以尝试 **使用Rotation Constraint** 选项。

---
