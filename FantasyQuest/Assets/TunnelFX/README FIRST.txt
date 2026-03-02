************************************
*             TUNNEL FX            *
* (C) Copyright 2016-2022 Kronnect * 
*            README FILE           *
************************************


How to use this asset
---------------------

Thanks for purchasing Tunnel FX.

Run the demo scene to get a first contact with the asset. It will allow you to change from one preset to another.

To use the asset in your project, select your camera and add "TunnelFX" script to it.
Use the custom inspector to select a preset configuration or customize the effect. Some properties in the inspector shows a tooltip with some additional details.

Remember to remove the Demo folder once you don't need it anymore.


Hints
-----

- Texture alpha channel can be used to create smooth blends
- Performance is scalable - using one layer will be faster than using 4 layers
- Do not enable "Depth Aware" to avoid camera depth texture fetches if you don't need it
- Choosing a preset will revert all values to the factory settings. When you change a value, the preset changes to "Custom". This allows you to preserve your changes in your scene.


C# Scripting support
--------------------

You can access the script API from C#:

using TunnelEffect;

TunnelFX fx = TunnelFX.instance;
fx.preset = TUNNEL_PRESET.xxx;

You can also call PlayTransition mode to issue a quick/flashy screen transition to current values. Useful when setting your own configuration or after loading/enabled the component with your custom settings:
fx.PlayTransition(2.0);


Support
-------

* Email support: contact@kronnect.com
* Website-Forum Support: https://kronnect.com
* Twitter: @Kronnect


Other Cool Assets!
------------------

Check our other assets on the Asset Store publisher page:
https://assetstore.unity.com/publishers/15018



Future updates
--------------

All our assets follow an incremental development process by which a few beta releases are published on our support forum (kronnect.com).
We encourage you to signup and engage our forum. The forum is the primary support and feature discussions medium.

Of course, all updates of Tunnel FX will be eventually available on the Asset Store.



Credits
-------

- Tunnel FX asset for Unity: (c) 2016-2022 Kronnect - All Rights Reserved.
- Fire texture CC by Filter Forge: https://www.flickr.com/photos/filterforge/13908586495
- Cloud texture and others CC by webtreats: https://www.flickr.com/photos/webtreatsetc/5584892057


Version history
---------------

Version 1.4.2
- Removed expensive matrix multiplication in shader if camera rotates is set to false

Version 1.4.1
- Smooth speed change allowed
- Shader optimizations

Version 1.4
- Added Global Speed parameter to layers section
- Added Hyper Speed effect parameter to general settings section

Version 1.3
- VR: Single Pass Stereo Rendering support

Version 1.2
- Added global alpha property

Version 1.1
- General shader optimizations
- New downsampling parameter
- Effect can now be seen in Scene View in Unity 5.4

Version 1.0 2016.08.11 - First release







