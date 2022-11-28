# Evac Alert

Evac alert is a demonstration of how to use Azure functions to pull 
evacuation notices as well as geo code addresses. 

## Functions

There are three functions in the Azure Function App inside EvacAlert.

| Function Name | Purpose |
|---------------|---------|
| GeocodeAddresses | Used for geocoding addresses.  Supports csv or json. This function resturns the name of the address sent in, along with the coordinate |
| CurrentEvacuationAreas | Retrieves the current evacuation areas |
| FindPersonsInEvacuationZones | Filters the list of people sent to the function with those that are in an active evacuation zone |

