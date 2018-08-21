# Address Cross Check Project

### [Click here to visit this project's web page](https://gregbunce.github.io/AddressData_CrossCheck/)

### Coding Notes
This project was written with C# and ArcObjects, and uses sql spatial queries to cross check address components in address point data and road centerline data - checking for attribute mismatch, distance issues, and address range issues.  The project outputs a file geodatabase containing the address point and road centerline data with possible issues.  
     
### Usage Notes
* These checks are considered essential when preparing GIS data for Next Generation 911.
* This code requires that address point and road data is in a SQL Server Database (Enterprise, SDE, etc.).
* User needs to set up a connection string to SQL Server in their Environment Variables or you can hard-code the connection string (you can update the commented out code to hard-code the connection string).
* Valid args are: [0] Type of CrossCheck (CountyID _or_ AddSystem); [1] Juris Name (LOA _or_ 49031); [2] Search Distance in meters (300)
* Examples of args: AddSystem LOA 295 _or_ CountyID 49031 295
