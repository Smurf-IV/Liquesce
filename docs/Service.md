# A Service
Seems like a reasonable request for something that will be operating just above the driver layer of the OS it is running on. 
**So what does the service need in order to perform something useful ? (Apart from the Pre-Reqs and installation :-))**
* A human readable configuration file:
	* to ensure that the variables that are being set are correct.
	* Allow others to write a better management GUI
	* Allow the XML Serialisation to do it's thing to make the config class objects easy to use.
* Must be changeable by the service:
	* Defeats the object of getting this to do anything if nothing can change it.
	* Not in the user locations to prevent unwanted access
* A drive letter
	* Where is this going to be found in the explorer to add shares if it is not visible
	* A "Drive Label" as well
* Actual locations
	* Physical areas to store stuff
	* Something to allow those areas to have min space before forcing writing into a different location
* Dokan specific stuff
	* Number of threads
	* Debug mode
* Specific stuff
	* Delay start (Allow network / USB / etc. drivers to connect to their sources)
	* _ACL Reconnect_
	* Share enabling
	* _Libraries (Win 7 ?)_

## Physical Areas
**So what does that mean?**
These are going to be locations the Host OS can see before the Liquesce Service kicks in. They can be mount points (To allow loads of drives to be used - Like WHS), Drive Roots, Share locations, Directories, _other things ??_
As these location fill up then there needs to be something to stop a file create happening, and then that file overflowing, so a min space will need to be implemented to force creation in another space.
The physical areas will (May start of) with duplicate directory structures, but the new "drive" needs to appear as a smooth directory over these duplicates.
There will need to be some way of flagging up duplicate file names that could appear, and for the service to display only one of them.

