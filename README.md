Eye Tracking Animation
====

## Prerequisites
- [Modular Avatar](https://modular-avatar.nadena.dev)
- [Animator As Code V1](https://docs.hai-vr.dev/docs/products/animator-as-code)
- [Parameter Smoother](https://github.com/logicmachine/avatar-parameter-smoother)


## How to use

### Install
- Install bd_'s and Haï~'s VPM repositories for prerequisites.
- Install [Logilabo Avatar Tools VPM repository](https://vpm.logilabo.dev) and import "Eye Tracking Animation".
- Put `Package/Eye Tracking Animation/EyeTrackingAnimation.prefab` into your avatar.

### Configure
- Set properties of the `Eye Tracking Controller` component.

TODO: Write about configuration

### Note: animator setup
This animator expresses eye closeness by playing only closing animations weighted by transition.
Therefore, you must play an animation that indicates eyes are opened (e.g., blink\_left <- 0 and blink\_right <- 0) in a layer before all layers created by this generator.


## How it works
TODO: Write this section

These articles (in Japanese) may help you to understand how this animator works:

- [VRChat向けアイトラッキング実装テクニック](https://note.com/logilabo/n/n83d1c2a06d75)
- [アイトラ時代の眼球表現](https://www.docswell.com/s/logilabo/ZRXDPR-AdvancedEyeTracking)


## Animator parameters
You have to send following parameters via OSC or Parameter Driver to drive this animator.

### Enable
- Name: `EyeTrackingController/v2/Enable`
- Type: `bool`

### LInOut, RInOut
- Name: `EyeTrackingController/v2/LInOut`, `EyeTrackingController/v2/RInOut`
- Type: `float`
- Range: \[-1.0, 1.0\]

### Pitch
- Name: `EyeTrackingController/v2/Pitch`
- Type: `float`
- Range: \[-1.0, 1.0\]

### LCloseness, RCloseness
- Name: `EyeTrackingController/v2/LCloseness`, `EyeTrackingController/v2/RCloseness`
- Type: `float`
- Range: \[0.0, 1.0\]

### LCloseRemote, RCloseRemote
- Name: `EyeTrackingController/v2/LCloseRemote`, `EyeTrackingController/v2/RCloseRemote`
- Type: `bool`

### Blink0, Blink1
- Name: `EyeTrackingController/v2/Blink0`, `EyeTrackingController/v2/Blink1`
- Type: `bool`

It is a counter in modulo 4 for blinks.
`Blink0` is 0th bit (LSB) and `Blink1` is 1st bit (MSB).

### LBlinkable, RBlinkable
- Name: `EyeTrackingController/v2/LBlinkable`, `EyeTrackingController/v2/RBlinkable`
- Type: `bool`

You can set them as `true` if you want to disable eye lid control for each eyes temporarily.
They will be helpful to avoid conflicting eye lid shapes with other animations.


## [EyeTrackingBridge](https://github.com/logicmachine/EyeTrackingBridge)
It is an example application to send OSC messages for this animator.
Currently, it supports only Pimax Crystal with [BrokenEye](https://github.com/ghostiam/BrokenEye).

