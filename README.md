# AddressData_CrossCheck
C#, ArcObjects project that uses sql spatial queries to cross check address points and roads address data - checking for attributes, distance, and address range issues.  The project outputs a file geodatabase containing the address and roads data with possible matching issues.  

Overview of operations: 
Iterates through each address point and spatially searches for the two closest, matching road segment with the same name, that are within a user-specified distance.
Then the code checks the road segments for other matching attributes such as: pre direction, post type, post direction, and if the house number is contained within the segment’s address range.  Additionally, the process allows the user to define a distance in which the address point will be flagged if it falls outside of this range.       

Notes:
*These checks are considered essential when preparing GIS data for Next Generation 911.
*This code requires that address point and road data is in a SQL Server Database (Enterprise, SDE, etc.).
*User needs to set up a connection string to SQL Server in their Environment Variables or you can hard-code the connection string (you can update the commented out code to hard-code the connection string).
*Valid args are: [0] Type of CrossCheck (CountyID _or_ AddSystem); [1] Juris Name (LOA _or_ 49031); [2] Search Distance in meters (300)
*Examples of args: AddSystem LOA 295 _or_ CountyID 49031 295
