# Stride.NanoUI
An attempt at getting NanoUI to render in the Stride game engine.

# Unfinished things
- Need the new package from NanoUI to be released to fix an issue with asset loading https://github.com/kbergius/NanoUI/issues/78
- Some of the interface methods didnt have a direct replacement in Stride. specifically the Texture update methods.
- Rendering is blank. I am missing something with Stridesd renderer that required to actually show the UI properly.
- Some inputs are missing in Stride key enum. May be a non issue but worth mentioning.

References that helped:
- [Empty Keys Stride implementation](https://github.com/EmptyKeys/UI_Engines/blob/master/EmptyKeys.UserInterface.Stride/Renderers/StrideRenderer.cs) I was debating using Spritebatch instead of direct communication with the command list but I didnt get around to it.
- [Stride ImGUI implementation](https://github.com/stride3d/stride-community-toolkit/blob/main/src/Stride.CommunityToolkit.ImGui/ImGuiSystem.cs) This is what the current rendering was based off of but it was very low which makes me think the above may be better.

