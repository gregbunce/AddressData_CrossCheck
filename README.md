# AddressData_CrossCheck
C#, ArcObjects project that uses sql spatial queries to cross check address points and roads address data - checking for attributes, distance, and address range issues.  The project outputs a file geodatabase containing the address and roads data with potential issues.  These checks are considered essential when preparing GIS data for Next Generation 911.

Notes:
*User needs to set up a connection string to SQL Server in their Environment Variables
*Valid args are: [0] Type of CrossCheck (CountyID _or_ AddSystem); [1] Juris Name (LOA _or_ 49031); [2] Search Distance in meters (300)
*Examaples of args: AddSystem LOA 295 _or_ CountyID 49031 295
