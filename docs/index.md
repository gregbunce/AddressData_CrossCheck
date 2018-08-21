# NG911 Data Prep and AGRC’s Address Cross Check Project

### Project Background
As the State of Utah moves closer to Next-Generation-911 (NG911) implementation, AGRC sees data prep (clean up) as the next, most logical step for GIS preparedness.  In a true NG911 environment, GIS is the backbone to address validation, call routing, and various dispatch functions.  Eventually, the GIS data will replace the MSAG and therefore the 911-based GIS data will be critical.  As a result, this GIS data will be required to adhere to national standards.   As the State’s GIS office, AGRC has the responsibility of validating, aggregating, and providing the NG911 data and database.

### AGRC and NG911 Data Validation
At the moment, AGRC is focused on facilitating two specific data validation checks for NG911 GIS data: 
Per NG911 specification, address points must be synchronized against the road centerline data.  In other words, address point must have a corresponding road segment.  Here are some common mismatch issues we have found:
Address components don’t match.  These issues include mismatch in the following address components: pre direction, post type, and post direction
The editor must determine which dataset (address points or roads) has the correct address component and then correct the issue
Address point can not find corresponding road centerline
Often because the road has the alias and primary names switched
Road centerline address range does not accommodate the address point 
Typically, the road centerline address ranges need to be adjusted  
AGRC will geocode the ALI database against the GIS data - to ensure that all valid addresses in the phone carrier’s database can be located/validated in the GIS data.  A similar process will need to happen with the MSAG and the GIS data.  All valid addresses (in the ALI database) that were not found in the GIS will need to be added.  Per NG911 specification, we must have a corresponding GIS address point for all valid ALI records. 
 	
### The Address Cross Check Project
AGRC’s Address Cross Check project is centered on the first data validation topic mentioned above.  The project is guided by NG911 and NENA standards.  To facilitate this, AGRC has created a custom process to perform validation checks on the address points and roads datasets.  

The process cross-checks the address components from address points and road centerlines - checking for attribute, distance, and address range issues (mismatches). The project outputs a file geodatabase containing the address points and road centerlines containing possible issues, as well the input, source data for map reference.  An overview of the code operations are as follows:

* Iterate through each address point and spatially searches for the two closest, matching road segments with the same name, that are within a user-specified distance.
* The code then checks the road segments for other matching attributes including: pre direction, post type, post direction, and if the house number is contained within the segment’s address range.  
* The code will also flag address points that do not find a corresponding road segment.
* Additionally, the process allows the user to define a distance in which the address point will be flagged if it falls outside of this distance range.
The project can be run at any time, and AGRC will provide each jurisdiction (county/city) with the following outputs: a file geodatabase containing the output data and an ArcMap document, symbolized, so that the end-user can easily explore the data.   

##### Notes on the File Geodatabase and ArcMap document:

![Geodatabase](/images/fgdb.png)

* AddrPnts_SGID and Roads_SGID represent a snapshot of the input data, based on when the project was run.
* AddrPoints_ToCheck and Roads_ToCheck contain the records that flagged based on one of the validation checks.  
* The user can view each validation check individually by way of definition queries on the following, boolean fields: 
![Geodatabase](/images/fields.png)
	* PREDIR_ISS (indicating pre direction mismatch)
	* POSTTYPE_ISS (indicating post type mismatch)
	* POSTDIR_ISS (indicating post direction mismatch)
	* ADDR_RANGE_ISS (indicating address range mismatch)
	* NOT_FOUND (indicating the address point did not find a matching road segment)
	* DIST_ISS (indicating the nearest road segment was beyond the user-defined distance radius)
* The corresponding feature can be found by using the following fields: ADDR_UID and ROAD_UID.  In other words, if you’re looking at the AddrPoints_ToCheck data, you can use the ROAD_UID field to determine what road segment the validation was run on (and visa vera is you’re looking at Roads_ToCheck data).
	* If you’re using the provided ‘AddressXCheck_Display.mxd’ to browse the data, a ‘relate’ has already been done using the above-mentioned fields.  To select the corresponding feature, use the ‘Related Tables” option in the attribute table.
![Geodatabase](/images/relate.png)
* The table named, ‘tbl_Report’ can be ignored.  It is used in the creation of the ‘xxxx_ToCheck’ feature classes and contains all the records that were flagged during processing.

The code for AGRC’s Address Cross Check project can be found in the following GitHub Repository: https://github.com/gregbunce/AddressData_CrossCheck

### Utah's NG911 GIS Data Roadmap
* Mid-term
	* Break/Split road segments at PSAP and Jurisdiction Boundaries - while updating any fields that are tied to the corresponding boundaries. 
	* Ensure the SGID PSAP boundaries are official, and that each PSAP has signed off on them.  It is essential that these boundaries are correct, as they will used to 911 route calls. 
	* Establish a multi-user mechanism that supports ‘near-live’ NG911 data editing.  This would allow data stewards (cities, counties, PSAPs, etc) to directly edit 911 GIS data.  This will facilitate in the requirement that data modifications get into the official NG911 database in xx hours (verify the time requirement).
* Long-term
	* Get all 911-related data stewards the needed training and access to the NG911 multi-user, data editing platform.
	* Incorporate a workflow to review these edits (from the NG911 multi-user, data editing platform) and push them into the official State of Utah NG911 database (on a daily basis).
	* Establish the NG911 multi-user, data editing platform as Utah’s official NG911 data editor.
