# Release process

Every Ragnavik Compatibility release requires a corresponding Ragnavik website blog post. The package must not be published until the post is ready to publish with it.

1. Update the plugin attribute, both project versions, package manifest, and changelog to the same version.
2. Build with the documented Valheim and BepInEx versions.
3. Run `scripts/package.sh` and `scripts/validate-package.sh`.
4. Test the packaged DLL in a controlled client and dedicated server profiles with the affected mod versions.
5. Confirm `lostkode.ragnavik.compat` is present on both clients and servers and remains version checked as a shared plugin.
6. Rebuild anti-cheat policy from the complete effective client and server manifests. Version check shared mods, put all client-only mods in the extra whitelist, and put all server-only mods in the server-only list. Confirm the retired `LostKode.RagnavikEpicMMOReloadGuard` GUID is absent after migration and do not add `lostkode.ragnavik.compat` to either exception list.
7. Prepare and review the corresponding Ragnavik website blog post. Include the version, player-visible changes, compatibility notes, and installation or upgrade guidance.
8. Publish the package and website blog post together.
9. For a related server deployment, verify live loaded plugin counts and source/runtime plugin parity before calling the deployment complete.
10. Tag the source commit as `vVERSION` only after the released archive has passed validation.

Release work in this repository does not authorize a live server restart or a Thunderstore publication. Those actions require their own explicit approval.
