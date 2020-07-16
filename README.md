# HandPosing
This project uses Oculus Quest hand-tracking to control Oculus default rigged hands and use it to generate grab poses.

This is a work in progress, and things are subject to change. I hope it serves others either as a useful tool or at least as a starting point for their grabbing-interaction implementations. At this moment I won't be accepting **Pull Requests** but reach me in the issues if something is not right or have any feature in mind that you think is missing.

![](https://user-images.githubusercontent.com/4976810/86815186-a97c5280-c082-11ea-9df2-8c45a28f06e7.gif)
![](https://user-images.githubusercontent.com/4976810/86817608-856e4080-c085-11ea-8210-d280904f5d00.gif)

You need to download the **Oculus Utilities** from the Asset Store, as this requires OVRSkeleton.

Open the **PoseTest** scene, the most important bit are the bindings in **RightHandAnchor** and **LeftHandAnchor** that demonstrates what joints from the tracked hand correspond to the ones in the model (pay special attention to the thumb and the pinky) and their offsets. As well as a set of grabbing experiments that shows the different grab behaviours.


Please refer to the ![Wiki](https://github.com/MephestoKhaan/HandPosing/wiki) for the documentation.
