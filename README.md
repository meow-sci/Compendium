# Compendium
KSA Game mod - Creates a window with information and minor utilities for loaded celestials

The Stars category offers target selection for orbiting stars, using the currently
controlled vehicle. Select the target again, or click the Current Target indicator,
to clear it. Target selection is disabled when no vehicle is controlled.

Fixed stars and root barycenters can also be selected as **navigation-only targets**.
Harmony patches provide their navball target marker, native TGT reference frame,
rotation hold, and Toward/Away attitude
tracking without inventing an orbit or changing the game's celestial definitions.
After selecting a root target, use the native TGT panel's Toward/Away or rotation
hold buttons (requires a controllable vehicle). **Track Toward** and **Track Away**
in Compendium remain available too. The TGT navball mode and relative distance/speed
readouts also work. Root closest distance and time are estimated over the next
10 game years of available coast flight-plan patches, excluding planned burns.
The estimate samples the trajectory and refines sampled local minima; short
encounters can be missed. Distances are center-to-center, not surface altitude.
The minimum can be at the current time or the end of the search window, so it is
not necessarily a future flyby. The search refreshes at most once per real second
on an unchanged plan (sooner when simulation time advances by 60 seconds or the
cached approach passes); no available patches leaves the readouts disabled.
The root's fixed orientation defines its TGT frame, so rotation hold is inertial
relative to that root, not continuous pointing toward it (use Toward for that).
These targets are per vehicle, session-only, and are not saved with the game.
Selecting a native target replaces the navigation-only target; clearing it or
unloading Compendium ends root-target tracking. Rendezvous planning, docking/Align,
and the native Target Track window still require a native orbiting target.
