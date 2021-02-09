# HandPosing
This package uses Oculus Quest hand-tracking to control Oculus default rigged hands and use it to generate grab poses in an instant.

There is a full Unity Project to serve as a game-demo [here](https://github.com/MephestoKhaan/HandPosing_demo)

![](https://user-images.githubusercontent.com/4976810/86815186-a97c5280-c082-11ea-9df2-8c45a28f06e7.gif)

## Requisites 
- You need to download the **Oculus Integration** from the Asset Store, as this requires OVRSkeleton. I have tested it with Unity 2019.3 and Oculus Integration 17.0 and 18.0.
- You need to use **Quest + Oculus Link** in the editor to generate poses (but the generated poses will work in a build with either hands or controllers).

## Install
Import this project as a .git package! Go to the *Unity Package Manager*, and in the ✚ icon select `Add Package from Git Url...` then in the box copy the address of this repo: `https://github.com/MephestoKhaan/HandPosing.git`
Don't forget to import the Samples like the **Oculus Integration (OVR)** to start working with your Quest, or the **Poses Gallery** for the samples on how to use it.

You could also download the latest stable package in the [releases section](https://github.com/MephestoKhaan/HandPosing/releases/). But the method above is the preferred one.


Please refer to the [Wiki](https://github.com/MephestoKhaan/HandPosing/wiki) for guidance.
You can also find the full code documentation [here](https://mephestokhaan.github.io/HandPosing/Documentation/html/annotated.html) 

This is a work in progress, and things are subject to change. I hope it serves others either as a useful tool or at least as a starting point for their grabbing-interaction implementations. At this moment I won't be accepting **Pull Requests** but reach me in the issues if something is not right or have any feature in mind that you think is missing.

And please leave a **star** if this helped you, it truly helps keep the motivation going!

![](https://user-images.githubusercontent.com/4976810/86817608-856e4080-c085-11ea-8210-d280904f5d00.gif)
