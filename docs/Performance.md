# When running these tests:
* **Make sure that explorer is not open.**
* **Logging set to Warn.**
* **Dokan Threads set to 0 (For automatic).**
## Tmp  File Creation
* 2012-05-05 (1024 files via [WinAFRED](http://winafred.codeplex.com/SourceControl/changeset/changes/76079))
	* Native drive in Windows 7 x64 = 9.26 seconds
	* Mounted drive on same (5 sources (all drives with low space)) = 11.59 seconds

## Tmp File Deletion
* 2012-05-05 (1024 files via [WinAFRED](http://winafred.codeplex.com/SourceControl/changeset/changes/76079))
	* Native drive in Windows 7 x64 = 6.57 seconds
	* Mounted drive on same (5 sources (all drives with low space)) = 6.88 seconds
