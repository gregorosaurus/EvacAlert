# Evac Alert

Evac alert is a demonstration of how to use Azure functions to pull 
evacuation notices as well as geo code addresses. 

The solution is primarily an Azure function app, that contains various functions that support geocoding and determining which addresses are under an evacuation state. 

Additionally, there is a simple Blazor Server application that visually shows the current evacuation zones in the province of BC. 

## Functions

There are several HTTP trigger functions in the Azure Function App inside EvacAlert.  These typically are used in conjunction with Azure Data Factory.

| Function Name | Purpose |
|---------------|---------|
| GeocodeAddresses | Used for geocoding addresses.  Supports csv or json. This function resturns the name of the address sent in, along with the coordinate |
| CurrentEvacuationAreas | Retrieves the current evacuation areas in the province of BC |
| FindPersonsInEvacuationZones | Filters the list of people sent to the function with those that are in an active evacuation zone |

## Storage

The solution requires a ADLS (HFS enabled storage account) on Azure.  This storage account is used for: 

1. Storing the uploaded non-geocoded addresses and identifiers.
2. Storing the geocoded addresses.
3. Storing a simple CSV output of those who are currently in an evacuation zone.

The storage account has two containers: one called upload, one called reports. 

Data must be uploaded to the path **/data/upload/addresses.csv**.  The ADF job is built to use this. 

## Process Overview

The deployment works in two stages. 

1. Geocoding the addresses. 
2. Checking if the addresses are in any evacuation zone. 

### Geocoding
```
┌───────────────┐                      ┌───────────────┐                      ┌───────────────┐
│               │                      │               │                      │               │
│     ADLS      │                      │      ADF      │                      │  FunctionApp  │
│               │                      │               │                      │               │
└───────┬───────┘                      └───────┬───────┘                      └───────┬───────┘
        │                                      │                                      │        
        │                                      │                                      │        
        ├────────────Read Addresses───────────▶│                                      │        
        │                                      │                                      │        
        │                                      ├──────────GeoCode Addresses──────────▶│        
        │                                      │                                      │        
        │                                      │                                      │        
        │                                      │                                      │        
        │                                      │◀────────────Read Response────────────┤        
        │                                      │                                      │        
        │◀──────Write GeoCoded Addresses───────┤                                      │        
        │                                      │                                      │        
        │                                      │                                      │        
        │                                      │                                      │        
        │                                      │                                      │        
        ▼                                      ▼                                      ▼        
```

### Evaluation

```
┌───────────────┐                      ┌───────────────┐                      ┌───────────────┐
│               │                      │               │                      │               │
│     ADLS      │                      │      ADF      │                      │  FunctionApp  │
│               │                      │               │                      │               │
└───────┬───────┘                      └───────┬───────┘                      └───────┬───────┘
        │                                      │                                      │        
        │                                      │                                      │        
        │                                      │                                      │        
        │                                      │                                      │        
        ├─────────Read GeoCoded Points────────▶│                                      │        
        │                                      │                                      │        
        │                                      │                                      │        
        │                                      ├───────────Check Geo Points──────────▶│        
        │                                      │                                      │        
        │                                      │                                      │        
        │                                      │                                      │        
        │                                      │                                      │        
        │                                      │◀──────Points in Evacuation Zones─────┤        
        │                                      │                                      │        
        │                                      │                                      │        
        │◀────────Write Evacuation Zones───────┤                                      │        
        │                                      │                                      │        
        │                                      │                                      │        
        │                                      │                                      │        
        │                                      │                                      │          
        ▼                                      ▼                                      ▼        
```

## Deployment

The deployment of the solution is handled mostly via a BICEP template in this repo. 

```bash
az login
#optionally select the subscription
#az account set --subscription <name>
az deployment group create --template-file ./deployment.bicep --resource-group '<resoucegroupname>' --parameters companyident=<companyident> environment=test --confirm-with-what-if
```

### Post Deployment

There are a few things we need to do post bicep deploment. 

1. Set the AzureMapsApiKey app setting inside the function app of the azure maps account.  Note: this should be saved in a keyvault in production.
