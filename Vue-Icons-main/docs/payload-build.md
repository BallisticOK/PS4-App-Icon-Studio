# Payload Ownership

This repo currently does `not` build PS4 icon payloads from source.

Upstream attribution for the base project and payload usage is documented in [`CREDITS.md`](d:\PS4 PKGs\Vue-Icons-main\Vue-Icons-main\CREDITS.md).

What it does today:
- Reads prebuilt `.elf` payload templates from [`PayloadKit`](d:\PS4 PKGs\Vue-Icons-main\Vue-Icons-main\PayloadKit).
- Patches the default title ID marker (`CUSA00960`) to the target app title ID.
- Replaces the embedded PNG block when you send a custom icon.

If you want to stop piggybacking on someone else's payloads, you need the actual payload source project, not just the compiled `.elf` files.

Minimum pieces you need:
- A PS4 payload source tree that implements the app icon replacement logic.
- A working PS4 payload toolchain, usually based on `ps4-payload-sdk` or an equivalent SDK/toolchain.
- A reproducible build script that outputs your own `.elf` templates into [`PayloadKit`](d:\PS4 PKGs\Vue-Icons-main\Vue-Icons-main\PayloadKit).
- Your own legal and technical review of the payload code you adopt or write.

Recommended approach:
1. Start from a payload source project that already changes `icon0.png` for a target title ID.
2. Replace any hardcoded branding, title IDs, and bundled art with your own assets.
3. Add a build script such as `build-payload.ps1` that compiles the payload and copies the finished `.elf` files into `PayloadKit\\payloads`.
4. Keep one known marker title ID in the payload binary so this app can continue patching it per title.
5. If you want to remove binary patching completely, change this app to pass the title ID and image data to your payload at runtime instead.

Important limitation:
- Without payload source code in this repo, I can only rebrand and reorganize the launcher side. I cannot honestly claim this repo builds an original payload yet.
