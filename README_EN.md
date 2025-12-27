# VRC-PhantomSystem
VRC-PhantomSystem is a tool designed to easily add a "Phantom Avatar" to your VRChat avatar that follows the movements of the base model.

## ✨ Features
- **One-Click Setup**: No complex setting steps. Once your models are ready, configure everything with a single click.
- **Multiple Phantom Controls**: Control whether the phantom stays in place or moves with you after spawning. You can also pick up and move the phantom manually. The "Freeze" option allows the phantom to lock into its current pose at any time.
- **Phantom Expression Menu**: An independent expression menu for the phantom, allowing it to change clothes or toggle minor features.
- **[Modular Avatar](https://github.com/bdunderscore/modular-avatar) Compatible**: You can freely add items using MA components to the phantom model.
- **Phantom View**: Open a screen that displays the phantom's perspective, allowing you to monitor its surroundings.

## 📦 Dependencies
Before importing this tool, please ensure the following are installed correctly:
1. Unity 2022.3.22f1
2. VRChat SDK (Version 3.10.1 used during documentation)
3. [Modular Avatar](https://github.com/bdunderscore/modular-avatar) (Version 1.15.1 used during documentation)

## 🚀 Quick Start
1. Prepare a **Base Avatar** and a model to be used as the **Phantom Avatar** (it is recommended that their body structures are the same or similar).
2. Select `MPCCT -> PhantomSystemSetup` from the top menu.
3. Drag and drop your models into the **Base Avatar** and **Phantom Avatar** fields.
4. Click **Setup!**

## ⚠️ Notes
- If you do not want the phantom and the base model to sync parameters on certain MA components, check the **Rename phantom avatar parameters** option.
- Pay attention to the total parameter count and PhysBone limits after configuration to ensure the avatar can be uploaded.
- It is NOT recommended to add any bone animation components or any components that use `VRC Tracking Control` in the animator controller to the phantom model.
    - For example, plugins like [GoGoLoco](https://booth.pm/items/3290806) and [AvatarPoseLibrary](https://github.com/HhotateA/AvatarPoseLibrary) that control avatar movement are not supported on the phantom model.
- Enabling the phantom model essentially doubles the performance load. Even when the phantom is hidden, the FX layers will still be more than a regular avatar. Please use it mindfully.

## ❓ FAQ
- **How do I delete a configured PhantomSystem?**
  - Simply delete the `PhantomSystem` object within your avatar hierarchy.
- **The phantom's body is distorted after activation:**
  - The phantom's bone hierarchy might be incompatible with the base model. Try checking **Use Rotation Constraint** in the **Advanced Settings**.
- **Some PhysBones on the phantom still move with the base model even when frozen:**
  - This is likely caused by PhysBones using "World" as the Immobile Type. Try checking **Change PhysBone ImmobileType (may break some physbones)** in the **Advanced Settings**.
- **Friends see the phantom in different positions (Sync issues):**
  - This is a synchronization issue. Try toggling the phantom off and on again to resync the position.
- **Can I spawn multiple phantoms at once?**
  - Yes. You can achieve this by nesting the PhantomSystem setup and ensuring that the parameters for each phantom system on each layer are renamed.
- **Can I install DPS/SPS on the phantom avatar?**
  - Well... yes, you can.

## 📜 License
This tool is licensed under the `MIT License`. See the `LICENSE` file for details.