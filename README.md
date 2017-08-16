# Liquesce
Liquesce: dictionary description

Definition: melt from solid to liquid; mix in
Synonyms: fuse, liquefy, blend, pool.
What is Liquesce?

The driving force behind the Liquesce code base is to take you hard drives that have Space / Directories and merge them into a single representation in an explorer view. This is very similar to the “Drive Extender” functionality in WHS, but without the need to format your drives :-)

    The storage pool can use any type and size of hard drive, including SATA, IDE, ESATA, FIREWIRE, USB etc.

    If the drives have the same directory representation i.e. Movies on the root of both, then these will be merged into a single Movies directory within the view.

    Writing to this merged pool will have different strategies dependent on the setup, e.g. Folder / Balanced / Priority.

image

So read on and find out what Liquesce could possibly do for your separate hard drives (logically and physically), and see if it can be of use to you.

For more information: go to The FAQS section
OS Requirements:

    You will need the .Net 4.5.1 or greater, so please install that.
    And then run the MS updates to ensure that the security fixes are applied. 

Notes:
-  For Phase I code only you will need Dokan for the OS you are using.
-  For Phase II code only you will need .Net4 Full profile, for the OS you are using.
Directory / Drive Merging

After an install, the Management Application will be launched showing the default setup.

This will be a direct mirror of Drive C with a default delay of over 32 seconds.

The management window is split into three areas

    The File System
    Merge Points
    Expected Output 

The File System window shows what can be “Seen” as a potential source for the merge points. These can be opened and will show directories only. once you have selected a source, it can be dragged and dropped into the Merge Points area.

The Merge Points, can be reordered and deleted in case mistakes a re made. The order in this window is used to determines the preference order for filling the storage locations.

The Expected Output, allows you to traverse over the merged directories to see if the structure is correct. This will also show duplicate filenames if they exist, so please expand all the directories to see if any collisions have occurred. The system will not scan, as this is resource and time expensive.

 

 

 

Below is an example of 3 small HDD’s (50MB), that have been used to set up an initial merge size of 150 MB.

    The “Hold Of Bytes” was set to 10MB, meaning that when a new file was to be placed onto the storage, and there is less than that number of bytes, it would move to the next in order.
    The configuration was then sent to the Liquesce Service via the menu operation “Send Configuration”
    The then Music Directory was created, and the Artist was copied into it (On the “N” drive).
    The Liquesce Service then spread the directory structure across the drives (See right hand explorer window)
    The management application was then refreshed and now you can see that it shows all the files from the album appearing as a single listing (Bottom window)
    Right clicking on the properties of the Music directory within the “N” drive shows that this is read as a single system. 

Merge-Results_thumb3[3]
 
The Application Tray

So why one of these ?

    Need to inform the logged on user that something might be going on.
    Need to have some way of alerting the normal user that things may not be good in the new drive
        Running out of space,
        unable to delete,
        Stats. 
    Just need to show that it's running (Like AV's do :-)
    Application Management
    Current Space usage 

Current Space Usage

The tray app when running on the host has a menu to show the distribution of the space over the mount points that make up the space available.

Here's a view from XP
Liquesce FreeDiskSpace.png
- The light blue shows the amount of space currently allocated in the _backup directories
- The Red border is showing that the drive has fallen below the "Hold Of Bytes" number
- The Orange border is showing that the free space is approaching the "Hold Of Bytes"
- The Green indicator bar (Below) shows where the next new file is likely to be created
- All of this is updated on a timer
