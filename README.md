# AddressData_CrossCheck

#### Project Web Page
Please visit the project's web page for the most current info on this project:
[NG911 Address Cross Check Web Page](https://gregbunce.github.io/AddressData_CrossCheck/)

#### Coding Notes
C#, ArcObjects project that uses sql spatial queries to cross check address points and roads address data - checking for attributes, distance, and address range issues.  The project outputs a file geodatabase containing the address and roads data with possible matching issues.  
     
#### Usage Notes
* These checks are considered essential when preparing GIS data for Next Generation 911.
* This code requires that address point and road data is in a SQL Server Database (Enterprise, SDE, etc.).
* User needs to set up a connection string to SQL Server in their Environment Variables or you can hard-code the connection string (you can update the commented out code to hard-code the connection string).
* Valid args are: [0] Type of CrossCheck (CountyID _or_ AddSystem); [1] Juris Name (LOA _or_ 49031); [2] Search Distance in meters (300)
* Examples of args: AddSystem LOA 295 _or_ CountyID 49031 295
